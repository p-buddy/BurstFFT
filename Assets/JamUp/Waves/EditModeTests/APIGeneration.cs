using System;
using System.IO;
using JamUp.Waves.Scripts;
using JamUp.Waves.Scripts.API;
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
                Value = new float4x4(new float4(0, 4, 8, 12),
                                     new float4(1, 5, 9, 13),
                                     new float4(2, 6, 10, 14),
                                     new float4(3, 7, 11, 15))
            };
            NativeArray<Matrix4x4> y = x.Reinterpret<Matrix4x4>();
            Debug.Log(y[0]);
            x.Dispose();
        }
    }
}