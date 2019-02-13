using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HousingFunctions
{
    public static class JavaScriptSerializerObjectExtensions
    {
        public static object JsonPropertyValue(this object obj, string key)
        {
            object val;
            if (!obj.ToDictionary().TryGetValue(key, out val))
                return null;
            return val;
        }

        public static IEnumerable<string> JsonPropertyNames(this object obj)
        {
            var dict = obj.ToDictionary();
            if (dict == null)
                return null;
            return dict.Keys;
        }
    }
}
