using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace JamUp.UnityUtility
{
    public class SingletonResource<TComponentType> : MonoBehaviour where TComponentType : MonoBehaviour
    {
        public static TComponentType Instance { get; protected set; }
        
        protected static void CheckInstance()
        {
            if (Instance == null)
            {
                Instance = Instantiate(Resources.Load(typeof(TComponentType).Name, typeof(GameObject)) as GameObject).GetComponent<TComponentType>();
                Assert.IsNotNull(Instance);
            }
        }

        public static void DestroyInstance()
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }
}