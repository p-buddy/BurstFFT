using UnityEngine;

namespace JamUp.UnityUtility.Scripts
{
    public abstract class SingletonMonoBehaviour<TComponentType> : MonoBehaviour where TComponentType : MonoBehaviour
    {
        public static TComponentType Instance { get; protected set; }
        
        protected static void CheckInstance()
        {
            if (Instance == null)
            {
                Instance = new GameObject(nameof(TComponentType)).AddComponent<TComponentType>();
            }
        }

        public static void DestroyInstance()
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }
}