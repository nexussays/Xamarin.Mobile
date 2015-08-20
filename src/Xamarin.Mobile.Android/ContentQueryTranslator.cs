//
//  Copyright 2011-2014, Xamarin Inc.
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
using System.Text;
using Android.Provider;
using Uri = Android.Net.Uri;

namespace Xamarin
{
   internal class ContentQueryTranslator : ExpressionVisitor
   {
      private readonly List<String> arguments = new List<String>();
      private readonly IQueryProvider provider;
      private readonly StringBuilder queryBuilder = new StringBuilder();
      private readonly ITableFinder tableFinder;
      private Boolean fallback = false;
      private List<ContentResolverColumnMapping> projections;
      private StringBuilder sortBuilder;

      public ContentQueryTranslator( IQueryProvider provider, ITableFinder tableFinder )
      {
         this.provider = provider;
         this.tableFinder = tableFinder;
         Skip = -1;
         Take = -1;
      }

      public String[] ClauseParameters
      {
         get { return (arguments.Count > 0) ? arguments.ToArray() : null; }
      }

      public Boolean IsAny { get; private set; }

      public Boolean IsCount { get; private set; }

      public IEnumerable<ContentResolverColumnMapping> Projections
      {
         get { return projections; }
      }

      public String QueryString
      {
         get { return (queryBuilder.Length > 0) ? queryBuilder.ToString() : null; }
      }

      public Type ReturnType { get; private set; }

      public Int32 Skip { get; private set; }

      public String SortString
      {
         get { return (sortBuilder != null) ? sortBuilder.ToString() : null; }
      }

      public Uri Table { get; private set; }

      public Int32 Take { get; private set; }

      public Expression Translate( Expression expression )
      {
         Expression expr = Visit( expression );

         if(Table == null)
         {
            Table = tableFinder.DefaultTable;
         }

         return expr;
      }

      protected override Expression VisitMethodCall( MethodCallExpression methodCall )
      {
         if(methodCall.Arguments.Count == 0 ||
            !(methodCall.Arguments[0] is ConstantExpression || methodCall.Arguments[0] is MethodCallExpression))
         {
            fallback = true;
            return methodCall;
         }

         Expression expression = base.VisitMethodCall( methodCall );

         methodCall = expression as MethodCallExpression;
         if(methodCall == null)
         {
            fallback = true;
            return expression;
         }

         if(!fallback)
         {
            if(methodCall.Method.Name == "Where")
            {
               expression = VisitWhere( methodCall );
            }
            else if(methodCall.Method.Name == "Any")
            {
               expression = VisitAny( methodCall );
            }
            else if(methodCall.Method.Name == "Select")
            {
               expression = VisitSelect( methodCall );
            }
            else if(methodCall.Method.Name == "SelectMany")
            {
               expression = VisitSelectMany( methodCall );
            }
            else if(methodCall.Method.Name == "OrderBy" || methodCall.Method.Name == "OrderByDescending")
            {
               expression = VisitOrder( methodCall );
            }
            else if(methodCall.Method.Name == "Skip")
            {
               expression = VisitSkip( methodCall );
            }
            else if(methodCall.Method.Name == "Take")
            {
               expression = VisitTake( methodCall );
            }
            else if(methodCall.Method.Name == "Count")
            {
               expression = VisitCount( methodCall );
            }
            else if(methodCall.Method.Name == "First" || methodCall.Method.Name == "FirstOrDefault")
            {
               expression = VisitFirst( methodCall );
            }
            else if(methodCall.Method.Name == "Single" || methodCall.Method.Name == "SingleOrDefault")
            {
               expression = VisitSingle( methodCall );
            }
         }

         return expression;
      }

      private MemberExpression FindMemberExpression( Expression expression )
      {
         UnaryExpression ue = expression as UnaryExpression;
         if(ue != null)
         {
            expression = ue.Operand;
         }

         LambdaExpression le = expression as LambdaExpression;
         if(le != null)
         {
            expression = le.Body;
         }

         MemberExpression me = expression as MemberExpression;
         if(me != null && tableFinder.IsSupportedType( me.Member.DeclaringType ))
         {
            return me;
         }

         BinaryExpression be = expression as BinaryExpression;
         if(be != null)
         {
            me = be.Left as MemberExpression;
            if(me != null && tableFinder.IsSupportedType( me.Member.DeclaringType ))
            {
               return me;
            }

            me = be.Right as MemberExpression;
            if(me != null && tableFinder.IsSupportedType( me.Member.DeclaringType ))
            {
               return me;
            }
         }

         return null;
      }

