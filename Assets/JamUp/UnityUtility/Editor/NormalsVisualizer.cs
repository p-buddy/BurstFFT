#define USE_NORMAL_VISUALIZER
#undef USE_NORMAL_VISUALIZER
using UnityEditor;
using UnityEngine;

namespace pbuddy.ShaderUtility.EditorScripts
{
    #if USE_NORMAL_VISUALIZER
    [CustomEditor(typeof(MeshFilter))]
    public class NormalsVisualizer : UnityEditor.Editor 
    {
        private Mesh mesh;

        void OnEnable() 
        {
            MeshFilter mf = target as MeshFilter;
            if (mf != null) 
            {
                mesh = mf.sharedMesh;
            }
        }

        void OnSceneGUI() 
        {
            if (mesh == null) 
            {
                return;
            }

            Handles.matrix = (target as MeshFilter).transform.localToWorldMatrix;
            Handles.color = Color.yellow;
            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int len = mesh.vertexCount;
        
            for (int i = 0; i < len; i++) 
            {
                Handles.DrawLine(verts[i], verts[i] + normals[i]);
            }
        }
    }
    #endif
}