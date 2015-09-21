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
using System.Linq;
using System.Linq.Expressions;
using AddressBook;

namespace Xamarin.Contacts
{
   internal class ContactQueryProvider : IQueryProvider
   {
      private readonly ABAddressBook addressBook;

      internal ContactQueryProvider( ABAddressBook addressBook )
      {
         this.addressBook = addressBook;
      }

      IQueryable IQueryProvider.CreateQuery( Expression expression )
      {
         throw new NotImplementedException();
      }

      IQueryable<TElement> IQueryProvider.CreateQuery<TElement>( Expression expression )
      {
         return new Query<TElement>( this, expression );
      }

      Object IQueryProvider.Execute( Expression expression )
      {
         IQueryable<Contact> q = GetContacts().AsQueryable();

         expression = ReplaceQueryable( expression, q );

         if(expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(IOrderedQueryable<>))
         {
            return q.Provider.CreateQuery( expression );
         }
         else
         {
            return q.Provider.Execute( expression );
         }
      }

      TResult IQueryProvider.Execute<TResult>( Expression expression )
      {
         return (TResult)((IQueryProvider)this).Execute( expression );
      }

      private IEnumerable<Contact> GetContacts()
      {
         return addressBook.GetPeople().Select( ContactHelper.GetContact );
      }

      private static Expression ReplaceQueryable( Expression expression, Object value )
      {
         var mc = expression as MethodCallExpression;
         if(mc != null)
         {
            Expression[] args = mc.Arguments.ToArray();
            Expression narg = ReplaceQueryable( mc.Arguments[0], value );
            if(narg != args[0])
            {
               args[0] = narg;
               return Expression.Call( mc.Method, args );
            }
            else
            {
               return mc;
            }
         }

         var c = expression as ConstantExpression;
         if(c != null && c.Type.GetInterfaces().Contains( typeof(IQueryable<Contact>) ))
         {
            return Expression.Constant( value );
         }

         return expression;
      }
   }
}