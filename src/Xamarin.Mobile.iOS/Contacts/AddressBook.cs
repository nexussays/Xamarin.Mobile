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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using AddressBook;
using UIKit;

namespace Xamarin.Contacts
{
   public class AddressBook : IAddressBook //IQueryable<Contact>
   {
      private ABAddressBook addressBook;
      private IQueryProvider provider;

      public Boolean AggregateContactsSupported
      {
         get { return false; }
      }

      public Boolean IsReadOnly
      {
         get { return true; }
      }

      public Boolean LoadSupported
      {
         get { return true; }
      }

      public Boolean PreferContactAggregation { get; set; }

      public Boolean SingleContactsSupported
      {
         get { return true; }
      }

      public IEnumerator<IContact> GetEnumerator()
      {
         CheckStatus();
         return addressBook.GetPeople().Select( ContactHelper.GetContact ).GetEnumerator();
      }

      public IContact Load( String id )
      {
         if(String.IsNullOrWhiteSpace( id ))
         {
            throw new ArgumentNullException( "id" );
         }

         CheckStatus();

         Int32 rowId;
         if(!Int32.TryParse( id, out rowId ))
         {
            throw new ArgumentException( "Not a valid contact ID", "id" );
         }

         ABPerson person = addressBook.GetPerson( rowId );
         if(person == null)
         {
            return null;
         }

         return ContactHelper.GetContact( person );
      }

      public Task<Boolean> RequestPermission()
      {
         var tcs = new TaskCompletionSource<Boolean>();
         if(UIDevice.CurrentDevice.CheckSystemVersion( 6, 0 ))
         {
            var status = ABAddressBook.GetAuthorizationStatus();
            if(status == ABAuthorizationStatus.Denied || status == ABAuthorizationStatus.Restricted)
            {
               tcs.SetResult( false );
            }
            else
            {
               if(addressBook == null)
               {
                  addressBook = new ABAddressBook();
                  provider = new ContactQueryProvider( addressBook );
               }

               if(status == ABAuthorizationStatus.NotDetermined)
               {
                  addressBook.RequestAccess(
                     ( s, e ) =>
                     {
                        tcs.SetResult( s );
                        if(!s)
                        {
                           addressBook.Dispose();
                           addressBook = null;
                           provider = null;
                        }
                     } );
               }
               else
               {
                  tcs.SetResult( true );
               }
            }
         }
         else
         {
            tcs.SetResult( true );
         }

         return tcs.Task;
      }

      private void CheckStatus()
      {
         if(UIDevice.CurrentDevice.CheckSystemVersion( 6, 0 ))
         {
            var status = ABAddressBook.GetAuthorizationStatus();
            if(status != ABAuthorizationStatus.Authorized)
            {
               throw new SecurityException( "AddressBook has not been granted permission" );
            }
         }

         if(addressBook == null)
         {
            addressBook = new ABAddressBook();
            provider = new ContactQueryProvider( addressBook );
         }
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }
   }
}