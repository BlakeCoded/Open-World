using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Project.Singleton;

/// <summary>
/// Manages reusable object pools for efficient spawning and recycling
/// of gameplay and UI objects.
/// </summary>
public class ObjectPooler : MonoBehaviourSingleton<ObjectPooler>
{
    [SerializeField] private List<GameObject> objectsToPreInitialize;

    private Dictionary<GameObject, ObjectPool<GameObject>> objectPools; // Stores the Object Pools
    private Dictionary<GameObject, GameObject> instanceToPrefab; // Stores a clone Reference to the original prefab

    private GameObject poolHolder;

    const int MIN_POOL_CAPACITY = 225;
    const int MAX_POOL_CAPACITY = 1000;

    private void Awake()
    {
        //OnInternalBootstrap();
    }

    protected override void OnInternalBootstrap()
    {
        base.OnInternalBootstrap();

        Setup();
    }

    private void Setup()
    {
        objectPools = new Dictionary<GameObject, ObjectPool<GameObject>>();
        instanceToPrefab = new Dictionary<GameObject, GameObject>();

        poolHolder = new GameObject("Object Pools");
        poolHolder.transform.SetParent(transform);

        PreInitializeObjectPool(objectsToPreInitialize, MIN_POOL_CAPACITY);

        objectsToPreInitialize = null;
    }

    private void PreInitializeObjectPool(List<GameObject> prefabs, int count)
    {
        foreach (GameObject prefab in prefabs)
        {
            if(!objectPools.ContainsKey(prefab))
            {
                CreatePool(prefab);
            }

            List<GameObject> temp = new(count);

            for(int i  = 0; i < count; i++)
            {
                GameObject obj = objectPools[prefab].Get();
                temp.Add(obj);
            }

            foreach(GameObject obj in temp)
            {
                //objectPools[prefab].Release(obj);
            }
        }
    }

    private void CreatePool(GameObject prefab)
    {
        //ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
        //    createFunc: () => CreateObject(prefab),
        //    actionOnGet: OnGetObject,
        //    actionOnRelease: OnReleaseObject,
        //    actionOnDestroy: OnDestroyObject,
        //    collectionCheck: true, // Editor safety checks. Disable in release builds for performance.
        //    defaultCapacity: MIN_POOL_CAPACITY,
        //    maxSize: MAX_POOL_CAPACITY
        //    );

        //objectPools[prefab] = pool;
    }

    private GameObject CreateObject(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);

        obj.transform.SetParent(poolHolder.transform);

        return obj;
    }

    private void OnGetObject(GameObject obj)
    {
        obj.GetComponent<IPoolable>()?.OnSpawn();
            
        obj.SetActive(true);
    }

    private void OnReleaseObject(GameObject obj)
    {
        obj.GetComponent<IPoolable>()?.OnDespawn();

        obj.SetActive(false);
    }

    private void OnDestroyObject(GameObject obj)
    {
        if (instanceToPrefab.ContainsKey(obj))
        {
            instanceToPrefab.Remove(obj);
        }

        Destroy(obj);
    }

    private GameObject GetInternal(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, Transform parent = null)
    {
        if (objectToSpawn == null)
        {
            Debug.LogError("Tried to spawn null object");
            return null;
        }

        if(!objectPools.ContainsKey(objectToSpawn))
        {
            CreatePool(objectToSpawn);
        }

        GameObject obj = objectPools[objectToSpawn].Get();

        instanceToPrefab[obj] = objectToSpawn;

        obj.transform.SetParent(parent);
        obj.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            
        obj.SetActive(true);

        return obj;
    }

    private GameObject GetInternalUI(GameObject objectToSpawn, Transform parent)
    {
        if (objectToSpawn == null)
        {
            Debug.LogError("Tried to spawn null object");
            return null;
        }

        if (!objectPools.ContainsKey(objectToSpawn))
        {
            CreatePool(objectToSpawn);
        }

        GameObject obj = objectPools[objectToSpawn].Get();

        instanceToPrefab[obj] = objectToSpawn;

        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        obj.SetActive(true);

        return obj;
    }

    private T GetInternal<T>(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, Transform parent = null) where T : Component
    {
        GameObject obj = GetInternal(objectToSpawn, spawnPosition, spawnRotation, parent);

        if(obj.TryGetComponent(out T component))
        {
            return component;
        }

        Debug.LogError($"{objectToSpawn.name} does not contain component {typeof(T)}");
        return null;
    }

    private T GetInternalUI<T>(GameObject objectToSpawn, Transform parent) where T : Component
    {
        GameObject obj = GetInternalUI(objectToSpawn, parent);

        if(obj.TryGetComponent(out T component))
        {
            return component;
        }

        Debug.LogError($"{objectToSpawn.name} does not contain component {typeof(T)}");
        return default;
    }

    private T GetInternal<T>(T prefab, Vector3 spawnPosition, Quaternion spawnRotation, Transform parent = null) where T : Component
    {
        return GetInternal<T>(prefab.gameObject, spawnPosition, spawnRotation, parent);
    }

    private T GetInternalUI<T>(T prefab, Transform parent) where T : Component
    {
        return GetInternalUI<T>(prefab.gameObject, parent);
    }

    private void ReleaseInternal(GameObject obj)
    {
        if(instanceToPrefab.TryGetValue(obj, out GameObject prefab))
        {
            obj.transform.SetParent(poolHolder.transform);
            obj.SetActive(false);

            //objectPools[prefab].Release(obj);
        }
        else
        {
            Debug.LogWarning("Trying to release an object that is not pooled: " + obj.name);
        }
    }

    public static void Get(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, Transform parent = null) => Instance.GetInternal(objectToSpawn, spawnPosition, spawnRotation, parent);
    public static T Get<T>(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, Transform parent = null) where T : Component => Instance.GetInternal<T>(objectToSpawn, spawnPosition, spawnRotation, parent);
    public static T Get<T>(T prefab, Vector3 spawnPosition, Quaternion spawnRotation, Transform parent = null) where T : Component => Instance.GetInternal<T>(prefab, spawnPosition, spawnRotation, parent);
    public static void GetUI(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, Transform parent = null) => Instance.GetInternalUI(objectToSpawn, parent);
    public static T GetUI<T>(GameObject objectToSpawn, Transform parent) where T : Component => Instance.GetInternalUI<T>(objectToSpawn, parent);
    public static T GetUI<T>(T prefab, Transform parent) where T : Component => Instance.GetInternalUI<T>(prefab, parent);
    public static void Release(GameObject obj) => Instance.ReleaseInternal(obj);
}