      private Type GetExpressionArgumentType( Expression expression )
      {
         switch(expression.NodeType)
         {
            case ExpressionType.Constant:
               return ((ConstantExpression)expression).Value.GetType();
         }

         return null;
      }

      private Boolean TryGetTable( List<MemberExpression> memberExpressions )
      {
         if(memberExpressions.Count == 0)
         {
            fallback = true;
            return false;
         }

         Uri existingTable = Table;

         TableFindResult presult = null;

         foreach(MemberExpression me in memberExpressions)
         {
            TableFindResult result = tableFinder.Find( me );
            if(result.Table == null)
            {
               fallback = true;
               return false;
            }

            if(existingTable == null)
            {
               existingTable = result.Table;
               presult = result;
            }
            else if(existingTable != result.Table)
            {
               fallback = true;
               return false;
            }
         }

         if(presult == null)
         {
            fallback = true;
            return false;
         }

         Table = presult.Table;

         if(presult.MimeType != null)
         {
            if(queryBuilder.Length > 0)
            {
               queryBuilder.Append( " AND " );
            }

            queryBuilder.Append( String.Format( "({0} = ?)", ContactsContract.DataColumns.Mimetype ) );
         }

         arguments.Add( presult.MimeType );

         return true;
      }

      private Boolean TryGetTable( MemberExpression me )
      {
         if(me == null)
         {
            fallback = true;
            return false;
         }

         TableFindResult result = tableFinder.Find( me );
         if(result.MimeType != null)
         {
            if(queryBuilder.Length > 0)
            {
               queryBuilder.Append( " AND " );
            }

            queryBuilder.Append( String.Format( "({0} = ?)", ContactsContract.DataColumns.Mimetype ) );
         }

         arguments.Add( result.MimeType );

         if(Table == null)
         {
            Table = result.Table;
         }
         else if(Table != result.Table)
         {
            fallback = true;
            return false;
         }

         return true;
      }

      private Expression VisitAny( MethodCallExpression methodCall )
      {
         if(methodCall.Arguments.Count > 1)
         {
            VisitWhere( methodCall );
            if(fallback)
            {
               return methodCall;
            }
         }

         IsAny = true;
         return methodCall.Arguments[0];
      }

      private Expression VisitCount( MethodCallExpression methodCall )
      {
         if(methodCall.Arguments.Count > 1)
         {
            VisitWhere( methodCall );
            if(fallback)
            {
               return methodCall;
            }
         }

         IsCount = true;
         return methodCall.Arguments[0];
      }

      private Expression VisitFirst( MethodCallExpression methodCall )
      {
         if(methodCall.Arguments.Count > 1)
         {
            VisitWhere( methodCall );
            if(fallback)
            {
               return methodCall;
            }
         }

         Take = 1;
         return methodCall;
      }

      private Expression VisitOrder( MethodCallExpression methodCall )
      {
         MemberExpression me = FindMemberExpression( methodCall.Arguments[1] );
         if(!TryGetTable( me ))
         {
            return methodCall;
         }

         ContentResolverColumnMapping column = tableFinder.GetColumn( me.Member );
         if(column != null && column.Columns != null)
         {
            StringBuilder builder = sortBuilder ?? (sortBuilder = new StringBuilder());
            if(builder.Length > 0)
            {
               builder.Append( ", " );
            }

            if(column.Columns.Length > 1)
            {
               throw new NotSupportedException();
            }

            builder.Append( column.Columns[0] );

            if(methodCall.Method.Name == "OrderByDescending")
            {
               builder.Append( " DESC" );
            }

            return methodCall.Arguments[0];
         }

         return methodCall;
      }

