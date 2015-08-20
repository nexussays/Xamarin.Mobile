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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Locations;
using Android.OS;
using Object = Java.Lang.Object;

namespace Xamarin.Geolocation
{
   internal class GeolocationSingleListener
      : Object,
        ILocationListener
   {
      private readonly HashSet<string> activeProviders = new HashSet<string>();
      private readonly TaskCompletionSource<Position> completionSource = new TaskCompletionSource<Position>();
      private readonly float desiredAccuracy;
      private readonly Action finishedCallback;
      private readonly object locationSync = new object();
      private readonly Timer timer;
      private Location bestLocation;

      public GeolocationSingleListener( float desiredAccuracy, int timeout, IEnumerable<string> activeProviders,
                                        Action finishedCallback )
      {
         this.desiredAccuracy = desiredAccuracy;
         this.finishedCallback = finishedCallback;

         this.activeProviders = new HashSet<string>( activeProviders );

         if(timeout != Timeout.Infinite)
         {
            timer = new Timer( TimesUp, null, timeout, 0 );
         }
      }

      public Task<Position> Task
      {
         get { return completionSource.Task; }
      }

      public void Cancel()
      {
         completionSource.TrySetCanceled();
      }

      public void OnLocationChanged( Location location )
      {
         if(location.Accuracy <= desiredAccuracy)
         {
            Finish( location );
            return;
         }

         lock(locationSync)
         {
            if(bestLocation == null || location.Accuracy <= bestLocation.Accuracy)
            {
               bestLocation = location;
            }
         }
      }

      public void OnProviderDisabled( string provider )
      {
         lock(activeProviders)
         {
            if(activeProviders.Remove( provider ) && activeProviders.Count == 0)
            {
               completionSource.TrySetException( new GeolocationException( GeolocationError.PositionUnavailable ) );
            }
         }
      }

      public void OnProviderEnabled( string provider )
      {
         lock(activeProviders) activeProviders.Add( provider );
      }

      public void OnStatusChanged( string provider, Availability status, Bundle extras )
      {
         switch(status)
         {
            case Availability.Available:
               OnProviderEnabled( provider );
               break;

            case Availability.OutOfService:
               OnProviderDisabled( provider );
               break;
         }
      }

      private void Finish( Location location )
      {
         var p = new Position();
         if(location.HasAccuracy)
         {
            p.Accuracy = location.Accuracy;
         }
         if(location.HasAltitude)
         {
            p.Altitude = location.Altitude;
         }
         if(location.HasBearing)
         {
            p.Heading = location.Bearing;
         }
         if(location.HasSpeed)
         {
            p.Speed = location.Speed;
         }

         p.Longitude = location.Longitude;
         p.Latitude = location.Latitude;
         p.Timestamp = Geolocator.GetTimestamp( location );

         if(finishedCallback != null)
         {
            finishedCallback();
         }

         completionSource.TrySetResult( p );
      }

      private void TimesUp( object state )
      {
         lock(locationSync)
         {
            if(bestLocation == null)
            {
               if(completionSource.TrySetCanceled() && finishedCallback != null)
               {
                  finishedCallback();
               }
            }
            else
            {
               Finish( bestLocation );
            }
         }
      }
   }
}