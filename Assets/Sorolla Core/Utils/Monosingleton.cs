using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Mono singleton Class. Extend this class to make singleton component.
    /// Example: 
    /// <code>
    /// public class Foo : MonoSingleton<Foo>
    /// </code>. To get the instance of Foo class, use <code>Foo.instance</code>
    /// Override <code>Init()</code> method instead of using <code>Awake()</code>
    /// from this class.
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T m_Instance;
        public static bool isTemporaryInstance { private set; get; }
        private static bool s_ShuttingDown;

        public static T Instance
        {
            get
            {
                if (s_ShuttingDown) return null;

                if (m_Instance == null)
                {
                    // Important: include inactive objects to avoid creating temps
#if UNITY_2023_1_OR_NEWER
                    m_Instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
                // For older Unity versions
                m_Instance = (T)FindObjectOfType(typeof(T), true);
#endif
                    if (m_Instance == null && Application.isPlaying)
                    {
                        Debug.LogWarning("No instance of " + typeof(T) + ", creating a temporary one.");
                        isTemporaryInstance = true;

                        var go = new GameObject("[Singleton] " + typeof(T).Name);
                        m_Instance = go.AddComponent<T>();
                    }

                    if (m_Instance != null && !m_Instance._isInitialized)
                    {
                        m_Instance._isInitialized = true;
                        m_Instance.Init();
                    }
                }

                return m_Instance;
            }
        }

        private bool _isInitialized;

        private void Awake()
        {
            if (s_ShuttingDown) return;

            if (m_Instance == null)
            {
                m_Instance = (T)this;

                // Call on root to make the whole prefab persistent (Unity only persists roots)
                var root = transform.root.gameObject;
                if (root.scene.name != "DontDestroyOnLoad")
                    DontDestroyOnLoad(root);
            }
            else if (m_Instance != this)
            {
                Debug.LogError("Another instance of " + GetType() + " already exists. Destroying this component...");
                Destroy(this); // safer than DestroyImmediate in Awake
                return;
            }

            if (!_isInitialized)
            {
                _isInitialized = true;
                Init();
            }
        }

        protected virtual void Init()
        {
        }

        private void OnDestroy()
        {
            if (m_Instance == this)
                m_Instance = null;
        }

        private void OnApplicationQuit()
        {
            s_ShuttingDown = true;
            m_Instance = null;
        }
    }
}