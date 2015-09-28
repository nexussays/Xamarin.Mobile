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
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Provider;
using Xamarin.Media;

namespace Xamarin.Contacts
{
   public class Contact : IContact
   {
      internal List<Address> addresses = new List<Address>();
      internal List<Email> emails = new List<Email>();
      internal List<InstantMessagingAccount> instantMessagingAccounts = new List<InstantMessagingAccount>();
      internal List<Note> notes = new List<Note>();
      internal List<Organization> organizations = new List<Organization>();
      internal List<Phone> phones = new List<Phone>();
      internal List<Relationship> relationships = new List<Relationship>();
      internal List<Website> websites = new List<Website>();

      private readonly ContentResolver m_contentResolver;

      public Contact()
      {
      }

      internal Contact( String id, Boolean isAggregate, ContentResolver contentResolver )
      {
         m_contentResolver = contentResolver;
         IsAggregate = isAggregate;
         Id = id;
      }

      public IEnumerable<Address> Addresses
      {
         get { return addresses; }
         set { addresses = new List<Address>( value ); }
      }

      public String DisplayName { get; set; }

      public IEnumerable<Email> Emails
      {
         get { return emails; }
         set { emails = new List<Email>( value ); }
      }

      public String FirstName { get; set; }

      public String Id { get; private set; }

      public IEnumerable<InstantMessagingAccount> InstantMessagingAccounts
      {
         get { return instantMessagingAccounts; }
         set { instantMessagingAccounts = new List<InstantMessagingAccount>( value ); }
      }

      public Boolean IsAggregate { get; private set; }

      public String LastName { get; set; }

      public String MiddleName { get; set; }

      public String Nickname { get; set; }

      public IEnumerable<Note> Notes
      {
         get { return notes; }
         set { notes = new List<Note>( value ); }
      }

      public IEnumerable<Organization> Organizations
      {
         get { return organizations; }
         set { organizations = new List<Organization>( value ); }
      }

      public IEnumerable<Phone> Phones
      {
         get { return phones; }
         set { phones = new List<Phone>( value ); }
      }

      public String Prefix { get; set; }

      public IEnumerable<Relationship> Relationships
      {
         get { return relationships; }
         set { relationships = new List<Relationship>( value ); }
      }

      public String Suffix { get; set; }

      public IEnumerable<Website> Websites
      {
         get { return websites; }
         set { websites = new List<Website>( value ); }
      }

      public Byte[] GetThumbnail()
      {
         var lookupColumn = (IsAggregate)
            ? ContactsContract.ContactsColumns.LookupKey
            : ContactsContract.RawContactsColumns.ContactId;
         var projection = new[]
         {
            ContactsContract.CommonDataKinds.Photo.PhotoColumnId,
            ContactsContract.DataColumns.Mimetype,
         };
         var uri = ContactsContract.Data.ContentUri;
         //uri = Android.Net.Uri.WithAppendedPath(ContentUris.WithAppendedId(uri, Id), ContactsContract.Contacts.Photo.ContentDirectory);
         using(
            var cursor = m_contentResolver.Query(
               uri,
               projection,
               lookupColumn + "=? AND " + ContactsContract.DataColumns.Mimetype + "=?",
               new[] {Id, ContactsContract.CommonDataKinds.Photo.ContentItemType},
               null ))
         {
            if(cursor.MoveToFirst())
            {
               do
               {
                  var bytes = cursor.GetBlob( cursor.GetColumnIndex( projection[0] ) );
                  if(bytes != null)
                  {
                     // return BitmapFactory.DecodeByteArray( bytes, 0, bytes.Length );
                     return bytes;
                  }
               } while(cursor.MoveToNext());
            }
         }
         return null;
      }

      public Task<IMediaFile> SaveThumbnailAsync( String path )
      {
         if(path == null)
         {
            throw new ArgumentNullException( "path" );
         }

         return Task.Factory.StartNew(
            () =>
            {
               var bytes = GetThumbnail();
               if(bytes == null)
               {
                  return null;
               }

               File.WriteAllBytes( path, bytes );
               return (IMediaFile)new MediaFile( path, deletePathOnDispose: false );
            } );
      }
   }
}