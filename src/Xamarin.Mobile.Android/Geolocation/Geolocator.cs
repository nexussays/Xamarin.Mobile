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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Locations;
using Android.OS;
using Java.Lang;

namespace Xamarin.Geolocation
{
   public class Geolocator
   {
      private static readonly DateTime Epoch = new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );
      private readonly LocationManager manager;
      private readonly Object positionSync = new Object();
      private readonly String[] providers;
      private String headingProvider;
      private Position lastPosition;
      private GeolocationContinuousListener listener;

      public Geolocator( Context context )
      {
         if(context == null)
         {
            throw new ArgumentNullException( "context" );
         }

         manager = (LocationManager)context.GetSystemService( Context.LocationService );
         providers =
            manager.GetProviders( enabledOnly: false ).Where( s => s != LocationManager.PassiveProvider ).ToArray();
      }

      public event EventHandler<PositionEventArgs> PositionChanged;

      public event EventHandler<PositionErrorEventArgs> PositionError;

      public Double DesiredAccuracy { get; set; }

      public Boolean IsGeolocationAvailable
      {
         get { return providers.Length > 0; }
      }

      public Boolean IsGeolocationEnabled
      {
         get { return providers.Any( manager.IsProviderEnabled ); }
      }

      public Boolean IsListening
      {
         get { return listener != null; }
      }

      public Boolean SupportsHeading
      {
         get
         {
            return false;
//				if (this.headingProvider == null || !this.manager.IsProviderEnabled (this.headingProvider))
//				{
//					Criteria c = new Criteria { BearingRequired = true };
//					string providerName = this.manager.GetBestProvider (c, enabledOnly: false);
//
//					LocationProvider provider = this.manager.GetProvider (providerName);
//
//					if (provider.SupportsBearing())
//					{
//						this.headingProvider = providerName;
//						return true;
//					}
//					else
//					{
//						this.headingProvider = null;
//						return false;
//					}
//				}
//				else
//					return true;
         }
      }

      public Task<Position> GetPositionAsync( CancellationToken cancelToken )
      {
         return GetPositionAsync( cancelToken, false );
      }

      public Task<Position> GetPositionAsync( CancellationToken cancelToken, Boolean includeHeading )
      {
         return GetPositionAsync( Timeout.Infinite, cancelToken );
      }

      public Task<Position> GetPositionAsync( Int32 timeout )
      {
         return GetPositionAsync( timeout, false );
      }

      public Task<Position> GetPositionAsync( Int32 timeout, Boolean includeHeading )
      {
         return GetPositionAsync( timeout, CancellationToken.None );
      }

      public Task<Position> GetPositionAsync( Int32 timeout, CancellationToken cancelToken )
      {
         return GetPositionAsync( timeout, cancelToken, false );
      }

      public Task<Position> GetPositionAsync( Int32 timeout, CancellationToken cancelToken, Boolean includeHeading )
      {
         if(timeout <= 0 && timeout != Timeout.Infinite)
         {
            throw new ArgumentOutOfRangeException( "timeout", "timeout must be greater than or equal to 0" );
         }

         var tcs = new TaskCompletionSource<Position>();

         if(!IsListening)
         {
            GeolocationSingleListener singleListener = null;
            singleListener = new GeolocationSingleListener(
               (Single)DesiredAccuracy,
               timeout,
               providers.Where( manager.IsProviderEnabled ),
               finishedCallback: () =>
               {
                  for(Int32 i = 0; i < providers.Length; ++i)
                  {
                     manager.RemoveUpdates( singleListener );
                  }
               } );

            if(cancelToken != CancellationToken.None)
            {
               cancelToken.Register(
                  () =>
                  {
                     singleListener.Cancel();

                     for(Int32 i = 0; i < providers.Length; ++i)
                     {
                        manager.RemoveUpdates( singleListener );
                     }
                  },
                  true );
            }

            try
            {
               Looper looper = Looper.MyLooper() ?? Looper.MainLooper;

               Int32 enabled = 0;
               for(Int32 i = 0; i < providers.Length; ++i)
               {
                  if(manager.IsProviderEnabled( providers[i] ))
                  {
                     enabled++;
                  }

                  manager.RequestLocationUpdates( providers[i], 0, 0, singleListener, looper );
               }

               if(enabled == 0)
               {
                  for(Int32 i = 0; i < providers.Length; ++i)
                  {
                     manager.RemoveUpdates( singleListener );
                  }

                  tcs.SetException( new GeolocationException( GeolocationError.PositionUnavailable ) );
                  return tcs.Task;
               }
            }
            catch(SecurityException ex)
            {
               tcs.SetException( new GeolocationException( GeolocationError.Unauthorized, ex ) );
               return tcs.Task;
            }

            return singleListener.Task;
         }

         // If we're already listening, just use the current listener
         lock(positionSync)
         {
            if(lastPosition == null)
            {
               if(cancelToken != CancellationToken.None)
               {
                  cancelToken.Register( () => tcs.TrySetCanceled() );
               }

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
               tcs.SetResult( lastPosition );
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
         if(IsListening)
         {
            throw new InvalidOperationException( "This Geolocator is already listening" );
         }

         listener = new GeolocationContinuousListener( manager, TimeSpan.FromMilliseconds( minTime ), providers );
         listener.PositionChanged += OnListenerPositionChanged;
         listener.PositionError += OnListenerPositionError;

         Looper looper = Looper.MyLooper() ?? Looper.MainLooper;
         for(Int32 i = 0; i < providers.Length; ++i)
         {
            manager.RequestLocationUpdates( providers[i], minTime, (Single)minDistance, listener, looper );
         }
      }

      public void StopListening()
      {
         if(listener == null)
         {
            return;
         }

         listener.PositionChanged -= OnListenerPositionChanged;
         listener.PositionError -= OnListenerPositionError;

         for(Int32 i = 0; i < providers.Length; ++i)
         {
            manager.RemoveUpdates( listener );
         }

         listener = null;
      }

      private void OnListenerPositionChanged( Object sender, PositionEventArgs e )
      {
         if(!IsListening) // ignore anything that might come in afterwards
         {
            return;
         }

         lock(positionSync)
         {
            lastPosition = e.Position;

            var changed = PositionChanged;
            if(changed != null)
            {
               changed( this, e );
            }
         }
      }

      private void OnListenerPositionError( Object sender, PositionErrorEventArgs e )
      {
         StopListening();

         var error = PositionError;
         if(error != null)
         {
            error( this, e );
         }
      }

      internal static DateTimeOffset GetTimestamp( Location location )
      {
         return new DateTimeOffset( Epoch.AddMilliseconds( location.Time ) );
      }
   }
}