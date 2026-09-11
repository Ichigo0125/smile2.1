using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

/// <summary>
/// Shared UDP JPEG receiver for avatar screens.
/// Multiple instances with the same port reuse one socket and one decoded texture.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class UdpImageReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public int listenPort = 9101;

    [Header("Delay")]
    [Tooltip("0 = live. N = this screen shows the stream N seconds behind. " +
             "Screens sharing a number share one texture, so duplicates are free.")]
    [Min(0)]
    public int delayNumber = 0;

    public bool logEachFrame = false;

    private static readonly Dictionary<int, SharedUdpImageStream> StreamsByPort =
        new Dictionary<int, SharedUdpImageStream>();

    private static readonly object StreamsLock = new object();
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    private SharedUdpImageStream sharedStream;
    private Texture2D appliedTexture;
    private bool registered;

    void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        RegisterToSharedStream();
        ApplyCurrentTexture();
    }

    void Update()
    {
        if (sharedStream == null)
        {
            return;
        }

        sharedStream.Tick();
        ApplyCurrentTexture();
    }

    void OnDisable()
    {
        UnregisterFromSharedStream();
    }

    private void RegisterToSharedStream()
    {
        if (registered)
        {
            return;
        }

        lock (StreamsLock)
        {
            if (!StreamsByPort.TryGetValue(listenPort, out sharedStream))
            {
                try
                {
                    sharedStream = new SharedUdpImageStream(listenPort);
                    StreamsByPort.Add(listenPort, sharedStream);
                }
                catch (SocketException e)
                {
                    Debug.LogError($"[UdpImageReceiver] Failed to bind shared UDP port {listenPort}: {e.Message}");
                    sharedStream = null;
                    enabled = false;
                    return;
                }
            }

            sharedStream.AddSubscriber(logEachFrame, delayNumber, name);
            registered = true;
        }
    }

    private void UnregisterFromSharedStream()
    {
        if (!registered || sharedStream == null)
        {
            return;
        }

        lock (StreamsLock)
        {
            bool shouldDispose = sharedStream.RemoveSubscriber(logEachFrame);
            if (shouldDispose)
            {
                StreamsByPort.Remove(sharedStream.ListenPort);
                sharedStream.Dispose();
            }
        }

        sharedStream = null;
        appliedTexture = null;
        registered = false;
    }

    /// <summary>
    /// Points the renderer at whichever frame this screen should be showing. The
    /// delayed texture only changes at the ring's store rate, so most frames this
    /// is a reference comparison and nothing else.
    /// </summary>
    private void ApplyCurrentTexture()
    {
        if (targetRenderer == null || sharedStream == null)
        {
            return;
        }

        Texture2D texture = delayNumber <= 0
            ? sharedStream.Texture
            : sharedStream.Ring.GetDelayed(delayNumber);

        if (texture == null || texture == appliedTexture)
        {
            return;
        }

        appliedTexture = texture;

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(MainTexId, texture);
        propertyBlock.SetTexture(BaseMapId, texture);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private sealed class SharedUdpImageStream
    {
        private readonly object imageLock = new object();
        private readonly Texture2D texture;
        private readonly UdpClient udpClient;
        private readonly Thread recvThread;

        private byte[] latestImageData;
        private bool hasNewImage;
        private bool disposed;
        private int subscriberCount;
        private int logSubscriberCount;
        private int maxDelaySeconds;
        private int lastTickFrame = -1;

        public int ListenPort { get; }
        public Texture2D Texture => texture;
        public DelayRingBuffer Ring { get; }

        public SharedUdpImageStream(int listenPort)
        {
            ListenPort = listenPort;
            Ring = new DelayRingBuffer(DelayRingConfig.StoreIntervalMs);

            texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            udpClient = new UdpClient(ListenPort);
            udpClient.Client.ReceiveBufferSize = 1024 * 1024;

            recvThread = new Thread(ReceiveLoop);
            recvThread.IsBackground = true;
            recvThread.Start();

            Debug.Log($"[UdpImageReceiver] Listening on shared UDP port {ListenPort}");
        }

        public void AddSubscriber(bool wantsLogging, int delaySeconds, string subscriberName)
        {
            subscriberCount++;
            if (wantsLogging)
            {
                logSubscriberCount++;
            }

            if (delaySeconds <= maxDelaySeconds)
            {
                return;
            }

            // The ring is sized on its first store, from the deepest delay known by
            // then. Anything enabled later that needs more history cannot be served.
            if (Ring.CapacitySeconds > 0)
            {
                Debug.LogError($"[UdpImageReceiver] '{subscriberName}' wants a {delaySeconds}s delay but the ring was " +
                               $"already sized to {Ring.CapacitySeconds}s. It will show the oldest frame available. " +
                               "Enable this screen before the first frame arrives, or raise another screen's Delay Number.");
                return;
            }

            maxDelaySeconds = delaySeconds;
        }

        public bool RemoveSubscriber(bool wantsLogging)
        {
            subscriberCount = Mathf.Max(0, subscriberCount - 1);
            if (wantsLogging)
            {
                logSubscriberCount = Mathf.Max(0, logSubscriberCount - 1);
            }
            return subscriberCount == 0;
        }

        /// <summary>
        /// Decodes at most one image and advances the ring, once per Unity frame no
        /// matter how many screens call it.
        /// </summary>
        public void Tick()
        {
            if (disposed || lastTickFrame == Time.frameCount)
            {
                return;
            }

            lastTickFrame = Time.frameCount;

            TryDecodeLatestImage();
            Ring.Tick(texture, maxDelaySeconds);
        }

        private void TryDecodeLatestImage()
        {
            if (!hasNewImage)
            {
                return;
            }

            byte[] dataCopy = null;

            lock (imageLock)
            {
                if (!hasNewImage)
                {
                    return;
                }

                dataCopy = latestImageData;
                hasNewImage = false;
            }

            if (dataCopy == null || dataCopy.Length == 0)
            {
                return;
            }

            if (!texture.LoadImage(dataCopy, markNonReadable: false))
            {
                Debug.LogWarning("[UdpImageReceiver] Failed to LoadImage from received data.");
            }
        }

        public void Dispose()
        {
            disposed = true;

            Ring.Dispose();

            try
            {
                udpClient.Close();
            }
            catch
            {
            }

            Debug.Log($"[UdpImageReceiver] Closed shared UDP port {ListenPort}");
        }

        private void ReceiveLoop()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (!disposed)
                {
                    byte[] data = udpClient.Receive(ref remoteEP);

                    lock (imageLock)
                    {
                        latestImageData = data;
                        hasNewImage = true;
                    }

                    if (logSubscriberCount > 0)
                    {
                        Debug.Log($"[UdpImageReceiver] Got {data.Length} bytes from {remoteEP}");
                    }
                }
            }
            catch (SocketException e)
            {
                if (!disposed)
                {
                    Debug.LogWarning($"[UdpImageReceiver] ReceiveLoop SocketException: {e.Message}");
                }
            }
            catch (System.Exception e)
            {
                if (!disposed)
                {
                    Debug.LogWarning($"[UdpImageReceiver] ReceiveLoop Exception: {e.Message}");
                }
            }
        }
    }
}
