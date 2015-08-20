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
using System.Linq.Expressions;

namespace Xamarin
{
   internal class EvaluationNominator : ExpressionVisitor
   {
      private readonly Func<Expression, Boolean> predicate;
      private HashSet<Expression> candidates;
      private Boolean cannotBeEvaluated;

      internal EvaluationNominator( Func<Expression, Boolean> predicate )
      {
         if(predicate == null)
         {
            throw new ArgumentNullException( "predicate" );
         }

         this.predicate = predicate;
      }

      public HashSet<Expression> Nominate( Expression expression )
      {
         candidates = new HashSet<Expression>();
         Visit( expression );
         return candidates;
      }

      public override Expression Visit( Expression expression )
      {
         if(expression == null)
         {
            return null;
         }

         Boolean currentState = cannotBeEvaluated;
         cannotBeEvaluated = false;

         base.Visit( expression );

         if(!cannotBeEvaluated)
         {
            if(predicate( expression ))
            {
               candidates.Add( expression );
            }
            else
            {
               cannotBeEvaluated = true;
            }
         }

         cannotBeEvaluated |= currentState;

         return expression;
      }
   }
}