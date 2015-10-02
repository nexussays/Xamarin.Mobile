using System;
using System.Diagnostics.Contracts;
using Android.Telephony;

namespace Xamarin
{
   public class DeviceAccountInfo : IDeviceAccountInfo
   {
      private readonly TelephonyManager m_telephony;

      public DeviceAccountInfo( TelephonyManager manager )
      {
         Contract.Requires( manager != null );
         if(manager == null)
         {
            throw new ArgumentNullException( "manager" );
         }
         m_telephony = manager;
      }

      public String PhoneNumber
      {
         get { return m_telephony.Line1Number; }
      }
   }
}