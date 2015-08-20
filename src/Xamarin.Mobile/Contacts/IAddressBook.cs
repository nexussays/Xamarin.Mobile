using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.Contacts
{
   public interface IAddressBook : IEnumerable<IContact>
   {
      Boolean AggregateContactsSupported { get; }

      Boolean IsReadOnly { get; }

      Boolean LoadSupported { get; }

      Boolean PreferContactAggregation { get; set; }

      Boolean SingleContactsSupported { get; }

      IContact Load( String id );

      Task<Boolean> RequestPermission();
   }
}