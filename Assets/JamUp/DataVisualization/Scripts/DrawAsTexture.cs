using System.Collections.Generic;
using JamUp.UnityUtility.Scripts;
using Unity.Collections;
using UnityEngine;

namespace JamUp.DataVisualization
{
    public class DrawAsTexture : SingletonMonoBehaviour<DrawAsTexture>
    {
        #region public static functions
        public static float TextureHeight { get; set; } = 16f;
        public static float TextureWidth { get; set; } = 1000f;
        public static float VerticalPadding { get; set; } = 100f;
        public static float HorizontalPadding { get; set; } = 10f;

        public static void Draw(NativeArray<float> data)
        {
            CheckInstance();
            Texture2D texture = TexturePool.Count == 0
                ? new Texture2D(data.Length, 1, TextureFormat.RFloat, false)
                : TexturePool.Dequeue();

            texture.LoadRawTextureData(data);
            texture.Apply();
            Textures.Add(texture);
        }

        public static void Clear()
        {
            foreach (Texture2D texture in Textures)
            {
                TexturePool.Enqueue(texture);
            }
            Textures.Clear();
        }
        #endregion public static functions
        
        #region private
        private static readonly Queue<Texture2D> TexturePool = new Queue<Texture2D>();
        private static readonly List<Texture2D> Textures = new List<Texture2D>();
        #endregion private

        public static void Dispose()
        {
            foreach (Texture2D texture in Textures)
            {
                Destroy(texture);
            }
            Textures.Clear();
            
            while (TexturePool.Count > 0)
            {
                Destroy(TexturePool.Dequeue());
            }
            
            DestroyInstance();
        }
        #region MonoBehaviour 
        private void OnGUI()
        {
            if (!Event.current.type.Equals(EventType.Repaint))
            {
                return;
            }

            for (var index = 0; index < Textures.Count; index++)
            {
                Texture texture = Textures[index];
                var rect = new Rect(HorizontalPadding, VerticalPadding * (index + 1), TextureWidth, TextureHeight);
                Graphics.DrawTexture(rect, texture);
            }
        }
        #endregion MonoBehaviour
    }
}