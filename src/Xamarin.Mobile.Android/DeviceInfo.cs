using System;
using Android.Content;

namespace Xamarin
{
   public class DeviceInfo : IDeviceInfo
   {
      public DeviceInfo( Context context )
      {
         ScreenHeight = (context.Resources.DisplayMetrics.WidthPixels - 0.5f) / context.Resources.DisplayMetrics.Density;
         ScreenWidth = (context.Resources.DisplayMetrics.HeightPixels - 0.5f) / context.Resources.DisplayMetrics.Density;
      }

      public Double ScreenHeight { get; }

      public Double ScreenWidth { get; }
   }
}