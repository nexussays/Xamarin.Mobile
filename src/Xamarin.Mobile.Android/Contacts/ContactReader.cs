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
using System.Text;
using Android.Content;
using Android.Content.Res;
using Android.Database;
using Android.Provider;

namespace Xamarin.Contacts
{
   internal class ContactReader : IEnumerable<Contact>
   {
      private const Int32 BatchSize = 20;
      private readonly ContentResolver content;
      private readonly Boolean rawContacts;
      private readonly Resources resources;
      private readonly ContentQueryTranslator translator;

      public ContactReader( Boolean useRawContacts, ContentQueryTranslator translator, ContentResolver content,
                            Resources resources )
      {
         rawContacts = useRawContacts;
         this.translator = translator;
         this.content = content;
         this.resources = resources;
      }

      public IEnumerator<Contact> GetEnumerator()
      {
         var table = (rawContacts) ? ContactsContract.RawContacts.ContentUri : ContactsContract.Contacts.ContentUri;

         String query = null;
         String[] parameters = null;
         String sortString = null;
         String[] projections = null;

         if(translator != null)
         {
            table = translator.Table;
            query = translator.QueryString;
            parameters = translator.ClauseParameters;
            sortString = translator.SortString;

            if(translator.Projections != null)
            {
               projections =
                  translator.Projections.Where( p => p.Columns != null ).SelectMany( t => t.Columns ).ToArray();

               if(projections.Length == 0)
               {
                  projections = null;
               }
            }

            if(translator.Skip > 0 || translator.Take > 0)
            {
               var limitb = new StringBuilder();

               if(sortString == null)
               {
                  limitb.Append( ContactsContract.ContactsColumns.LookupKey );
               }

               limitb.Append( " LIMIT " );

               if(translator.Skip > 0)
               {
                  limitb.Append( translator.Skip );
                  if(translator.Take > 0)
                  {
                     limitb.Append( "," );
                  }
               }

               if(translator.Take > 0)
               {
                  limitb.Append( translator.Take );
               }

               sortString = (sortString == null) ? limitb.ToString() : sortString + limitb;
            }
         }

         ICursor cursor = null;
         try
         {
            cursor = content.Query( table, projections, query, parameters, sortString );
            if(cursor == null)
            {
               yield break;
            }

            foreach(Contact contact in ContactHelper.GetContacts( cursor, rawContacts, content, resources, BatchSize ))
            {
               yield return contact;
            }
         }
         finally
         {
            if(cursor != null)
            {
               cursor.Close();
            }
         }
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }
   }
}