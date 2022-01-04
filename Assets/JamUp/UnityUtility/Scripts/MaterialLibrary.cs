using System;
using System.Collections.Generic;

using UnityEngine;

using static pbuddy.LoggingUtility.RuntimeScripts.ContextProvider;

namespace JamUp.UnityUtility
{
    public class MaterialLibrary : SingletonResource<MaterialLibrary>
    {
        public static Material GetMaterial(string name)
        {
            CheckInstance();
            
            if (materialByName.TryGetValue(name, out Material material))
            {
                return material;
            }

            throw new ArgumentException($"{Context()}No material named '{name}' found in collection");
        }

        [SerializeField] private List<Material> materials;

        private static Dictionary<string, Material> materialByName;

        private void Awake()
        {
            materialByName = new Dictionary<string, Material>(materials.Count);
            foreach (Material material in materials)
            {
                materialByName[material.name] = material;
            }
        }
    }
}