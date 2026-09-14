using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomChildActivator : MonoBehaviour
{
    [Header("每波間隔 (秒)")]
    public float interval = 0.5f;

    [Header("第一波啟用數量")]
    public int startBatchSize = 1;

    private List<GameObject> children = new List<GameObject>();

    void OnEnable()
    {
        StartCoroutine(ActivateChildrenRoutine());
    }

    IEnumerator ActivateChildrenRoutine()
    {
        children.Clear();

        // 收集所有第一層子物件，並先全部關閉
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.SetActive(false);
            children.Add(child);
        }

        // Fisher-Yates Shuffle
        for (int i = children.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            GameObject temp = children[i];
            children[i] = children[j];
            children[j] = temp;
        }

        int index = 0;
        int batchSize = startBatchSize;

        while (index < children.Count)
        {
            yield return new WaitForSeconds(interval);

            int countThisRound = Mathf.Min(batchSize, children.Count - index);

            for (int i = 0; i < countThisRound; i++)
            {
                children[index].SetActive(true);
                index++;
            }

            // // 每波數量加倍：1 → 2 → 4 → 8 → ...
            // batchSize *= 2;

            // 如果想永遠一次只出現一個，把上面改成：
            batchSize *= 1;
        }
    }
}