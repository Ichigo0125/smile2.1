using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomChildActivator : MonoBehaviour
{
    [Header("每波間隔 (秒)")]
    public float interval = 0.5f;

    [Header("第一波啟用數量")]
    public int startBatchSize = 1;

    [Header("啟用語音")]
    [Tooltip("每個子物件啟用時，從這些語音中隨機播放一段")]
    [SerializeField] private AudioClip[] activationVoiceClips;

    [Min(0f)]
    [SerializeField] private float voiceVolume = 1f;

    private const string VoiceFolder = "Assets/Vocal/9我是誰？";

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
                GameObject child = children[index];
                child.SetActive(true);
                PlayActivationVoice(child.transform.position);
                index++;
            }

            // // 每波數量加倍：1 → 2 → 4 → 8 → ...
            // batchSize *= 2;

            // 如果想永遠一次只出現一個，把上面改成：
            batchSize *= 1;
        }
    }

    private void PlayActivationVoice(Vector3 position)
    {
        if (activationVoiceClips == null || activationVoiceClips.Length == 0)
        {
            return;
        }

        AudioClip clip = activationVoiceClips[Random.Range(0, activationVoiceClips.Length)];
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, voiceVolume);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { VoiceFolder });
        List<AudioClip> clips = new List<AudioClip>(guids.Length);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
            {
                clips.Add(clip);
            }
        }

        clips.Sort((first, second) => string.Compare(first.name, second.name, System.StringComparison.Ordinal));
        activationVoiceClips = clips.ToArray();
    }
#endif
}