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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AddressBook;
using Foundation;
using UIKit;
using Xamarin.Media;

namespace Xamarin.Contacts
{
   public class Contact : IContact
   {
      internal List<Address> addresses = new List<Address>();
      internal List<Email> emails = new List<Email>();
      internal List<InstantMessagingAccount> imAccounts = new List<InstantMessagingAccount>();
      internal List<Note> notes = new List<Note>();
      internal List<Organization> organizations = new List<Organization>();
      internal List<Phone> phones = new List<Phone>();
      internal List<Relationship> relationships = new List<Relationship>();
      internal List<Website> websites = new List<Website>();
      private readonly ABPerson person;

      public Contact()
      {
      }

      internal Contact( ABPerson person )
      {
         Id = person.Id.ToString();
         this.person = person;
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
         set
         {
            try
            {
               emails = new List<Email>( value );
            }
            catch(Exception)
            {
               emails = new List<Email>();
            }
         }
      }

      public String FirstName { get; set; }

      public String Id { get; private set; }

      public IEnumerable<InstantMessagingAccount> InstantMessagingAccounts
      {
         get { return imAccounts; }
         set { imAccounts = new List<InstantMessagingAccount>( value ); }
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
         var img = GetThumbnailAsUIImage();
         if(img != null)
         {
            using(var input = img.AsPNG().AsStream())
            {
               if(input is MemoryStream)
               {
                  return ((MemoryStream)input).ToArray();
               }
               using(var output = new MemoryStream())
               {
                  Int32 read;
                  var buffer = new Byte[4096];
                  while((read = input.Read( buffer, 0, buffer.Length )) > 0)
                  {
                     output.Write( buffer, 0, read );
                  }
                  return output.ToArray();
               }
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

         return Task<IMediaFile>.Factory.StartNew(
            s =>
            {
               var p = (String)s;

               using(UIImage img = GetThumbnailAsUIImage())
               {
                  if(img == null)
                  {
                     return null;
                  }

                  using(var stream = new NSDataStream( img.AsJPEG() ))
                  using(var fs = File.OpenWrite( p ))
                  {
                     stream.CopyTo( fs );
                     fs.Flush();
                  }
               }

               return new MediaFile( p, () => File.OpenRead( path ) );
            },
            path );
      }

      private UIImage GetThumbnailAsUIImage()
      {
         if(!person.HasImage)
         {
            return null;
         }

         NSData data;
         lock(person) data = person.GetImage( ABPersonImageFormat.Thumbnail );

         if(data == null)
         {
            return null;
         }
         return UIImage.LoadFromData( data );
      }

      [DllImport( "/System/Library/Frameworks/AddressBook.framework/AddressBook" )]
      private static extern IntPtr ABPersonCopyImageDataWithFormat( IntPtr handle, ABPersonImageFormat format );
   }
}