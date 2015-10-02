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
using System.Globalization;
using System.Linq;
using AddressBook;
using Foundation;

namespace Xamarin.Contacts
{
   internal static class ContactHelper
   {
      internal static AddressType GetAddressType( String label )
      {
         if(label == ABLabel.Home)
         {
            return AddressType.Home;
         }
         if(label == ABLabel.Work)
         {
            return AddressType.Work;
         }

         return AddressType.Other;
      }

      internal static IContact GetContact( ABPerson person )
      {
         var contact = new Contact( person )
         {
            DisplayName = person.ToString(),
            Prefix = person.Prefix,
            FirstName = person.FirstName,
            MiddleName = person.MiddleName,
            LastName = person.LastName,
            Suffix = person.Suffix,
            Nickname = person.Nickname,
            Notes = (person.Note != null) ? new[] {new Note {Contents = person.Note}} : new Note[0],
            Emails =
               person.GetEmails()
                     .Select(
                        e =>
                           new Email
                           {
                              Address = e.Value,
                              Type = GetEmailType( e.Label ),
                              Label = (e.Label != null) ? GetLabel( e.Label ) : GetLabel( ABLabel.Other )
                           } ),
            Phones =
               person.GetPhones()
                     .Select(
                        p =>
                           new Phone
                           {
                              Number = p.Value,
                              Type = GetPhoneType( p.Label ),
                              Label = (p.Label != null) ? GetLabel( p.Label ) : GetLabel( ABLabel.Other )
                           } )
         };

         Organization[] orgs;
         if(person.Organization != null)
         {
            orgs = new Organization[1];
            orgs[0] = new Organization
            {
               Name = person.Organization,
               ContactTitle = person.JobTitle,
               Type = OrganizationType.Work,
               Label = GetLabel( ABLabel.Work )
            };
         }
         else
         {
            orgs = new Organization[0];
         }

         contact.Organizations = orgs;

         contact.InstantMessagingAccounts =
            person.GetInstantMessageServices()
                  .Select(
                     ima =>
                        new InstantMessagingAccount()
                        {
                           Service = GetImService( (NSString)ima.Value.Dictionary[ABPersonInstantMessageKey.Service] ),
                           ServiceLabel = ima.Value.ServiceName,
                           Account = ima.Value.Username
                        } );

         contact.Addresses =
            person.GetAllAddresses()
                  .Select(
                     a =>
                        new Address()
                        {
                           Type = GetAddressType( a.Label ),
                           Label = (a.Label != null) ? GetLabel( a.Label ) : GetLabel( ABLabel.Other ),
                           StreetAddress = a.Value.Street,
                           City = a.Value.City,
                           Region = a.Value.State,
                           Country = a.Value.Country,
                           PostalCode = a.Value.Zip
                        } );

         try
         {
            contact.Websites = person.GetUrls().Select(url => new Website { Address = url.Value });
         }
         catch(Exception)
         {
            contact.Websites = new List<Website>();
         }
         

         contact.Relationships =
            person.GetRelatedNames().Select( p => new Relationship {Name = p.Value, Type = GetRelationType( p.Label )} );

         return contact;
      }

      internal static EmailType GetEmailType( String label )
      {
         if(label == ABLabel.Home)
         {
            return EmailType.Home;
         }
         if(label == ABLabel.Work)
         {
            return EmailType.Work;
         }

         return EmailType.Other;
      }

      internal static InstantMessagingService GetImService( String service )
      {
         if(service == ABPersonInstantMessageService.Aim)
         {
            return InstantMessagingService.Aim;
         }
         if(service == ABPersonInstantMessageService.Icq)
         {
            return InstantMessagingService.Icq;
         }
         if(service == ABPersonInstantMessageService.Jabber)
         {
            return InstantMessagingService.Jabber;
         }
         if(service == ABPersonInstantMessageService.Msn)
         {
            return InstantMessagingService.Msn;
         }
         if(service == ABPersonInstantMessageService.Yahoo)
         {
            return InstantMessagingService.Yahoo;
         }
         if(service == ABPersonInstantMessageService.Facebook)
         {
            return InstantMessagingService.Facebook;
         }
         if(service == ABPersonInstantMessageService.Skype)
         {
            return InstantMessagingService.Skype;
         }
         if(service == ABPersonInstantMessageService.GoogleTalk)
         {
            return InstantMessagingService.Google;
         }
         if(service == ABPersonInstantMessageService.GaduGadu)
         {
            return InstantMessagingService.GaduGadu;
         }
         if(service == ABPersonInstantMessageService.QQ)
         {
            return InstantMessagingService.QQ;
         }

         return InstantMessagingService.Other;
      }

      internal static String GetLabel( NSString label )
      {
         return CultureInfo.CurrentCulture.TextInfo.ToTitleCase( ABAddressBook.LocalizedLabel( label ) );
      }

      internal static PhoneType GetPhoneType( String label )
      {
         if(label == ABLabel.Home)
         {
            return PhoneType.Home;
         }
         if(label == ABLabel.Work)
         {
            return PhoneType.Work;
         }
         if(label == ABPersonPhoneLabel.Mobile || label == ABPersonPhoneLabel.iPhone)
         {
            return PhoneType.Mobile;
         }
         if(label == ABPersonPhoneLabel.Pager)
         {
            return PhoneType.Pager;
         }
         if(label == ABPersonPhoneLabel.HomeFax)
         {
            return PhoneType.HomeFax;
         }
         if(label == ABPersonPhoneLabel.WorkFax)
         {
            return PhoneType.WorkFax;
         }

         return PhoneType.Other;
      }

      internal static RelationshipType GetRelationType( String label )
      {
         if(label == ABPersonRelatedNamesLabel.Spouse || label == ABPersonRelatedNamesLabel.Friend)
         {
            return RelationshipType.SignificantOther;
         }
         if(label == ABPersonRelatedNamesLabel.Child)
         {
            return RelationshipType.Child;
         }

         return RelationshipType.Other;
      }
   }
}