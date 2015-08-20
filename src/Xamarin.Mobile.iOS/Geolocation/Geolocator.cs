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
using UIKit;

#else
using MonoTouch.CoreLocation;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Xamarin.Geolocation
{
   public class Geolocator
   {
      private readonly CLLocationManager manager;
      private Boolean isListening;
      private Position position;

      public Geolocator()
      {
         manager = GetManager();
         manager.AuthorizationChanged += OnAuthorizationChanged;
         manager.Failed += OnFailed;

         if(UIDevice.CurrentDevice.CheckSystemVersion( 6, 0 ))
         {
            manager.LocationsUpdated += OnLocationsUpdated;
         }
         else
         {
            manager.UpdatedLocation += OnUpdatedLocation;
         }

         manager.UpdatedHeading += OnUpdatedHeading;

         RequestAuthorization();
      }

      public event EventHandler<PositionEventArgs> PositionChanged;

      public event EventHandler<PositionErrorEventArgs> PositionError;

      public Double DesiredAccuracy { get; set; }

      public Boolean IsGeolocationAvailable
      {
         get { return true; } // all iOS devices support at least wifi geolocation
      }

      public Boolean IsGeolocationEnabled
      {
         get
         {
            var status = CLLocationManager.Status;

            if(UIDevice.CurrentDevice.CheckSystemVersion( 8, 0 ))
            {
               return status == CLAuthorizationStatus.AuthorizedAlways ||
                      status == CLAuthorizationStatus.AuthorizedWhenInUse;
            }
            else
            {
               return status == CLAuthorizationStatus.Authorized;
            }
         }
      }

      public Boolean IsListening
      {
         get { return isListening; }
      }

      public Boolean SupportsHeading
      {
         get { return CLLocationManager.HeadingAvailable; }
      }

      public Task<Position> GetPositionAsync( Int32 timeout )
      {
         return GetPositionAsync( timeout, CancellationToken.None, false );
      }

      public Task<Position> GetPositionAsync( Int32 timeout, Boolean includeHeading )
      {
         return GetPositionAsync( timeout, CancellationToken.None, includeHeading );
      }

      public Task<Position> GetPositionAsync( CancellationToken cancelToken )
      {
         return GetPositionAsync( Timeout.Infinite, cancelToken, false );
      }

      public Task<Position> GetPositionAsync( CancellationToken cancelToken, Boolean includeHeading )
      {
         return GetPositionAsync( Timeout.Infinite, cancelToken, includeHeading );
      }

      public Task<Position> GetPositionAsync( Int32 timeout, CancellationToken cancelToken )
      {
         return GetPositionAsync( timeout, cancelToken, false );
      }

      public Task<Position> GetPositionAsync( Int32 timeout, CancellationToken cancelToken, Boolean includeHeading )
      {
         if(timeout <= 0 && timeout != Timeout.Infinite)
         {
            throw new ArgumentOutOfRangeException( "timeout", "Timeout must be positive or Timeout.Infinite" );
         }

         TaskCompletionSource<Position> tcs;
         if(!IsListening)
         {
            var m = GetManager();

            tcs = new TaskCompletionSource<Position>( m );
            var singleListener = new GeolocationSingleUpdateDelegate(
               m,
               DesiredAccuracy,
               includeHeading,
               timeout,
               cancelToken );
            m.Delegate = singleListener;

            m.StartUpdatingLocation();
            if(includeHeading && SupportsHeading)
            {
               m.StartUpdatingHeading();
            }

            return singleListener.Task;
         }
         else
         {
            tcs = new TaskCompletionSource<Position>();
            if(position == null)
            {
               EventHandler<PositionErrorEventArgs> gotError = null;
               gotError = ( s, e ) =>
               {
                  tcs.TrySetException( new GeolocationException( e.Error ) );
                  PositionError -= gotError;
               };

               PositionError += gotError;

               EventHandler<PositionEventArgs> gotPosition = null;
               gotPosition = ( s, e ) =>
               {
                  tcs.TrySetResult( e.Position );
                  PositionChanged -= gotPosition;
               };

               PositionChanged += gotPosition;
            }
            else
            {
               tcs.SetResult( position );
            }
         }

         return tcs.Task;
      }

      public void StartListening( Int32 minTime, Double minDistance )
      {
         StartListening( minTime, minDistance, false );
      }

      public void StartListening( Int32 minTime, Double minDistance, Boolean includeHeading )
      {
         if(minTime < 0)
         {
            throw new ArgumentOutOfRangeException( "minTime" );
         }
         if(minDistance < 0)
         {
            throw new ArgumentOutOfRangeException( "minDistance" );
         }
         if(isListening)
         {
            throw new InvalidOperationException( "Already listening" );
         }

         isListening = true;
         manager.DesiredAccuracy = DesiredAccuracy;
         manager.DistanceFilter = minDistance;
         manager.StartUpdatingLocation();

         if(includeHeading && CLLocationManager.HeadingAvailable)
         {
            manager.StartUpdatingHeading();
         }
      }

      public void StopListening()
      {
         if(!isListening)
         {
            return;
         }

         isListening = false;
         if(CLLocationManager.HeadingAvailable)
         {
            manager.StopUpdatingHeading();
         }

         manager.StopUpdatingLocation();
         position = null;
      }

      private CLLocationManager GetManager()
      {
         CLLocationManager m = null;
         new NSObject().InvokeOnMainThread( () => m = new CLLocationManager() );
         return m;
      }

      private void OnAuthorizationChanged( Object sender, CLAuthorizationChangedEventArgs e )
      {
         if(e.Status == CLAuthorizationStatus.Denied || e.Status == CLAuthorizationStatus.Restricted)
         {
            OnPositionError( new PositionErrorEventArgs( GeolocationError.Unauthorized ) );
         }
      }

      private void OnFailed( Object sender, NSErrorEventArgs e )
      {
         if((CLError)(Int32)e.Error.Code == CLError.Network)
         {
            OnPositionError( new PositionErrorEventArgs( GeolocationError.PositionUnavailable ) );
         }
      }

      private void OnLocationsUpdated( Object sender, CLLocationsUpdatedEventArgs e )
      {
         foreach(CLLocation location in e.Locations)
         {
            UpdatePosition( location );
         }
      }

      private void OnPositionChanged( PositionEventArgs e )
      {
         var changed = PositionChanged;
         if(changed != null)
         {
            changed( this, e );
         }
      }

      private void OnPositionError( PositionErrorEventArgs e )
      {
         StopListening();

         var error = PositionError;
         if(error != null)
         {
            error( this, e );
         }
      }

      private void OnUpdatedHeading( Object sender, CLHeadingUpdatedEventArgs e )
      {
         if(e.NewHeading.TrueHeading == -1)
         {
            return;
         }

         Position p = (position == null) ? new Position() : new Position( position );

         p.Heading = e.NewHeading.TrueHeading;

         position = p;

         OnPositionChanged( new PositionEventArgs( p ) );
      }

      private void OnUpdatedLocation( Object sender, CLLocationUpdatedEventArgs e )
      {
         UpdatePosition( e.NewLocation );
      }

      private void RequestAuthorization()
      {
         var info = NSBundle.MainBundle.InfoDictionary;

         if(UIDevice.CurrentDevice.CheckSystemVersion( 8, 0 ))
         {
            if(info.ContainsKey( new NSString( "NSLocationWhenInUseUsageDescription" ) ))
            {
               manager.RequestWhenInUseAuthorization();
            }
            else if(info.ContainsKey( new NSString( "NSLocationAlwaysUsageDescription" ) ))
            {
               manager.RequestAlwaysAuthorization();
            }
            else
            {
               throw new UnauthorizedAccessException(
                  "On iOS 8.0 and higher you must set either NSLocationWhenInUseUsageDescription or NSLocationAlwaysUsageDescription in your Info.plist file to enable Authorization Requests for Location updates!" );
            }
         }
      }

      private void UpdatePosition( CLLocation location )
      {
         Position p = (position == null) ? new Position() : new Position( position );

         if(location.HorizontalAccuracy > -1)
         {
            p.Accuracy = location.HorizontalAccuracy;
            p.Latitude = location.Coordinate.Latitude;
            p.Longitude = location.Coordinate.Longitude;
         }

         if(location.VerticalAccuracy > -1)
         {
            p.Altitude = location.Altitude;
            p.AltitudeAccuracy = location.VerticalAccuracy;
         }

         if(location.Speed > -1)
         {
            p.Speed = location.Speed;
         }

         p.Timestamp = new DateTimeOffset( (DateTime)location.Timestamp );

         position = p;

         OnPositionChanged( new PositionEventArgs( p ) );

         location.Dispose();
      }
   }
}