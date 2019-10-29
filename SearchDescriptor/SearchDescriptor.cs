using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SearchDescriptor
{
    public class SearchDescriptor<T>
    {
        #region construction
        public static SearchDescriptor<T> Create<TProperty>(Expression<Func<T, TProperty>> expression, Operand operand, TProperty value)
            => new SearchDescriptor<T>().Start(expression, operand, value);
        protected SearchDescriptor()
        {
            //properdies = typeof(T).GetProperties().ToList();
            Braskets = new List<int>();
            Parts = new List<SearchPart>();
        }
        #endregion

        #region private and protected
        protected SearchDescriptor<T> Start<TProperty>(Expression<Func<T, TProperty>> expression, Operand operand, TProperty value)
        {
            if (Parts.Count == 0)
                Parts.Add(new SearchPart
                {
                    Property = expression.GetPropertyInfo(),
                    Operand = operand,
                    Value = value
                });
            else
                throw new InvalidOperationException();
            Clauses.Add(Clause.Start);
            AddBraskets();
            return this;
        }

        private static readonly MethodInfo stringContains = typeof(string).GetMethod("Contains", new[] { typeof(string) });

        private static readonly Dictionary<Type, MethodInfo> equalityMethods = new Dictionary<Type, MethodInfo>
        {
            {typeof(string), typeof(string).GetMethod("Equals", new[] { typeof(string) }) }
        };
        protected List<Expression> Expressions => expressions ?? (expressions = new List<Expression>(Parts.Select(ExpressionFromPart)));
        private readonly ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "item");
        private List<Expression> expressions;
        private bool brasketFlag;
        #endregion

        #region public properties
        public int Skip { get; set; }
        public int Take { get; set; }
        public List<int> Braskets { get; }
        public List<SearchPart> Parts { get; }
        public List<Clause> Clauses { get; set; } = new List<Clause>();
        #endregion

        #region public methods
        public SearchDescriptor<T> And<TProperty>(Expression<Func<T, TProperty>> expression, Operand operand, TProperty value)
        {
            if (Parts.Count > 0)
            {
                Parts.Add(new SearchPart
                {
                    Property = expression.GetPropertyInfo(),
                    Operand = operand,
                    Value = value
                });
                Clauses.Add(Clause.And);
                AddBraskets();
            }
            else
                throw new InvalidOperationException();
            return this;
        }

        public SearchDescriptor<T> Or<TProperty>(Expression<Func<T, TProperty>> expression, Operand operand, TProperty value)
        {
            if (Parts.Count > 0)
            {
                Parts.Add(new SearchPart
                {
                    Property = expression.GetPropertyInfo(),
                    Operand = operand,
                    Value = value
                });
                Clauses.Add(Clause.Or);
                AddBraskets();
            }
            else
                throw new InvalidOperationException();
            return this;
        }

        public SearchDescriptor<T> OpenBrasket(int i = 1)
        {
            AddBraskets(i, 0);
            brasketFlag = true;
            return this;
        }
        public SearchDescriptor<T> CloseBrasket(int i = 1)
        {
            Braskets[Braskets.Count-1] += i;
            return this;
        }

        public Expression<Func<T, bool>> ToExpression()
        {
            expressions = new List<Expression>(Parts.Select(ExpressionFromPart));
            return Expression.Lambda<Func<T, bool>>(JoinExpression(0, Parts.Count), parameterExpression);
        }

        #endregion

        #region private methods
        private void AddBraskets(int opening = 0, int closing = 0)
        {
            if (brasketFlag)
            {
                brasketFlag = false;
                return;
            }
            if (closing > 0 && closing > opening + BrasketsSum())
                throw new ArgumentException();
            Braskets.Add(opening);
            Braskets.Add(closing);
        }

        private int BrasketsSum()
        {
            int sign = 1;
            return Braskets.Aggregate((o, c) => o + (c * (sign *= -1)));
        }

        private Expression ExpressionFromPart(SearchPart part)
        {
            var property = PropertyGetter(part.Property);
            var value = Expression.Constant(part.Value, part.Property.PropertyType);
            switch (part.Operand)
            {
                case Operand.Includes: return Expression.Call(property, stringContains, value);
                case Operand.NotIncludes: return Expression.Not(Expression.Call(property, stringContains, value));
                case Operand.Equal: return Expression.Equal(property, value);
                default: return Expression.Empty();
            }
        }

        private /*Lambda*/Expression PropertyGetter(PropertyInfo propertyInfo)
        {
            var property = Expression.Property(parameterExpression, propertyInfo);
            return property; 
            //var convert = Expression.TypeAs(property, propertyInfo.PropertyType);
            //return Expression.Lambda(property, parameterExpression);
        }

        private Expression ExpressionFromClause(Clause c, Expression left, Expression right)
        {
            switch (c)
            {
                case Clause.And: return Expression.AndAlso(left, right);
                case Clause.Or: return Expression.Or(left, right);
                default: return Expression.Empty();
            }
        }

        private int IndexOfClosingBrasket(List<int> arr, int startIndex)
        {
            if (startIndex % 2 == 1)
                throw new ArgumentException("Открывающие скобки находятся в чётных индексах!");
            int sum = arr[startIndex], sign = 1;
            while (sum > 0)
                sum += arr[++startIndex] * (sign = -1 * sign);
            return startIndex;
        }

        private int IndexOfFirstClosingBrasket(int indexOfOpeningBraskets)
        {
            for (var i = indexOfOpeningBraskets; i < Braskets.Count; i += 2)
            {
                if (Braskets[i] > 0) return i;
            }
            throw new Exception("Не удалось найти первую закрывающую скобку!");
        }

        private static Expression JoinOr(IEnumerable<Expression> expressions) => expressions.Aggregate(Expression.Or);

        private Expression JoinExpression(int from, int to)
        {
            if (Braskets.Skip(2 * from).Take(2 * to).All(b => b == 0))
                return joinSimpleSequence(from, to);
            int i;
            for (i = 2 * from; i < 2 * to && Braskets[i] == 0; i += 2) ;//нашли первую(ые) открывающую(ие) скобку(и)
            var j = IndexOfClosingBrasket(Braskets, i);// нашли индекс где закрываются все те скобки
            var left = Expressions.GetRange(0, i / 2);//взяли то что слева ото всех скобок (м.б. и ничего если скобки открываются сразу)
            Braskets[i]--;
            Braskets[j]--;
            left.Add(JoinExpression(i / 2, j / 2));//добавили результат соединения междускобочной части
            return joinSimpleSequence(left);
        }

        private Expression joinSimpleSequence(int start, int end)
        {
            var subset = expressions.Skip(start).Take(end).ToList();
            for (var j = 1; j < subset.Count; j++)
            {
                if (Clauses[start + j] == Clause.And)
                {
                    subset[j] = Expression.AndAlso(subset[j - 1], subset[j]);
                    subset[j - 1] = null;
                }
            }
            return JoinOr(subset.Where(e => e != null));
        }

        private Expression joinSimpleSequence(List<Expression> subset)
        {
            for (var j = 1; j < subset.Count; j++)
            {
                if (Clauses[j] == Clause.And)
                {
                    subset[j] = Expression.AndAlso(subset[j - 1], subset[j]);
                    subset[j - 1] = null;
                }
            }
            return JoinOr(subset.Where(e => e != null));
        }
        #endregion

        #region overrides
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0, j; i < Parts.Count; i++)
            {
                if (Clauses[i] != Clause.Start)
                {
                    sb.Append(' ');
                    sb.Append(Clauses[i]);
                    sb.Append(' ');
                }
                for (j = 0; j < Braskets[i * 2]; j++)
                    sb.Append('(');
                sb.Append(Parts[i]);
                for (j = 0; j < Braskets[i * 2 + 1]; j++)
                    sb.Append(')');
            }
            return sb.ToString();
        }
        #endregion
    }
}
