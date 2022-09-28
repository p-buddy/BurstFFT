using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using pbuddy.StringUtility.RuntimeScripts;

namespace JamUp.Waves.Scripts.API
{
    /// <summary>
    /// <example>
    /// +=1 t=1 th=0.1 s=1000 pr=per
    /// f=1 p=0 a=1 w=sine d=1,0,0
    /// f=1 p=90 a=1 w=sine d=0,1,0
    /// </example>
    /// </summary>
    public static class BareBonesAPI
    {
        private static class Parse
        {
            internal static object Int(string query) => Int32.Parse(query);
            internal static object Float(string query) => float.Parse(query);
            
            internal static object _Enum<T>(string query)
            {
                var names = Enum.GetNames(typeof(T)).ToList().Select(s => $"{s.ToLower().Substring(0, 2)}").ToList();
                var types = Enum.GetValues(typeof(T)) as T[];
                var index = names.IndexOf(query);
                if (index < 0 || index >= names.Count)
                {
                    var msg =
                        $"'{query}' could not be matched to entry in enum of type {typeof(T)}. Valid entries are: {String.Join(", ", names)}";
                    throw new ArgumentException(msg);
                }
                return types[names.IndexOf(query)];
            }
            
            internal static object Vector(string query)
            {
                var coords = query.Split(',').ToList().Select(float.Parse).ToArray();
                return new SimpleFloat3(coords[0], coords[1], coords[2]);
            }
        }

        private static readonly Dictionary<Type, Dictionary<string, (string, Func<string, object>)>> map =
            new Dictionary<Type, Dictionary<string, (string, Func<string, object>)>>
            {
                {
                    typeof(KeyFrame), 
                    new Dictionary<string, (string, Func<string, object>)>
                    {
                        {NewFrameKey, (nameof(KeyFrame.Duration), Parse.Float)},
                        {"th", (nameof(KeyFrame.Thickness), Parse.Float)},
                        {"t", (nameof(KeyFrame.SignalLength), Parse.Float)},
                        {"s", (nameof(KeyFrame.SampleRate), Parse.Int)},
                        {"pr", (nameof(KeyFrame.ProjectionType), Parse._Enum<ProjectionType>)}
                    }
                },
                {
                    typeof(WaveState), new Dictionary<string, (string, Func<string, object>)>
                    {
                        {"f", (nameof(WaveState.Frequency), Parse.Float)},
                        {"p", (nameof(WaveState.PhaseDegrees), Parse.Float)},
                        {"w", (nameof(WaveState.WaveType), Parse._Enum<WaveType>)},
                        {"a", (nameof(WaveState.Amplitude), Parse.Float)},
                        {"d", (nameof(WaveState.DisplacementAxis), Parse.Vector)},
                    }}
            };
        
        private const string NewFrameKey = "+";
        private const char KeyValueSeparator = '=';
        private const char TokenSeparator = ' ';

        private static string Identifier(string key) => $"{key}{KeyValueSeparator}";
        private static T GetValue<T>(string data, string key, Func<string, T> converter) =>
            converter(data.RemoveSubString(Identifier(key)));
        
        public static KeyFrame[] GetTestFrames(string content)
        {
            string[] lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            List<KeyFrame> frames = new List<KeyFrame>();
            List<List<WaveState>> waves = new List<List<WaveState>>();

            KeyFrame defaultFrame = new KeyFrame(1f, 1000, ProjectionType.Orthographic, 0.1f, null, 10f);
            WaveState defaultWave = new WaveState(1f, 1f, 0f, WaveType.Sine, new SimpleFloat3(1f, 0, 0));
            
            for (var index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                if (index == 0 || line.StartsWith(NewFrameKey))
                {
                    KeyFrame frame = ParseLine(line, defaultFrame);
                    frames.Add(frame);
                    continue;
                }

                WaveState state = ParseLine(line, defaultWave);

                if (waves.Count < frames.Count)
                {
                    waves.Add(new List<WaveState> { state });
                    continue;
                }

                waves[frames.Count - 1].Add(state);
            }

            return frames.Select((frame, index) => AddWaves(frame, waves[index])).ToArray();
        }

        private static T ParseLine<T>(string content, T @default)
        {
            object boxed = @default;
            foreach (string setting in content.Split(TokenSeparator))
            {
                var key = setting.Split(KeyValueSeparator)[0];
                if (map[typeof(T)].TryGetValue(key, out (string name, Func<string, object> converter) value))
                {
                    PropertyInfo info = typeof(T).GetProperty(value.name);
                    FieldInfo field = GetBackingField(typeof(T), info);
                    field.SetValue(boxed, GetValue(setting, key, value.converter));
                }
            }

            return (T)boxed;
        }

        private static KeyFrame AddWaves(in KeyFrame frame, List<WaveState> waves)
        {
            return new KeyFrame(frame.Duration, frame.SampleRate.Value, frame.ProjectionType.Value, frame.Thickness.Value, waves.ToArray(), frame.SignalLength.Value);
        }

        private static FieldInfo GetBackingField(Type type, PropertyInfo property)
        {
            return type
                   .GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                   .FirstOrDefault(field => field.Attributes.HasFlag(FieldAttributes.Private) &&
                                            field.Attributes.HasFlag(FieldAttributes.InitOnly) &&
                                            field.CustomAttributes.Any(attr => attr.AttributeType == typeof(CompilerGeneratedAttribute)) &&
                                            (field.DeclaringType == property.DeclaringType) &&
                                            field.FieldType.IsAssignableFrom(property.PropertyType) &&
                                            field.Name.StartsWith("<" + property.Name + ">"));
        }

    }
}