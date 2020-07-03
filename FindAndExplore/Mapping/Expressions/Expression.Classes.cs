using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace FindAndExplore.Mapping.Expressions
{
    /**
     * ExpressionLiteral wraps an object to be used as a literal in an expression.
     * <p>
     * ExpressionLiteral is created with {@link #Literal(double)}, {@link #Literal(bool)},
     * {@link #Literal(string)} and {@link #Literal(object)}.
     * </p>
     */
    public class ExpressionLiteral<T> : Expression, IValueExpression, IEquatable<ExpressionLiteral<T>>
    {
        public T Value { get; private set; }

        public ExpressionLiteral(T @object)
        {
            Value = @object;
        }
        /**
         * Get the literal object.
         *
         * @return the literal object
         */

        public object ToValue()
        {
            if (Value is IValueExpression valueExpression)
            {
                return valueExpression.ToValue();
            }
            return Value;
        }
        
        public override object[] ToArray()
        {
            return new object[] { "literal", Value };
        }

        /**
         * Returns a string representation of the expression literal.
         *
         * @return a string representation of the object.
         */

        public override string ToString()
        {
            string @string;
            if (Value is string)
            {
                @string = "\"" + Value + "\"";
            }
            else
            {
                @string = Value.ToString();
            }
            return @string;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is ExpressionLiteral<T> expressionLiteral)
            {
                return Equals(expressionLiteral);
            }

            return false;
        }

        /**
         * Returns a hash code value for the expression literal.
         *
         * @return a hash code value for this expression literal
         */
        public override int GetHashCode()
        {
            int result = base.GetHashCode();
            result = 31 * result + (Value != null ? Value.GetHashCode() : 0);
            return result;
        }

        public bool Equals(ExpressionLiteral<T> other)
        {
            if (other == null) return false;

            if (base.Equals(other) == false) return false;

            return Equals(Value, other.Value);
        }
    }

    public partial class ExpressionVisibility : ExpressionLiteral<string>
    {
        public static ExpressionVisibility VISIBLE = new ExpressionVisibility("visible");
        public static ExpressionVisibility NONE = new ExpressionVisibility("none");
        
        public ExpressionVisibility(string @object) : base(@object)
        {
        }
        
        public object GetValue()
        {
            #if __IOS__
            // iOS-specific code
                return Value == "visible";
            #endif
            
            return Value;
        }
    }
    
    /**
     * Expression interpolator type.
     * <p>
     * Is used for first parameter of {@link #interpolate(Interpolator, Expression, params Stop[])}.
     * </p>
     */
    public sealed class Interpolator : Expression
    {
        public Interpolator(string @operator, params Expression[] arguments) : base(@operator, arguments) { }
    }

    /**
     * Expression stop type.
     * <p>
     * Can be used for {@link #stop(object, object)} as part of varargs parameter in
     * {@link #step(double, Expression, params Stop[])} or {@link #interpolate(Interpolator, Expression, params Stop[])}.
     * </p>
     */
    public sealed class Stop
    {
        readonly object value;
        readonly object output;

        public Stop(object value, object output)
        {
            this.value = value;
            this.output = output;
        }

        /**
         * Converts a varargs of Stops to a Expression array.
         *
         * @param stops the stops to convert
         * @return the converted stops as an expression array
         */

        public static Expression[] ToExpressionArray(params Stop[] stops)
        {
            Expression[] expressions = new Expression[stops.Length * 2];
            for (int i = 0; i < stops.Length; i++)
            {
                expressions[i * 2] = stops[i].value is Expression valueExpression
                    ? valueExpression
                    : Expression.Literal(stops[i].value);
                expressions[i * 2 + 1] = stops[i].output is Expression outputExpression
                    ? outputExpression
                    : Expression.Literal(stops[i].output);
            }
            return expressions;
        }
    }

    /**
     * Holds format entries used in a {@link #Format(FormatEntry...)} expression.
     */
    public sealed class FormatEntry
    {

        public Expression Text { get; private set; }

        public FormatOption[] Options { get; private set; }

        public FormatEntry(Expression text, FormatOption[] options)
        {
            this.Text = text;
            this.Options = options;
        }
    }

    /**
     * Holds format options used in a {@link #formatEntry(Expression, FormatOption...)} that builds
     * a {@link #Format(FormatEntry...)} expression.
     * <p>
     * If an option is not set, it defaults to the base value defined for the symbol.
     */
    public sealed class FormatOption
    {

        public string Type { get; private set; }

        public Expression Value { get; private set; }

        FormatOption(string type, Expression value)
        {
            this.Type = type;
            this.Value = value;
        }

        /**
         * If set, the font-scale argument specifies a scaling factor relative to the text-size
         * specified in the root layout properties.
         * <p>
         * "font-scale" is required to be of a resulting type number.
         *
         * @param expression expression
         * @return format option
         */

        public static FormatOption FormatFontScale(Expression expression)
        {
            return new FormatOption("font-scale", expression);
        }

        /**
         * If set, the font-scale argument specifies a scaling factor relative to the text-size
         * specified in the root layout properties.
         * <p>
         * "font-scale" is required to be of a resulting type number.
         *
         * @param scale value
         * @return format option
         */

        public static FormatOption FormatFontScale(double scale)
        {
            return new FormatOption("font-scale", Expression.Literal(scale));
        }

        /**
         * If set, the text-font argument overrides the font specified by the root layout properties.
         * <p>
         * "text-font" is required to be a literal array.
         * <p>
         * The requested font stack has to be a part of the used style.
         * For more information see <a href="https://www.mapbox.com/help/define-font-stack/">the documentation</a>.
         *
         * @param expression expression
         * @return format option
         */

        public static FormatOption FormatTextFont(Expression expression)
        {
            return new FormatOption("text-font", expression);
        }

        /**
         * If set, the text-font argument overrides the font specified by the root layout properties.
         * <p>
         * "text-font" is required to be a literal array.
         * <p>
         * The requested font stack has to be a part of the used style.
         * For more information see <a href="https://www.mapbox.com/help/define-font-stack/">the documentation</a>.
         *
         * @param fontStack value
         * @return format option
         */

        public static FormatOption FormatTextFont(string[] fontStack)
        {
            return new FormatOption("text-font", Expression.Literal(fontStack));
        }

        /**
         * If set, the text-color argument overrides the color specified by the root paint properties.
         *
         * @param expression expression
         * @return format option
         */

        public static FormatOption FormatTextColor(Expression expression)
        {
            return new FormatOption("text-color", expression);
        }

        /**
         * If set, the text-color argument overrides the color specified by the root paint properties.
         *
         * @param color value
         * @return format option
         */
        public static FormatOption FormatTextColor(Color color)
        {
            return new FormatOption("text-color", Expression.Color(color));
        }
    }

    
    public sealed class ExpressionLiteralArray<T> : ExpressionLiteral<IEnumerable<T>>, IEquatable<ExpressionLiteralArray<T>>
    {

        /**
         * Create an expression literal.
         *
         * @param object the object to be treated as literal
         */
        public ExpressionLiteralArray(IEnumerable<T> @object) : base(@object)
        {
        }

        /**
         * Convert the expression array to a string representation.
         *
         * @return the string representation of the expression array
         */

        public override string ToString()
        {
            var array = Value?.ToArray() ?? new T[0];
            var builder = new StringBuilder("[");
            for (int i = 0; i < array.Length; i++)
            {
                object argument = array[i];
                if (argument is string)
                {
                    builder.Append("\"").Append(argument).Append("\"");
                }
                else
                {
                    builder.Append(argument);
                }

                if (i != array.Length - 1)
                {
                    builder.Append(", ");
                }
            }
            builder.Append("]");
            return builder.ToString();
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is ExpressionLiteralArray<T> expressionLiteralArray)
            {
                // TODO Ensure arrays are equal
                return Equals(expressionLiteralArray);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(ExpressionLiteralArray<T> other)
        {
            if (other == null) return false;

            if (base.Equals(other) == false) return false;

            var left = Value;
            var right = other.Value;

            return Enumerable.SequenceEqual(left, right);
        }
    }

    /**
     * Wraps an expression value stored in a Map.
     */
    public sealed class ExpressionMap : Expression, IValueExpression, IEquatable<ExpressionMap>
    {
        public Dictionary<string, Expression> Map { get; private set; }

        public ExpressionMap(Dictionary<string, Expression> map)
        {
            Map = map;
        }

        public object ToValue()
        {
            var unwrappedMap = new Dictionary<string, object>();

            foreach (string key in Map.Keys)
            {
                var expression = Map[key];

                switch (expression)
                {
                    case IValueExpression valueExpression:
                        unwrappedMap[key] = valueExpression.ToValue();
                        break;
                    default:
                        unwrappedMap[key] = expression.ToArray();
                        break;
                }
            }

            return unwrappedMap;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("{");
            foreach (string key in Map.Keys)
            {
                builder.Append("\"").Append(key).Append("\": ");
                builder.Append(Map[key]);
                builder.Append(", ");
            }

            if (Map.Count > 0)
            {
                builder.Remove(builder.Length - 2, builder.Length);
            }

            builder.Append("}");
            return builder.ToString();
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (!base.Equals(obj))
            {
                return false;
            }

            if (obj is ExpressionMap expressionMap)
            {
                return Equals(expressionMap);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int result = base.GetHashCode();
            result = 31 * result + (Map == null ? 0 : Map.GetHashCode());
            return result;
        }

        public bool Equals(ExpressionMap other)
        {
            if (other == null) return false;

            return ReferenceEquals(Map, other.Map);
        }
    }

    /**
     * Interface used to describe expressions that hold a Java value.
     */
    interface IValueExpression
    {
        object ToValue();
    }
}