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

namespace Xamarin
{
   internal class GenericQueryReader<T> : IEnumerable<T>
   {
      private readonly ContentResolver content;
      private readonly String defaultSort;
      private readonly Resources resources;
      private readonly Func<ICursor, Resources, T> selector;
      private readonly ContentQueryTranslator translator;

      public GenericQueryReader( ContentQueryTranslator translator, ContentResolver content, Resources resources,
                                 Func<ICursor, Resources, T> selector, String defaultSort )
         : this( translator, content, resources, selector )
      {
         if(defaultSort == null)
         {
            throw new ArgumentNullException( "defaultSort" );
         }

         this.defaultSort = defaultSort;
      }

      public GenericQueryReader( ContentQueryTranslator translator, ContentResolver content, Resources resources,
                                 Func<ICursor, Resources, T> selector )
      {
         if(translator == null)
         {
            throw new ArgumentNullException( "translator" );
         }
         if(content == null)
         {
            throw new ArgumentNullException( "content" );
         }
         if(resources == null)
         {
            throw new ArgumentNullException( "resources" );
         }
         if(selector == null)
         {
            throw new ArgumentNullException( "selector" );
         }

         this.translator = translator;
         this.content = content;
         this.resources = resources;
         this.selector = selector;
      }

      public IEnumerator<T> GetEnumerator()
      {
         ICursor cursor = null;
         try
         {
            String sortString = translator.SortString;
            if((sortString != null || defaultSort != null) && translator != null &&
               (translator.Skip > 0 || translator.Take > 0))
            {
               StringBuilder limitb = new StringBuilder();

               if(sortString == null)
               {
                  limitb.Append( defaultSort );
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

            String[] projections = (translator.Projections != null)
               ? translator.Projections.Where( p => p.Columns != null ).SelectMany( t => t.Columns ).ToArray()
               : null;

            cursor = content.Query(
               translator.Table,
               projections,
               translator.QueryString,
               translator.ClauseParameters,
               sortString );

            while(cursor.MoveToNext())
            {
               yield return selector( cursor, resources );
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