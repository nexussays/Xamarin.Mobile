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
using Android.Locations;
using Android.OS;
using Object = Java.Lang.Object;

namespace Xamarin.Geolocation
{
   internal class GeolocationContinuousListener
      : Object,
        ILocationListener
   {
      private readonly HashSet<String> activeProviders = new HashSet<String>();
      private readonly LocationManager manager;

      private String activeProvider;
      private Location lastLocation;
      private IList<String> providers;
      private TimeSpan timePeriod;

      public GeolocationContinuousListener( LocationManager manager, TimeSpan timePeriod, IList<String> providers )
      {
         this.manager = manager;
         this.timePeriod = timePeriod;
         this.providers = providers;

         foreach(String p in providers)
         {
            if(manager.IsProviderEnabled( p ))
            {
               activeProviders.Add( p );
            }
         }
      }

      public event EventHandler<PositionEventArgs> PositionChanged;

      public event EventHandler<PositionErrorEventArgs> PositionError;

      public void OnLocationChanged( Location location )
      {
         if(location.Provider != activeProvider)
         {
            if(activeProvider != null && manager.IsProviderEnabled( activeProvider ))
            {
               LocationProvider pr = manager.GetProvider( location.Provider );
               TimeSpan lapsed = GetTimeSpan( location.Time ) - GetTimeSpan( lastLocation.Time );

               if(pr.Accuracy > manager.GetProvider( activeProvider ).Accuracy && lapsed < timePeriod.Add( timePeriod ))
               {
                  location.Dispose();
                  return;
               }
            }

            activeProvider = location.Provider;
         }

         var previous = Interlocked.Exchange( ref lastLocation, location );
         if(previous != null)
         {
            previous.Dispose();
         }

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

         var changed = PositionChanged;
         if(changed != null)
         {
            changed( this, new PositionEventArgs( p ) );
         }
      }

      public void OnProviderDisabled( String provider )
      {
         if(provider == LocationManager.PassiveProvider)
         {
            return;
         }

         lock(activeProviders)
         {
            if(activeProviders.Remove( provider ) && activeProviders.Count == 0)
            {
               OnPositionError( new PositionErrorEventArgs( GeolocationError.PositionUnavailable ) );
            }
         }
      }

      public void OnProviderEnabled( String provider )
      {
         if(provider == LocationManager.PassiveProvider)
         {
            return;
         }

         lock(activeProviders) activeProviders.Add( provider );
      }

      public void OnStatusChanged( String provider, Availability status, Bundle extras )
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

      private TimeSpan GetTimeSpan( Int64 time )
      {
         return new TimeSpan( TimeSpan.TicksPerMillisecond * time );
      }

      private void OnPositionError( PositionErrorEventArgs e )
      {
         var error = PositionError;
         if(error != null)
         {
            error( this, e );
         }
      }
   }
}