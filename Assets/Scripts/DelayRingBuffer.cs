using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

/// <summary>
/// Holds the last N seconds of decoded frames so several screens can show the
/// same stream at different delays without decoding it more than once.
///
/// Slots are written on Unity's clock rather than on packet arrival, so the ring
/// stays evenly spaced even though the sender's frame rate drifts. A slot is
/// allocated the first time it is written, which spreads the cost of the ring
/// over its first full lap instead of hitching at startup.
/// </summary>
public sealed class DelayRingBuffer
{
    private readonly int slotsPerSecond;
    private readonly float storeInterval;

    private Texture2D[] slots;
    private int writeIndex;
    private int storedCount;
    private float nextStoreTime;
    private bool loggedFootprint;
    private bool loggedSizeMismatch;

    public int SlotsPerSecond => slotsPerSecond;
    public int CapacitySeconds { get; private set; }

    public DelayRingBuffer(int storeIntervalMs)
    {
        slotsPerSecond = Mathf.Clamp(Mathf.RoundToInt(1000f / Mathf.Max(1, storeIntervalMs)), 1, 120);
        storeInterval = 1f / slotsPerSecond;
    }

    /// <summary>
    /// Copies the live frame into the ring when the next slot is due. Safe to call
    /// every frame; does nothing until the source has decoded at least one image.
    /// </summary>
    public void Tick(Texture2D source, int maxDelaySeconds)
    {
        if (source == null || source.width <= 2 || maxDelaySeconds <= 0)
        {
            return;
        }

        float now = Time.unscaledTime;

        if (slots == null)
        {
            Allocate(maxDelaySeconds);
            nextStoreTime = now;
        }

        if (now < nextStoreTime)
        {
            return;
        }

        // After a long hitch, resync instead of firing a burst of catch-up stores.
        nextStoreTime = (now - nextStoreTime > storeInterval * 4f)
            ? now + storeInterval
            : nextStoreTime + storeInterval;

        Store(source);
    }

    /// <summary>
    /// The frame from <paramref name="delaySeconds"/> ago, or the oldest frame the
    /// ring holds while it is still filling. Null until the first store.
    /// </summary>
    public Texture2D GetDelayed(int delaySeconds)
    {
        if (slots == null || storedCount == 0)
        {
            return null;
        }

        int back = Mathf.Min(delaySeconds * slotsPerSecond, storedCount - 1);
        int index = (writeIndex - 1 - back) % slots.Length;
        if (index < 0)
        {
            index += slots.Length;
        }

        return slots[index];
    }

    public void Dispose()
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                Object.Destroy(slots[i]);
                slots[i] = null;
            }
        }

        slots = null;
        storedCount = 0;
        writeIndex = 0;
    }

    private void Allocate(int maxDelaySeconds)
    {
        CapacitySeconds = Mathf.Max(1, maxDelaySeconds);

        // One spare slot so the oldest readable frame is never the one being overwritten.
        slots = new Texture2D[CapacitySeconds * slotsPerSecond + 1];

        if ((SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) == 0)
        {
            Debug.LogError("[DelayRingBuffer] Graphics.CopyTexture is unsupported here; delayed screens will stay blank.");
        }

        Debug.Log($"[DelayRingBuffer] Ring holds {CapacitySeconds}s at {slotsPerSecond} slots/s ({slots.Length} slots).");
    }

    private void Store(Texture2D source)
    {
        Texture2D slot = slots[writeIndex];

        if (slot == null)
        {
            slot = new Texture2D(source.width, source.height, source.format, false);
            slot.wrapMode = TextureWrapMode.Clamp;
            slot.filterMode = FilterMode.Bilinear;
            slots[writeIndex] = slot;
        }
        else if (slot.width != source.width || slot.height != source.height || slot.format != source.format)
        {
            if (!loggedSizeMismatch)
            {
                Debug.LogError($"[DelayRingBuffer] Stream changed to {source.width}x{source.height} {source.format} " +
                               $"but the ring holds {slot.width}x{slot.height} {slot.format}. Delayed screens are frozen.");
                loggedSizeMismatch = true;
            }
            return;
        }

        Graphics.CopyTexture(source, slot);

        writeIndex = (writeIndex + 1) % slots.Length;

        if (storedCount < slots.Length)
        {
            storedCount++;
            if (storedCount == slots.Length && !loggedFootprint)
            {
                LogFootprint();
                loggedFootprint = true;
            }
        }
    }

    private void LogFootprint()
    {
        long perSlot = Profiler.GetRuntimeMemorySizeLong(slots[0]);
        long total = perSlot * slots.Length;

        Debug.Log($"[DelayRingBuffer] Ring full: {slots.Length} x {slots[0].width}x{slots[0].height} {slots[0].format} " +
                  $"= {perSlot / 1024f:F0} KB/slot, {total / (1024f * 1024f):F0} MB total.");
    }
}
