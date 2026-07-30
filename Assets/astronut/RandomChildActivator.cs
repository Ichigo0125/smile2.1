using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomChildActivator : MonoBehaviour
{
    public float interval = 0.5f; // 每波間隔

    private List<GameObject> children = new List<GameObject>();

    void OnEnable()
    {
        StartCoroutine(ActivateChildrenRoutine());
    }

    IEnumerator ActivateChildrenRoutine()
    {
        children.Clear();

        // 取得所有子物件（第一層）
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (child != null)
            {
                child.SetActive(false);
                children.Add(child);
            }
        }

        int index = 0;
        int batchSize = 1;

        while (index < children.Count)
        {
            yield return new WaitForSeconds(interval);

            int countThisRound = Mathf.Min(batchSize, children.Count - index);

            for (int i = 0; i < countThisRound; i++)
            {
                children[index].SetActive(true);
                index++;
            }

            batchSize *= 1; // 🔥 1 → 2 → 4 → 8
        }
    }
}