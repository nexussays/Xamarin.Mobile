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
using System.Threading.Tasks;
using Android.Content;
using Android.Database;
using Object = Java.Lang.Object;

namespace Xamarin
{
   internal class AsyncQuery<T> : AsyncQueryHandler
   {
      private readonly Func<ICursor, bool> predicate;
      private readonly Func<ICursor, T> selector;
      private readonly TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

      internal AsyncQuery( ContentResolver cr, Func<ICursor, T> selector )
         : base( cr )
      {
         if(selector == null)
         {
            throw new ArgumentNullException( "selector" );
         }

         this.selector = selector;
      }

      internal AsyncQuery( ContentResolver cr, Func<ICursor, T> selector, Func<ICursor, bool> predicate )
         : this( cr, selector )
      {
         this.selector = selector;
         this.predicate = predicate;
      }

      public Task<T> Task
      {
         get { return tcs.Task; }
      }

      protected override void OnQueryComplete( int token, Object cookie, ICursor cursor )
      {
         bool set = false;
         while(cursor.MoveToNext())
         {
            if(predicate == null || predicate( cursor ))
            {
               set = true;
               tcs.SetResult( selector( cursor ) );
               break;
            }
         }

         if(!set)
         {
            tcs.SetResult( default(T) );
         }

         base.OnQueryComplete( token, cookie, cursor );
      }
   }
}