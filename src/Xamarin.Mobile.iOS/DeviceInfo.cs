using System;
using UIKit;

namespace Xamarin
{
   public class DeviceInfo : IDeviceInfo
   {
      public Double ScreenHeight => UIScreen.MainScreen.Bounds.Height;

      public Double ScreenWidth => UIScreen.MainScreen.Bounds.Width;
   }
}