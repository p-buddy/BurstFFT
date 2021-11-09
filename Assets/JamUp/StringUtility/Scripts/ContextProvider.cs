using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;

namespace JamUp.StringUtility
{
    public static class ContextProvider
    {
        private const string ObjectConstructor = ".ctor";
        private const string StaticConstructor = ".cctor";
        private const string Finalizer = "Finalize";

        public static string Context([CallerMemberName] string methodName = "", [CallerFilePath] string sourceFilePath = "")
        {
            string className;
            bool isConstructor;
            bool isFinalizer;
            
            #if UNITY_EDITOR
            SlowButAccurate:
                StackTrace stackTrace = new System.Diagnostics.StackTrace();
                StackFrame frame = stackTrace.GetFrames()?[1];
                Assert.IsNotNull(frame);
                MethodBase method = frame.GetMethod();
                className = method.DeclaringType?.Name;
                methodName = method.Name;
                Assert.IsNotNull(className);
                isConstructor = method.IsConstructor;
                isFinalizer = !isConstructor && method.Name == Finalizer;
            #else
            FastButInaccurate:
                className = Path.GetFileName(sourceFilePath).RemoveSubString(Path.GetExtension(sourceFilePath));
                isConstructor = memberName == ObjectConstructor || memberName == StaticConstructor;
                isFinalizer = !isConstructor && memberName == Finalizer;
            #endif
            
            if (isConstructor)
            {
                return $"{className}()::"; 
            }

            if (isFinalizer)
            {
                return $"~{className}()::";
            }
            
            return $"{className}::{methodName}()::";
        }
    }
}