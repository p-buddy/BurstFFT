using System;
using System.IO;
using JamUp.Waves.RuntimeScripts;
using JamUp.Waves.RuntimeScripts.API;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.Waves.EditModeTests
{
    public class APIGeneration
    {
        public const string InternalInitFunc = "internalInit";
        public const string InternalAddAtFunc = "internalAddAt";
        
        // Add internalize function
        private static readonly string Functions = $@"
export const init = (frame: {nameof(KeyFrame)}): void => {{
    // @ts-ignore
    initInternal(frame);
}}

export const addAt = (frame: {nameof(KeyFrame)}, time: number): void => {{
    // @ts-ignore
    addAtInternal(frame, time);
}};";
        
        private static string Folder = Path.Combine(Application.streamingAssetsPath, "src");
        private static string ApiFile = "api.ts";
        public static string GenerateDeclarations()
        {
            //return Generator.CodeForType<KeyFrame>();
            return "";
        }

        [Test]
        public void WriteOutAPI()
        {
            Directory.CreateDirectory(Folder);
            string content = String.Join("", GenerateDeclarations(), Functions);
            string path = Path.Combine(Folder, ApiFile);
            if (File.Exists(path)) File.Delete(path);
            File.WriteAllText(Path.Combine(Folder, ApiFile), content);
        }

        [Test]
        public void T()
        {
            NativeArray<CurrentWavesElement> x = new NativeArray<CurrentWavesElement>(1, Allocator.Temp);
            x[0] = new CurrentWavesElement()
            {
                Value = new float4x4(
                                     new float4(1, 2, 3, 4), 
                                     new float4(1, 2, 3, 4) + 4, 
                                     new float4(1, 2, 3, 4) + 8, 
                                     new float4(1, 2, 3, 4) + 12)
            };
            NativeArray<float> y = x.Reinterpret<float>(sizeof(float) * 16);
            foreach (var a in y)
            {
                Debug.Log(a);
            }
            x.Dispose();
        }
    }
}