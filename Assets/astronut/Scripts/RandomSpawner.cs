using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject prefab;

    [Tooltip("生成物件的 Parent，不指定則生成在 Scene Root")]
    [SerializeField] private Transform parent;

    [Header("Spawn Area")]
    [SerializeField] private float minRadius = 1f;
    [SerializeField] private float maxRadius = 5f;

    [SerializeField] private float minHeight = -1f;
    [SerializeField] private float maxHeight = 2f;

    [Header("Distance")]
    [Tooltip("生成物件彼此最小距離")]
    [SerializeField] private float minDistanceBetweenObjects = 1f;

    [Tooltip("找位置最多嘗試次數")]
    [SerializeField] private int maxSpawnAttempts = 100;

    [Header("Spawn Settings")]
    [SerializeField] private int spawnCount = 100;

    [Tooltip("幾秒內生成完所有物件")]
    [SerializeField] private float spawnDuration = 10f;

    [Tooltip("每次生成間隔最小值")]
    [SerializeField] private float minInterval = 0.02f;

    [Tooltip("每次生成間隔最大值")]
    [SerializeField] private float maxInterval = 0.2f;

    private Coroutine spawnRoutine;

    private readonly List<Vector3> spawnedPositions = new();

    private void OnEnable()
    {
        spawnedPositions.Clear();
        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    IEnumerator SpawnRoutine()
    {
        int spawned = 0;
        float startTime = Time.time;

        while (spawned < spawnCount)
        {
            bool success = Spawn();

            if (success)
                spawned++;

            float elapsed = Time.time - startTime;
            float remainTime = Mathf.Max(0f, spawnDuration - elapsed);
            int remainCount = spawnCount - spawned;

            if (remainCount <= 0)
                yield break;

            float averageInterval = remainTime / remainCount;

            float interval = Random.Range(minInterval, maxInterval);
            interval = Mathf.Min(interval, averageInterval * 2f);

            yield return new WaitForSeconds(interval);
        }
    }

    bool Spawn()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized *
                             Random.Range(minRadius, maxRadius);

            float y = Random.Range(minHeight, maxHeight);

            Vector3 pos = transform.position +
                          new Vector3(circle.x, y, circle.y);

            if (!IsPositionValid(pos))
                continue;

            spawnedPositions.Add(pos);

            GameObject obj;

            if (parent != null)
                obj = Instantiate(prefab, pos, Quaternion.identity, parent);
            else
                obj = Instantiate(prefab, pos, Quaternion.identity);

            // 只旋轉 Y 軸，朝向 Spawner
            Vector3 target = transform.position;
            target.y = obj.transform.position.y;
            obj.transform.LookAt(target);

            return true;
        }

        Debug.LogWarning($"[{name}] 找不到符合距離限制的位置，已放棄此次生成。");

        return false;
    }

    bool IsPositionValid(Vector3 position)
    {
        foreach (Vector3 p in spawnedPositions)
        {
            if (Vector3.Distance(position, p) < minDistanceBetweenObjects)
                return false;
        }

        return true;
    }
}