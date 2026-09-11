using UnityEngine;

/// <summary>
/// Optional scene-wide tuning for the delayed-screen ring buffer. Drop one on any
/// GameObject to override the default; without it the default below is used.
///
/// The interval is the ring's time resolution, and it sets how much memory the
/// ring costs: memory = (largest Delay Number) x (1000 / Store Interval Ms) x frame size.
/// Raise it to spend less memory at the cost of choppier delayed screens.
/// </summary>
[DefaultExecutionOrder(-100)]
public class DelayRingConfig : MonoBehaviour
{
    public const int DefaultStoreIntervalMs = 40;

    [Header("Ring Settings")]
    [Tooltip("How often a frame is copied into the ring. 40 ms = 25 stored frames per second.")]
    [Range(20, 200)]
    public int storeIntervalMs = DefaultStoreIntervalMs;

    public static int StoreIntervalMs { get; private set; } = DefaultStoreIntervalMs;

    void Awake()
    {
        StoreIntervalMs = storeIntervalMs;
        Debug.Log($"[DelayRingConfig] Storing one frame every {StoreIntervalMs} ms ({1000 / StoreIntervalMs} per second).");
    }
}
