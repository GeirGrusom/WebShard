using System;
using System.Dynamic;
using System.Linq;
using WebShard.Routing;

namespace WebShard.Serialization.Form
{
    public class FormDeserializer 
    {
        public object Deserialize(string form)
        {
            return Deserialize(form, typeof (ExpandoObject));
        }

        public object Deserialize(string form, Type resultType)
        {
            var ctors = resultType.GetConstructors().OrderByDescending(o => o.GetParameters().Length);

            // At some point we sould like to support non-default constructor objects.

            var ctor = ctors.FirstOrDefault(c => c.GetParameters().Length == 0);
            if(ctor == null)
                throw new FormatException(string.Format("The type '{0}' must have a public default constructor.", resultType.Namespace + "." + resultType.Name));

            var queryString = QueryString.Parse(form);

            object result = Activator.CreateInstance(resultType);
            
            var properties = resultType.GetProperties()
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var item in queryString)
            {
                properties[item.Key].SetValue(result, Convert.ChangeType(item.Value, properties[item.Key].PropertyType));
            }
            return result;
        }

        public T Deserialize<T>(string form)
        {
            return (T)Deserialize(form, typeof(T));
        }
    }
}
