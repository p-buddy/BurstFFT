using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JamUp.Waves.RuntimeScripts.API;
using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Entities;
using UnityEngine;


public partial class LocalCodeRunnerSystem : SystemBase
{
    private readonly string baseDirectory = GetFolderInHome("WavyNthLocal");
    
    private const string JavascriptExecutableFileName = "bundle.js";

    private const string TypescriptAPIOutputFileName = "api.ts";

    private FileSystemWatcher watcher;

    protected override void OnCreate()
    {
        base.OnCreate();

        if (!Directory.Exists(baseDirectory)) throw new Exception($"{baseDirectory} folder not found");

        string apiFile = Path.Combine(baseDirectory, TypescriptAPIOutputFileName);

        API api = new();
        File.WriteAllText(apiFile, api.Generate());

        string watchFile = Path.Combine(baseDirectory, JavascriptExecutableFileName);
        watcher = new FileSystemWatcher(baseDirectory);

        watcher.Changed += (_, e) =>
        {
            if (e.ChangeType != WatcherChangeTypes.Changed || e.FullPath != watchFile) return;
            JsRunner.ExecuteFile(watchFile, context => context.ApplyAPI(api));
        };

        watcher.Created += (_, e) =>
        {
            if (e.FullPath != watchFile) return;
            JsRunner.ExecuteFile(watchFile, context => context.ApplyAPI(api));
        };
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        watcher?.Dispose();
    }

    private static string GetFolderInHome(string folderName) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), folderName);

    protected override void OnUpdate() { }
}
