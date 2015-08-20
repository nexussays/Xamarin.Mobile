//
//  Copyright 2011-2013, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.Threading;
using System.Threading.Tasks;
#if __UNIFIED__
using CoreLocation;
using Foundation;

#else
using MonoTouch.CoreLocation;
using MonoTouch.Foundation;
#endif

namespace Xamarin.Geolocation
{
   internal class GeolocationSingleUpdateDelegate : CLLocationManagerDelegate
   {
      private readonly double desiredAccuracy;
      private readonly bool includeHeading;
      private readonly CLLocationManager manager;
      private readonly Position position = new Position();
      private readonly TaskCompletionSource<Position> tcs;
      private CLHeading bestHeading;
      private bool haveHeading;
      private bool haveLocation;

      public GeolocationSingleUpdateDelegate( CLLocationManager manager, double desiredAccuracy, bool includeHeading,
                                              int timeout, CancellationToken cancelToken )
      {
         this.manager = manager;
         tcs = new TaskCompletionSource<Position>( manager );
         this.desiredAccuracy = desiredAccuracy;
         this.includeHeading = includeHeading;

         if(timeout != Timeout.Infinite)
         {
            Timer t = null;
            t = new Timer(
               s =>
               {
                  if(haveLocation)
                  {
                     tcs.TrySetResult( new Position( position ) );
                  }
                  else
                  {
                     tcs.TrySetCanceled();
                  }

                  StopListening();
                  t.Dispose();
               },
               null,
               timeout,
               0 );
         }

         cancelToken.Register(
            () =>
            {
               StopListening();
               tcs.TrySetCanceled();
            } );
      }

      public Task<Position> Task
      {
         get { return tcs.Task; }
      }

      public override void AuthorizationChanged( CLLocationManager manager, CLAuthorizationStatus status )
      {
         // If user has services disabled, we're just going to throw an exception for consistency.
         if(status == CLAuthorizationStatus.Denied || status == CLAuthorizationStatus.Restricted)
         {
            StopListening();
            tcs.TrySetException( new GeolocationException( GeolocationError.Unauthorized ) );
         }
      }

      public override void Failed( CLLocationManager manager, NSError error )
      {
         switch((CLError)(int)error.Code)
         {
            case CLError.Network:
               StopListening();
               tcs.SetException( new GeolocationException( GeolocationError.PositionUnavailable ) );
               break;
         }
      }

      public override bool ShouldDisplayHeadingCalibration( CLLocationManager manager )
      {
         return true;
      }

      public override void UpdatedHeading( CLLocationManager manager, CLHeading newHeading )
      {
         if(newHeading.HeadingAccuracy < 0)
         {
            return;
         }
         if(bestHeading != null && newHeading.HeadingAccuracy >= bestHeading.HeadingAccuracy)
         {
            return;
         }

         bestHeading = newHeading;
         position.Heading = newHeading.TrueHeading;
         haveHeading = true;

         if(haveLocation && position.Accuracy <= desiredAccuracy)
         {
            tcs.TrySetResult( new Position( position ) );
            StopListening();
         }
      }

      public override void UpdatedLocation( CLLocationManager manager, CLLocation newLocation, CLLocation oldLocation )
      {
         if(newLocation.HorizontalAccuracy < 0)
         {
            return;
         }

         if(haveLocation && newLocation.HorizontalAccuracy > position.Accuracy)
         {
            return;
         }

         position.Accuracy = newLocation.HorizontalAccuracy;
         position.Altitude = newLocation.Altitude;
         position.AltitudeAccuracy = newLocation.VerticalAccuracy;
         position.Latitude = newLocation.Coordinate.Latitude;
         position.Longitude = newLocation.Coordinate.Longitude;
         position.Speed = newLocation.Speed;
         position.Timestamp = new DateTimeOffset( (DateTime)newLocation.Timestamp );

         haveLocation = true;

         if((!includeHeading || haveHeading) && position.Accuracy <= desiredAccuracy)
         {
            tcs.TrySetResult( new Position( position ) );
            StopListening();
         }
      }

      private void StopListening()
      {
         if(CLLocationManager.HeadingAvailable)
         {
            manager.StopUpdatingHeading();
         }

         manager.StopUpdatingLocation();
      }
   }
}