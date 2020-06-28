using Com.Mapbox.Mapboxsdk.Style.Expressions;
using Newtonsoft.Json;
using NxExpressions = FindAndExplore.Mapping.Expressions;

namespace FindAndExplore.Droid.Mapping
{
    public static class ExpressionExtensions
    {
        public static Expression ToNative(this NxExpressions.Expression expression)
        {
            var json = JsonConvert.SerializeObject(expression.ToArray());
            System.Diagnostics.Debug.WriteLine(json);
            return Expression.Raw(json);
        }
    }
}