      private Expression VisitSelect( MethodCallExpression methodCall )
      {
         MemberExpression me = FindMemberExpression( methodCall.Arguments[1] );
         if(!TryGetTable( me ))
         {
            return methodCall;
         }

         ContentResolverColumnMapping column = tableFinder.GetColumn( me.Member );
         if(column == null || column.Columns == null)
         {
            return methodCall;
         }

         (projections ?? (projections = new List<ContentResolverColumnMapping>())).Add( column );
         if(column.ReturnType.IsValueType || column.ReturnType == typeof(String))
         {
            ReturnType = column.ReturnType;
         }

         fallback = true;

         Type argType = GetExpressionArgumentType( methodCall.Arguments[0] );
         if(ReturnType == null || (argType != null && ReturnType.IsAssignableFrom( argType )))
         {
            return methodCall.Arguments[0];
         }

         return Expression.Constant(
            Activator.CreateInstance( typeof(Query<>).MakeGenericType( ReturnType ), provider ) );
      }

//		private Expression VisitSelect (MethodCallExpression methodCall)
//		{
//			List<MemberExpression> mes = MemberExpressionFinder.Find (methodCall.Arguments[1], this.tableFinder);
//			if (!TryGetTable (mes))
//				return methodCall;
//
//			Type returnType = null;
//
//			List<Tuple<string, Type>> projs = new List<Tuple<string, Type>>();
//			foreach (MemberExpression me in mes)
//			{
//				Tuple<string, Type> column = this.tableFinder.GetColumn (me.Member);
//				if (column == null)
//					return methodCall;
//				
//				if (returnType == null)
//					returnType = column.Item2;
//				if (returnType != column.Item2)
//					return methodCall;
//
//				projs.Add (column);
//			}
//
//			ReturnType = returnType;
//			this.fallback = true;
//
//			(this.projections ?? (this.projections = new List<Tuple<string, Type>>()))
//				.AddRange (projs);
//
//			return methodCall.Arguments[0];
//		}

