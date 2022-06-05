using System;
using System.IO;
using Jint;
using UnityEngine;

namespace JamUp.JavascriptRunner.Scripts
{
    public static class Runner
    {
        public class Context
        {
            private Engine engine;
            public Context(Engine engine)
            {
                this.engine = engine;
            }

            public void AddFunction<TFunction>(string name, TFunction function)
            {
                engine.SetValue(name, function);
            }
        }
        
        private class JsLogger
        {
            // ReSharper disable once InconsistentNaming
            public void log(object msg) => Debug.Log(msg);

            // ReSharper disable once InconsistentNaming
            public void error(object msg) => Debug.LogError(msg);

            // ReSharper disable once InconsistentNaming
            public void warn(object msg) => Debug.LogWarning(msg);
        }

        public static void ExecuteString(string js, Action<Context> decorator = null)
        {
            var engine = Construct();
            decorator?.Invoke(new Context(engine));
            engine.Execute(js);
        }

        public static void ExecuteFile(string path, Action<Context> decorator = null)
        {
            ExecuteString(File.ReadAllText(path), decorator);
        }

        static Engine Construct()
        {
            var engine = new Engine();
            engine.SetValue("console", new JsLogger());
            return engine;
        }
    }
}
