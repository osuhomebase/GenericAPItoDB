using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api2db
{
    public static class JavaScriptSerializerObjectExtensions
    {
        public static object JsonPropertyValue(this object obj, string key)
        {
            var dict = obj as IDictionary<string, object>;
            if (dict == null)
                return null;
            object val;
            if (!dict.TryGetValue(key, out val))
                return null;
            return val;
        }

        public static IEnumerable<string> JsonPropertyNames(this object obj)
        {
            var dict = obj as IDictionary<string, object>;
            if (dict == null)
                return null;
            return dict.Keys;
        }
    }
}
