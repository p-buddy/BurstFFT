using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine.Assertions;

namespace JamUp.StringUtility
{
    public static class ToStringHelper
    {
        private readonly struct Member
        {
            public Type Type { get; }
            public object Value { get; }
            public string Name { get; }
            
            public Member(FieldInfo fieldInfo, object parent)
            {
                Type = fieldInfo.FieldType;
                Value = fieldInfo.GetValue(parent);
                Name = fieldInfo.Name;
            }
            
            public Member(PropertyInfo propertyInfo, object parent)
            {
                Type = propertyInfo.PropertyType;
                Value = propertyInfo.GetValue(parent);
                Name = propertyInfo.Name;
            }
        }
        
        private static Dictionary<Type, PropertyInfo[]> PublicPropertiesByType;
        private static Dictionary<Type, FieldInfo[]> PublicFieldsByType;
        private static Dictionary<Type, bool> DoesDeclareToStringByType;
        private static Dictionary<Type, MethodInfo> GenericNameAndPublicDataMethodByType;
        private static string Tab = "\t";
        private static string NewLine = "\n";

        static ToStringHelper()
        {
            PublicPropertiesByType = new Dictionary<Type, PropertyInfo[]>();
            PublicFieldsByType = new Dictionary<Type, FieldInfo[]>();
            DoesDeclareToStringByType = new Dictionary<Type, bool>();
            GenericNameAndPublicDataMethodByType = new Dictionary<Type, MethodInfo>();
        }
        
        public static string NameAndPublicData<T>(T obj, bool oneLine, int recursionDepth = 0)
        {
            string indentation = oneLine ? null : String.Join("", Enumerable.Repeat(Tab, recursionDepth));
            string entryDelimiter = oneLine ? null : $"{NewLine}{indentation}{Tab}";;
            
            Type type = typeof(T);
            StringBuilder stringBuilder = new StringBuilder($"{type.Name}");
            string bracketSpacing = oneLine ? " " : NewLine;
            stringBuilder.Append($"{bracketSpacing}{indentation}{{ ");

            Member[] properties = GetPublicProperties<T>().Select(propertyInfo => new Member(propertyInfo, obj)).ToArray();
            Member[] fields = GetPublicFields<T>().Select(fieldInfo => new Member(fieldInfo, obj)).ToArray();
            
            List<Member> members = new List<Member>(properties.Length + fields.Length);
            members.AddRange(properties);
            members.AddRange(fields);

            List<String> data = new List<string>(members.Count * 2);
            foreach (Member member in members)
            {
                data.AddIfNotNullOrEmpty(entryDelimiter);
                if (CanBeTurnedIntoString(in member))
                {
                    data.Add($"{member.Name}: {member.Value}");
                }
                else
                {
                    string nested = NameAndPublicDataForRunTimeType(member.Type, member.Value, oneLine, recursionDepth + 1);
                    data.Add($"{member.Name}: {nested}");
                }
            }

            stringBuilder.Append(String.Join(oneLine ? "; " : "", data));
            stringBuilder.Append($"{bracketSpacing}{indentation}}}");
            return stringBuilder.ToString();
        }

        private static PropertyInfo[] GetPublicProperties<T>()
        {
            if (!PublicPropertiesByType.TryGetValue(typeof(T), out PropertyInfo[] propertyInfos))
            {
                propertyInfos = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
                PublicPropertiesByType[typeof(T)] = propertyInfos;
            }

            return propertyInfos;
        }
        
        private static FieldInfo[] GetPublicFields<T>()
        {
            if (!PublicFieldsByType.TryGetValue(typeof(T), out FieldInfo[] fieldInfos))
            {
                fieldInfos = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
                PublicFieldsByType[typeof(T)] = fieldInfos;
            }

            return fieldInfos;
        }

        private static string NameAndPublicDataForRunTimeType(Type type, object value, bool oneLine, int recursionDepth)
        {
            if (!type.IsValueType && value == null)
            {
                return "null";
            }
            
            if (!GenericNameAndPublicDataMethodByType.TryGetValue(type, out MethodInfo nameAndPublicDataForType))
            {
                nameAndPublicDataForType = typeof(ToStringHelper).GetMethod(nameof(NameAndPublicData))?.MakeGenericMethod(type);
                Assert.IsNotNull(nameAndPublicDataForType);
                GenericNameAndPublicDataMethodByType[type] = nameAndPublicDataForType;
            }
            return nameAndPublicDataForType.Invoke(null, new [] { value, oneLine, recursionDepth }) as string;
        }

        private static bool CanBeTurnedIntoString(in Member member)
        {
            if (member.Type.IsPrimitive || member.Type.IsEnum)
            {
                return true;
            }

            if (!member.Type.IsValueType && member.Value is null)
            {
                return false;
            }

            if (DoesDeclareToStringByType.TryGetValue(member.Type, out bool doesDeclare))
            {
                return doesDeclare;
            }

            MethodInfo toStringMethodInfo = member.Type.GetMethod(nameof(ToString), BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            Assert.IsNotNull(toStringMethodInfo);
            bool doesOverrideToString = toStringMethodInfo.DeclaringType == member.Type;
            DoesDeclareToStringByType[member.Type] = doesOverrideToString;
            return doesOverrideToString;
        }

        private static void AddIfNotNullOrEmpty(this List<string> strings, string toAdd)
        {
            if (String.IsNullOrEmpty(toAdd))
            {
                return;
            }
            
            strings.Add(toAdd);
        }
    }
}