using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using TsGenerator = JamUp.TypescriptGenerator.Scripts.Generator ;

namespace JamUp.Waves
{
    public static class Converter
    {
        private static Dictionary<Type, List<PropertyInfo>> PropertiesByType;
        
        static Converter()
        {
            PropertiesByType = new Dictionary<Type, List<PropertyInfo>>
            {
                { typeof(KeyFrame), typeof(KeyFrame).GetProperties().ToList() },
                { typeof(WaveState), typeof(WaveState).GetProperties().ToList() },
                { typeof(SimpleFloat3), typeof(SimpleFloat3).GetProperties().ToList() },
            };
        }

        public static KeyFrame ToKeyFrame(ExpandoObject obj)
        {
            IDictionary<string, object> valueByProperty = obj;
            object frame = new KeyFrame();

            foreach (KeyValuePair<string, object> kvp in valueByProperty)
            {
                string propertyName = kvp.Key;
                if (TryMatchProperty<KeyFrame>(propertyName, out PropertyInfo propertyInfo))
                {
                    if (IsPrimitiveType(propertyInfo))
                    {
                        propertyInfo.SetValue(frame, Convert.ChangeType(kvp.Value, propertyInfo.PropertyType));
                        continue;
                    }

                    if (propertyInfo.PropertyType.IsArray)
                    {
                        Type elementType = propertyInfo.PropertyType.GetElementType();
                        Array input = kvp.Value as Array;
                        Array converted = Array.CreateInstance(elementType, input.Length);
                        for (int i = 0; i < input.Length; i++)
                        {
                            var x = input.GetValue(i);
                            converted.SetValue(ToWaveState(x), i); 
                        }

                        propertyInfo.SetValue(frame, converted);
                    }
                    
                    continue;
                }

                throw new Exception($"Could not match property for: {propertyName}");
            }

            return (KeyFrame)frame;
        }

        public static WaveState ToWaveState(object obj)
        {
            IDictionary<string, object> valueByProperty = obj as ExpandoObject;
            object state = new WaveState();
            foreach (KeyValuePair<string, object> kvp in valueByProperty)
            {
                string propertyName = kvp.Key;
                if (TryMatchProperty<WaveState>(propertyName, out PropertyInfo propertyInfo))
                {
                    if (IsPrimitiveType(propertyInfo))
                    {
                        propertyInfo.SetValue(state, Convert.ChangeType(kvp.Value, propertyInfo.PropertyType));
                        continue;
                    }

                    if (IsEnumType(propertyInfo))
                    {
                        propertyInfo.SetValue(state, TsGenerator.ConvertEnumValueBack<WaveType>(kvp.Value as string));
                    }
                }
            }

            return (WaveState)state;
        }

        private static bool IsPrimitiveType(PropertyInfo propertyInfo)
        {
            Type[] primitiveTypes = { typeof(int), typeof(float), typeof(string), typeof(double) };
            return IsType(propertyInfo.PropertyType, primitiveTypes);
        }
        
        private static bool IsEnumType(PropertyInfo propertyInfo)
        {
            return IsType(propertyInfo.PropertyType, typeof(Enum));
        }
        
        private static bool IsType(Type queryType, params Type[] types)
        {
            foreach (Type type in types)
            {
                if (type.IsAssignableFrom(queryType)) return true;
            }

            return false;
        }
        
        private static bool TryMatchProperty<T>(string name, out PropertyInfo propertyInfo)
        {
            propertyInfo = null;
            bool keyExists = PropertiesByType.TryGetValue(typeof(T), out List<PropertyInfo> properties);
            
            if (!keyExists) return false;

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


        public static Wave FromState(WaveState waveState) =>
            new Wave(waveState.WaveType, waveState.Frequency, waveState.PhaseDegrees, waveState.Amplitude);
    }
}