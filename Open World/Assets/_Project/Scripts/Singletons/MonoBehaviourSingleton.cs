using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Project.Singleton
{
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviourSingleton where T : MonoBehaviour
    {
        static T _Instance = null;
        static bool _isInitializing = false;
        static readonly object _InstanceLock = new object();

        public static T Instance
        {
            get
            {
                lock(_InstanceLock)
                {
                    // DO NOTHING IF QUITTING
                    if(IsQuitting) return null;

                    // INSTANCE ALREADY FOUND
                    if(_Instance != null) return _Instance;

                    _isInitializing = true;

                    // SEARCH FOR ANY IN-SCENE INSTANCES OF T
                    var AllInstances = FindObjectsByType<T>(FindObjectsSortMode.None);

                    // FOUND EXACTLY ONE?
                    if(AllInstances.Length == 1)
                    {
                        _Instance = AllInstances[0];
                    } // FOUND NONE
                    else if(AllInstances.Length == 0)
                    {
                        _Instance = new GameObject($"Singleton<{typeof(T)}>").AddComponent<T>();
                    }
                    else
                    {
                        _Instance = AllInstances[0];

                        // DESTROY DUPLICATES
                        for(int index = 1; index < AllInstances.Length; ++index)
                        {
                            Debug.LogError($"Destroying duplicate {typeof(T)} on {AllInstances[0].gameObject.name}");
                            Destroy(AllInstances[index].gameObject);
                        }
                    }

                    _isInitializing = false;
                    return _Instance;
                }
            }
        }

        static void ConstructIfNeeded(MonoBehaviourSingleton<T> InInstace)
        {
            lock(_InstanceLock)
            {
                // ONLY CONSTRUCT IF THE INSTANCE IS NULL AND IS NOT BEING INITIALIZED
                if(_Instance ==  null && !_isInitializing)
                {
                    _Instance = InInstace as T;
                }
                else if (_Instance != null && !_isInitializing)
                {
                    Debug.LogError($"Destroying duplicate {typeof(T)} on {InInstace.gameObject.name}");
                    Destroy(InInstace.gameObject);
                }
            }
        }

        protected virtual void OnInternalBootstrap()
        {
            if (_Instance != this) return;

            DontDestroyOnLoad(gameObject);
        }

        private void Awake()
        {
            ConstructIfNeeded(this);

            if (_Instance != this) return;

            OnAwake();
        }

        protected virtual void OnAwake()
        {
            DontDestroyOnLoad(gameObject);
        }
#if UNITY_EDITOR
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        void OnPlayModeChanged(PlayModeStateChange InChange)
        {
            if((InChange == PlayModeStateChange.ExitingPlayMode) && (_Instance != null))
            {
                IsQuitting = true;
                DestroyImmediate(gameObject);
            }
        }
#endif // UNITY_EDITOR

    }

    public abstract class MonoBehaviourSingleton : MonoBehaviour
    {
        protected static bool IsQuitting { get; set; } = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad()
        {
            IsQuitting = false;
        }

        void OnApplicationQuit()
        {
            IsQuitting = true;
        }
    }
}