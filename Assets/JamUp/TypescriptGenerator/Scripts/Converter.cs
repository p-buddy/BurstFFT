using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace JamUp.TypescriptGenerator.Scripts
{
    public static class Converter
    {
        private static Dictionary<Type, List<PropertyInfo>> PropertiesByType;

        static Converter()
        {
            PropertiesByType = new Dictionary<Type, List<PropertyInfo>>();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T To<T>(ExpandoObject obj) where T : new()
        {
            return (T)To(obj, typeof(T), _ => new T());
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsValue"></param>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public static object ConvertEnumValueBack(string jsValue, Type enumType)
        {
            return Enum.Parse(enumType, jsValue);
        }

        private static bool IsDirectlyConvertible(Type type) =>
            type.IsPrimitive || typeof(string).IsAssignableFrom(type);
        

        private static object As(this object obj, Type type) => Convert.ChangeType(obj, type);

        private static object To(ExpandoObject obj, Type type, Func<Type, object> constructor)
        {
            if (IsDirectlyConvertible(type))
            {
                return obj.As(type);
            }
            
            IDictionary<string, object> valueByProperty = obj;
            object container = constructor.Invoke(type);

            foreach (KeyValuePair<string, object> kvp in valueByProperty)
            {
                string propertyName = kvp.Key;
                if (TryMatchProperty(propertyName, type, out PropertyInfo propertyInfo))
                {
                    Type propertyType = propertyInfo.PropertyType;
                    if (IsDirectlyConvertible(propertyType))
                    {
                        propertyInfo.SetValue(container, kvp.Value.As(propertyType));
                        continue;
                    }

                    if (propertyType.IsEnum)
                    {
                        string entry = kvp.Value as string;
                        propertyInfo.SetValue(container, ConvertEnumValueBack(entry, propertyType).As(propertyType));
                        continue;
                    }

                    if (propertyType.IsArray)
                    {
                        Type elementType = propertyInfo.PropertyType.GetElementType();
                        Array input = kvp.Value as Array;
                        Array converted = Array.CreateInstance(elementType, input.Length);
                        for (int i = 0; i < input.Length; i++)
                        {
                            ExpandoObject item = input.GetValue(i) as ExpandoObject;
                            converted.SetValue(To(item, elementType, Activator.CreateInstance).As(elementType), i); 
                        }

                        propertyInfo.SetValue(container, converted);
                        continue;
                    }
                    
                    propertyInfo.SetValue(container, To(kvp.Value as ExpandoObject, propertyType, Activator.CreateInstance));
                    continue;
                }

                throw new Exception($"Could not match property for: {propertyName}");
            }

            return container;
        }
        
        private static bool TryMatchProperty(string name, Type type, out PropertyInfo propertyInfo)
        {
            propertyInfo = null;
            if (!PropertiesByType.TryGetValue(type, out List<PropertyInfo> properties))
            {
                properties = type.GetProperties().ToList();
            }
            
            string camel = AsCamelCase(name);
            string pascal = AsPascalCase(name);
            
            foreach (PropertyInfo property in properties)
            {
                if (property.Name == camel || property.Name == pascal)
                {
                    propertyInfo = property;
                    return true;
                }
            }

            return false;
        }

        private static string AsPascalCase(string s) => Char.ToUpper(s[0]) + s.Substring(1);
        private static string AsCamelCase(string s) => Char.ToLower(s[0]) + s.Substring(1);


    }
}