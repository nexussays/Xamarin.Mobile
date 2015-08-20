using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Media;

namespace Xamarin.Contacts
{
   public interface IContact
   {
      IEnumerable<Address> Addresses { get; set; }

      String DisplayName { get; set; }

      IEnumerable<Email> Emails { get; set; }

      String FirstName { get; set; }

      String Id { get; }

      IEnumerable<InstantMessagingAccount> InstantMessagingAccounts { get; set; }

      Boolean IsAggregate { get; }

      String LastName { get; set; }

      String MiddleName { get; set; }

      String Nickname { get; set; }

      IEnumerable<Note> Notes { get; set; }

      IEnumerable<Organization> Organizations { get; set; }

      IEnumerable<Phone> Phones { get; set; }

      String Prefix { get; set; }

      IEnumerable<Relationship> Relationships { get; set; }

      String Suffix { get; set; }

      IEnumerable<Website> Websites { get; set; }

      //Bitmap GetThumbnail();
      //UIImage GetThumbnail();
      Task<IMediaFile> SaveThumbnailAsync( String path );
   }
}