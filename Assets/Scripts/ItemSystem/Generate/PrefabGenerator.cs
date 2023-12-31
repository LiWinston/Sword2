using System.Collections;
using Behavior;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using IPoolable = Utility.IPoolable;
using Random = UnityEngine.Random;

namespace ItemSystem.Generate
{
    [System.Serializable]
    public class PrefabGenerator : MonoBehaviour
    {
        [InspectorLabel("生成--GenerationInfo")]
        public GameObject prefab;
        [SerializeField] private float initialSpawnInterval = 1f;
        [SerializeField] private float minSpawnInterval = 0.1f;
        [SerializeField] private float spawnHeight = 1f;
        [SerializeField] private float accelerationRate = 0.1f;
        [SerializeField] private float maxExistTime = 10f;
    
        private float spawnTimer = 0f;
        private float spawnInterval;
        private Vector3 targetPosition;

        [InspectorLabel("对象池--ObjectPool")]
        [SerializeField]private int defaultCapacity = 40;
        [SerializeField]internal int maxCapacity = 100;
        private ObjectPool<GameObject> objPool;
        public int countAll;
        public int countActive;
        public int countInactive;
        [FormerlySerializedAs("XMax")] [SerializeField] private float MaxOffsetXZ = 10;

        private void Start()
        {
            targetPosition = transform.position;
            spawnInterval = initialSpawnInterval;
            objPool = new ObjectPool<GameObject>(CreateFunc, actionOnGet, actionOnRelease, actionOnDestroy,
                true, defaultCapacity, maxCapacity);
        }
        GameObject CreateFunc()
        {
            var newPos = targetPosition + new Vector3(Random.Range(-MaxOffsetXZ, MaxOffsetXZ), spawnHeight, Random.Range(-MaxOffsetXZ, MaxOffsetXZ));
            RaycastHit hit;
            while (!Physics.Raycast(newPos, Vector3.down, out hit, 10, NavMesh.AllAreas))
            {
                newPos = targetPosition + new Vector3(Random.Range(-MaxOffsetXZ, MaxOffsetXZ), spawnHeight, Random.Range(-MaxOffsetXZ, MaxOffsetXZ));
            }
            var prfb = Instantiate(prefab, hit.point, Quaternion.identity);
            
            prfb.GetComponent<IPoolable>().SetPool(objPool);
            // SetPoolForGeneratedObject(prfb);
            prfb.name = prefab.name + countAll.ToString();
            return prfb;
        }
    
        void actionOnGet(GameObject obj)
        {
            obj.GetComponent<IPoolable>().actionOnGet();
            var newPos = targetPosition + new Vector3(Random.Range(-MaxOffsetXZ, MaxOffsetXZ), spawnHeight, Random.Range(-MaxOffsetXZ, MaxOffsetXZ));
            RaycastHit hit;
            while (!Physics.Raycast(newPos, Vector3.down, out hit, 10, NavMesh.AllAreas))
            {
                newPos = targetPosition + new Vector3(Random.Range(-MaxOffsetXZ, MaxOffsetXZ), spawnHeight, Random.Range(-MaxOffsetXZ, MaxOffsetXZ));
            }
            obj.transform.position = hit.point;
            obj.SetActive(true);
        }

        void actionOnRelease(GameObject obj)
        {
            obj.GetComponent<IPoolable>().actionOnRelease();
            obj.SetActive(false);
        }

        void actionOnDestroy(GameObject obj)
        {
            Destroy(obj);
        }
    
        private void Update()
        {
            targetPosition = transform.position;
            
            countAll = objPool.CountAll;
            countActive = objPool.CountActive;
            countInactive = objPool.CountInactive;
        
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                // Accelerate the generation speed, but do not exceed the minimum interval
                spawnInterval = Mathf.Max(minSpawnInterval, spawnInterval - accelerationRate);

                if (countActive < maxCapacity)
                {
                    GameObject prfb = objPool.Get();
                    if (isToDestroy)
                    {
                        StartCoroutine(ReturnToPoolDelayed(prfb, maxExistTime));
                    }
                }
            }
        }
    

        public bool isToDestroy => maxExistTime > 0;

        private IEnumerator ReturnToPoolDelayed(GameObject obj, float delay)
        {
            if (!obj.GetComponent<IPoolable>().IsExisting) yield break;
            yield return new WaitForSeconds(delay);
            // Return the object to the object pool
            if (obj.activeSelf)
            {
                objPool.Release(obj);
            }
        }
    }
}