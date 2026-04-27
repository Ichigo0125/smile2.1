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

    public bool logEachFrame = false;

    private static readonly Dictionary<int, SharedUdpImageStream> StreamsByPort =
        new Dictionary<int, SharedUdpImageStream>();

    private static readonly object StreamsLock = new object();
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    private SharedUdpImageStream sharedStream;
    private bool registered;

    void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        RegisterToSharedStream();
        ApplySharedTexture();
    }

    void Update()
    {
        if (sharedStream == null)
        {
            return;
        }

        sharedStream.TryDecodeLatestImage();
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

            sharedStream.AddSubscriber(logEachFrame);
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
        registered = false;
    }

    private void ApplySharedTexture()
    {
        if (targetRenderer == null || sharedStream == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(MainTexId, sharedStream.Texture);
        propertyBlock.SetTexture(BaseMapId, sharedStream.Texture);
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

        public int ListenPort { get; }
        public Texture2D Texture => texture;

        public SharedUdpImageStream(int listenPort)
        {
            ListenPort = listenPort;

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

        public void AddSubscriber(bool wantsLogging)
        {
            subscriberCount++;
            if (wantsLogging)
            {
                logSubscriberCount++;
            }
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

        public void TryDecodeLatestImage()
        {
            if (disposed || !hasNewImage)
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
