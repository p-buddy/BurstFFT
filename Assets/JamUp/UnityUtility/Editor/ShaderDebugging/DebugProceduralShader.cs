using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace JamUp.UnityUtility.Editor
{
    public struct DebugProceduralShader : IDisposable
    {
        private DebugShaderUsingReadWriteBuffer<float> debugDirtyFlag;
        private DebugShaderUsingReadWriteBuffer<float3> debugVerts;
        public bool Ready
        {
            get
            {
                NativeArray<float> data = debugDirtyFlag.GetDataBlocking();
                bool ready = data[0].Equals(1f);
                data.Dispose();
                return ready;
            }
        }

        public DebugProceduralShader(int size, Material material)
        {
            debugDirtyFlag = new DebugShaderUsingReadWriteBuffer<float>(1, material, "DEBUG_DIRTY_FLAG_BUFFER");
            debugVerts = new DebugShaderUsingReadWriteBuffer<float3>(size, material, "DEBUG_VERTS_BUFFER");
        }

        public bool TryGetVertData(out NativeArray<float3> data)
        {
            if (!Ready)
            {
                data = default;
                return false;
            }
            
            data = debugVerts.GetDataBlocking();
            return true;
        }
        
        public void Dispose()
        {
            debugDirtyFlag.Dispose();
            debugVerts.Dispose();
        }
    }
}