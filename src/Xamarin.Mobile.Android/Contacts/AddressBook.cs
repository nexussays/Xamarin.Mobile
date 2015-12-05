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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.Res;
using Android.Database;
using Android.Provider;
using Java.Lang;
using Boolean = System.Boolean;
using String = System.String;
using Uri = Android.Net.Uri;

namespace Xamarin.Contacts
{
   public sealed class AddressBook
      : IQueryable<IContact>,
        IAddressBook
   {
      private readonly ContactQueryProvider contactsProvider;
      private readonly ContentResolver content;
      private readonly Resources resources;

      public AddressBook( Context context )
      {
         if(context == null)
         {
            throw new ArgumentNullException( "context" );
         }

         content = context.ContentResolver;
         resources = context.Resources;
         contactsProvider = new ContactQueryProvider( context.ContentResolver, context.Resources );
      }

      public Boolean AggregateContactsSupported
      {
         get { return true; }
      }

      public Boolean IsReadOnly
      {
         get { return true; }
      }

      public Boolean LoadSupported
      {
         get { return true; }
      }

      public Boolean PreferContactAggregation
      {
         get { return !contactsProvider.UseRawContacts; }
         set { contactsProvider.UseRawContacts = !value; }
      }

      public Boolean SingleContactsSupported
      {
         get { return true; }
      }

      Type IQueryable.ElementType
      {
         get { return typeof(Contact); }
      }

      Expression IQueryable.Expression
      {
         get { return Expression.Constant( this ); }
      }

      IQueryProvider IQueryable.Provider
      {
         get { return contactsProvider; }
      }

      public IEnumerator<IContact> GetEnumerator()
      {
         return ContactHelper.GetContacts( !PreferContactAggregation, content, resources ).GetEnumerator();
      }

      /// <summary>
      /// Attempts to load a contact for the specified <paramref name="id" />.
      /// </summary>
      /// <param name="id"></param>
      /// <returns>The <see cref="Contact" /> if found, <c>null</c> otherwise.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="id" /> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException"><paramref name="id" /> is empty.</exception>
      public IContact Load( String id )
      {
         if(id == null)
         {
            throw new ArgumentNullException( "id" );
         }
         if(id.Trim() == String.Empty)
         {
            throw new ArgumentException( "Invalid ID", "id" );
         }

         Uri curi;
         String column;
         if(PreferContactAggregation)
         {
            curi = ContactsContract.Contacts.ContentUri;
            column = ContactsContract.ContactsColumns.LookupKey;
         }
         else
         {
            curi = ContactsContract.RawContacts.ContentUri;
            column = ContactsContract.RawContactsColumns.ContactId;
         }

         ICursor c = null;
         try
         {
            c = content.Query( curi, null, column + " = ?", new[] {id}, null );
            return (c.MoveToNext() ? ContactHelper.GetContact( !PreferContactAggregation, content, resources, c ) : null);
         }
         finally
         {
            if(c != null)
            {
               c.Deactivate();
            }
         }
      }

      public Task<Boolean> RequestPermission()
      {
         return Task.Run(
            () =>
            {
               try
               {
                  ICursor cursor = content.Query( ContactsContract.Data.ContentUri, null, null, null, null );
                  cursor.Dispose();
                  return true;
               }
               catch(SecurityException ex)
               {
                  Debug.WriteLine( ex.ToString() );
                  return false;
               }
            } );
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }
   }
}