      private Expression VisitSelectMany( MethodCallExpression methodCall )
      {
         List<MemberExpression> mes = MemberExpressionFinder.Find( methodCall, tableFinder );
         if(mes.Count > 1)
         {
            fallback = true;
            return methodCall;
         }

         if(!TryGetTable( mes ))
         {
            return methodCall;
         }

         ContentResolverColumnMapping column = tableFinder.GetColumn( mes[0].Member );
         if(column == null || column.ReturnType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
         {
            fallback = true;
            return methodCall;
         }

         ReturnType = column.ReturnType.GetGenericArguments()[0];

         return Expression.Constant(
            Activator.CreateInstance( typeof(Query<>).MakeGenericType( ReturnType ), provider ) );
         //return methodCall.Arguments[0];
      }

      private Expression VisitSingle( MethodCallExpression methodCall )
      {
         if(methodCall.Arguments.Count > 1)
         {
            VisitWhere( methodCall );
            if(fallback)
            {
               return methodCall;
            }
         }

         Take = 2;
         return methodCall;
      }

      private Expression VisitSkip( MethodCallExpression methodCall )
      {
         ConstantExpression ce = (ConstantExpression)methodCall.Arguments[1];
         Skip = (Int32)ce.Value;

         return methodCall.Arguments[0];
      }

      private Expression VisitTake( MethodCallExpression methodCall )
      {
         ConstantExpression ce = (ConstantExpression)methodCall.Arguments[1];
         Take = (Int32)ce.Value;

         return methodCall.Arguments[0];
      }

      private Expression VisitWhere( MethodCallExpression methodCall )
      {
         Expression expression = ExpressionEvaluator.Evaluate( methodCall );

         var eval = new WhereEvaluator( tableFinder, Table );
         expression = eval.Evaluate( expression );

         if(eval.Fallback || eval.Table == null || (Table != null && eval.Table != Table))
         {
            fallback = true;
            return methodCall;
         }

         if(Table == null)
         {
            Table = eval.Table;
         }

         arguments.AddRange( eval.Arguments );
         if(queryBuilder.Length > 0)
         {
            queryBuilder.Append( " AND " );
         }

         queryBuilder.Append( eval.QueryString );

         return methodCall.Arguments[0];
      }

      private class WhereEvaluator : ExpressionVisitor
      {
         private readonly List<String> arguments = new List<String>();
         private readonly ITableFinder tableFinder;
         private StringBuilder builder = new StringBuilder();
         private ContentResolverColumnMapping currentMap;
         private TableFindResult table;

         public WhereEvaluator( ITableFinder tableFinder, Uri existingTable )
         {
            this.tableFinder = tableFinder;
            if(existingTable != null)
            {
               table = new TableFindResult( existingTable, null );
            }
         }

         public List<String> Arguments
         {
            get { return arguments; }
         }

         public Boolean Fallback { get; private set; }

         public String QueryString
         {
            get { return builder.ToString(); }
         }

         public Uri Table
         {
            get { return table.Table; }
         }

         public Expression Evaluate( Expression expression )
         {
            expression = Visit( expression );

            if(!Fallback && table != null && table.MimeType != null)
            {
               builder.Insert( 0, String.Format( "(({0} = ?) AND ", ContactsContract.DataColumns.Mimetype ) );
               builder.Append( ")" );

               arguments.Insert( 0, table.MimeType );
            }

            return expression;
         }

         protected override Expression VisitBinary( BinaryExpression binary )
         {
            String current = builder.ToString();
            builder = new StringBuilder();

            Visit( binary.Left );
            if(Fallback)
            {
               return binary;
            }

            String left = builder.ToString();
            builder = new StringBuilder();

            String joiner;
            switch(binary.NodeType)
            {
               case ExpressionType.AndAlso:
                  joiner = " AND ";
                  break;

               case ExpressionType.OrElse:
                  joiner = " OR ";
                  break;

               case ExpressionType.Equal:
                  joiner = " = ";
                  break;

               case ExpressionType.GreaterThan:
                  joiner = " > ";
                  break;

               case ExpressionType.LessThan:
                  joiner = " < ";
                  break;

               case ExpressionType.NotEqual:
                  joiner = " IS NOT ";
                  break;

               default:
                  Fallback = true;
                  return binary;
            }

            Visit( binary.Right );
            if(Fallback)
            {
               if(binary.NodeType == ExpressionType.AndAlso)
               {
                  Fallback = false;
                  builder = new StringBuilder( current );
                  builder.Append( "(" );
                  builder.Append( left );
                  builder.Append( ")" );
                  return binary.Right;
               }
               else
               {
                  return binary;
               }
            }

            String right = builder.ToString();

            builder = new StringBuilder( current );
            builder.Append( "(" );
            builder.Append( left );
            builder.Append( joiner );
            builder.Append( right );
            builder.Append( ")" );

            return binary;
         }

         protected override Expression VisitConstant( ConstantExpression constant )
         {
            if(constant.Value is IQueryable)
            {
               return constant;
            }

            if(constant.Value == null)
            {
               builder.Append( "NULL" );
            }
            else
            {
               Object value = constant.Value;
               if(currentMap != null && currentMap.ValueToQueryable != null)
               {
                  value = currentMap.ValueToQueryable( value );
               }

               switch(Type.GetTypeCode( value.GetType() ))
               {
                  case TypeCode.Object:
                     Fallback = true;
                     return constant;

                  case TypeCode.Boolean:
                     arguments.Add( (Boolean)value ? "1" : "0" );
                     builder.Append( "?" );
                     break;

                  default:
                     arguments.Add( value.ToString() );
                     builder.Append( "?" );
                     break;
               }
            }

            return base.VisitConstant( constant );
         }

         protected override Expression VisitMember( MemberExpression memberExpression )
         {
            TableFindResult result = tableFinder.Find( memberExpression );
            if(table == null)
            {
               table = result;
            }
            else if(Table != result.Table || result.MimeType != table.MimeType)
            {
               Fallback = true;
               return memberExpression;
            }

            ContentResolverColumnMapping cmap = tableFinder.GetColumn( memberExpression.Member );
            if(cmap == null || cmap.Columns == null)
            {
               Fallback = true;
               return memberExpression;
            }

            currentMap = cmap;

            if(cmap.Columns.Length == 1)
            {
               builder.Append( cmap.Columns[0] );
            }
            else
            {
               throw new NotSupportedException();
            }

            return base.VisitMember( memberExpression );
         }
      }
   }
}