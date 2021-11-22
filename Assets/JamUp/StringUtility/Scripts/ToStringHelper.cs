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
            public string Name { get; }
            public bool IsPublic { get; }
            public bool IsBackingField { get; }
            public object ValueOnObject(object obj) => getValue.Invoke(obj);
            private readonly Func<object, object> getValue;
            
            public Member(FieldInfo fieldInfo)
            {
                Type = fieldInfo.FieldType;
                Name = fieldInfo.Name;
                IsPublic = fieldInfo.IsPublic;
                getValue = fieldInfo.GetValue;
                IsBackingField = Name.ToLower().Contains(BackingFieldIdentifier.ToLower());
            }
            
            public Member(PropertyInfo propertyInfo)
            {
                Type = propertyInfo.PropertyType;
                Name = propertyInfo.Name;
                IsPublic = propertyInfo.GetAccessors().Any(accessor => accessor.IsPublic);
                getValue = propertyInfo.GetValue;
                IsBackingField = false;
            }
        }
        
        private static Dictionary<Type, bool> DoesDeclareToStringByType;
        private static Dictionary<Type, MethodInfo> GenericNameAndDataMethodByType;
        private static Dictionary<Type, Member[]> MembersByType;

        private const string BackingFieldIdentifier = "BackingField";
        private const string Tab = "\t";
        private const string NewLine = "\n";

        static ToStringHelper()
        {
            MembersByType = new Dictionary<Type, Member[]>();
            DoesDeclareToStringByType = new Dictionary<Type, bool>();
            GenericNameAndDataMethodByType = new Dictionary<Type, MethodInfo>();
            
        }
        
        public static string NameAndPublicData<T>(this T obj, bool oneLine)
        {
            return NameAndData(obj, oneLine, true);
        }
        
        public static string NameAndAllData<T>(this T obj, bool oneLine)
        {
            return NameAndData(obj, oneLine, false);
        }

        #region Private
        
        private static string NameAndData<T>(T obj, bool oneLine, bool publicOnly, int recursionDepth = 0)
        {
            string indentation = oneLine ? null : String.Join("", Enumerable.Repeat(Tab, recursionDepth));
            string entryDelimiter = oneLine ? null : $"{NewLine}{indentation}{Tab}";;
            
            Type type = typeof(T);
            StringBuilder stringBuilder = new StringBuilder($"{type.Name}");
            string bracketSpacing = oneLine ? " " : NewLine;
            stringBuilder.Append($"{bracketSpacing}{indentation}{{ ");

            Member[] members = publicOnly
                ? GetMembers<T>().Where(member => member.IsPublic).ToArray()
                : GetMembers<T>().Where(member => !member.IsBackingField).ToArray();

            List<String> data = new List<string>(members.Length * 2);
            foreach (Member member in members)
            {
                data.AddIfNotNullOrEmpty(entryDelimiter);
                object value = member.ValueOnObject(obj);
                if (CanBeTurnedIntoString(in member, value))
                {
                    data.Add($"{member.Name}: {value}");
                }
                else
                {
                    string nested = NameAndDataForRunTimeType(member.Type, value, oneLine, publicOnly, recursionDepth + 1);
                    data.Add($"{member.Name}: {nested}");
                }
            }

            stringBuilder.Append(String.Join(oneLine ? "; " : "", data));
            stringBuilder.Append($"{bracketSpacing}{indentation}}}");
            return stringBuilder.ToString();
        }

        private static Member[] GetMembers<T>()
        {
            if (!MembersByType.TryGetValue(typeof(T), out Member[] members))
            {
                PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                List<Member> membersList = new List<Member>(properties.Length + fields.Length);
                membersList.AddRange(properties.Select(property => new Member(property)));
                membersList.AddRange(fields.Select(field => new Member(field)));
                members = membersList.ToArray();
                MembersByType[typeof(T)] = members;
            }

            return members;
        }

        private static string NameAndDataForRunTimeType(Type type, object value, bool oneLine, bool publicOnly, int recursionDepth)
        {
            if (!type.IsValueType && value == null)
            {
                return "null";
            }
            
            if (!GenericNameAndDataMethodByType.TryGetValue(type, out MethodInfo nameAndDataForType))
            {
                nameAndDataForType = typeof(ToStringHelper).GetMethod(nameof(NameAndData), BindingFlags.NonPublic | BindingFlags.Static)?.MakeGenericMethod(type);
                Assert.IsNotNull(nameAndDataForType);
                GenericNameAndDataMethodByType[type] = nameAndDataForType;
            }
            return nameAndDataForType.Invoke(null, new [] { value, oneLine, publicOnly, recursionDepth }) as string;
        }

        private static bool CanBeTurnedIntoString(in Member member, object value)
        {
            if (member.Type.IsPrimitive || member.Type.IsEnum)
            {
                return true;
            }

            if (!member.Type.IsValueType && value is null)
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
        
        #endregion
    }
}