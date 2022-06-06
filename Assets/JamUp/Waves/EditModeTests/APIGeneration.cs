using System;
using System.IO;
using JamUp.TypescriptGenerator.Scripts;
using JamUp.Waves.Scripts.API;
using NUnit.Framework;
using UnityEngine;

namespace JamUp.Waves.EditModeTests
{
    public class APIGeneration
    {
        public const string InternalInitFunc = "internalInit";
        public const string InternalAddAtFunc = "internalAddAt";
        
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
            return Generator.CodeForType<KeyFrame>();
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
    }
}