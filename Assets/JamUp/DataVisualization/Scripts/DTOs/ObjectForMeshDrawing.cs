using UnityEngine;
using UnityEngine.Rendering;

namespace JamUp.DataVisualization
{
    public readonly struct ObjectForMeshDrawing
    {
        public GameObject GameObject { get; }
        public MeshRenderer Renderer { get; }
        public MeshFilter Filter { get; }

        public ObjectForMeshDrawing(GameObject gameObject, Material material, Color color)
        {
            GameObject = gameObject;
            Filter = gameObject.GetComponent<MeshFilter>();
            Filter = Filter == null ? gameObject.AddComponent<MeshFilter>() : Filter;
            Renderer = gameObject.GetComponent<MeshRenderer>();
            Renderer = Renderer == null ? gameObject.AddComponent<MeshRenderer>() : Renderer;
            Filter.mesh.indexFormat = IndexFormat.UInt32;
            Renderer.material = material;
            Renderer.material.color = color;
        }

        public void SetColor(Color color)
        {
            Renderer.material.color = color;
        }

        public TComponent Attach<TComponent, TState>() where TComponent : Component, ILiveDrawer<TState> where TState : struct
        {
            TComponent component = GameObject.AddComponent<TComponent>();
            component.SetDrawingObject(in this);
            return component;
        }
    }
}