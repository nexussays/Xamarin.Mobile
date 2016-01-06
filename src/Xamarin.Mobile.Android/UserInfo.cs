using System;
using System.Diagnostics.Contracts;
using Android.Telephony;

namespace Xamarin
{
   public class UserInfo : IUserInfo
   {
      private readonly TelephonyManager m_telephony;

      public UserInfo( TelephonyManager manager )
      {
         Contract.Requires( manager != null );
         if(manager == null)
         {
            throw new ArgumentNullException( nameof( manager ) );
         }
         m_telephony = manager;
      }

      public String PhoneNumber => m_telephony.Line1Number;
   }
}