using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// Receives Timeline signals and gently pushes the target backward once per signal.
public class SignalPushBack : MonoBehaviour, INotificationReceiver
{
    public Transform target;
    public float distance = 0.25f;   // approximate meters moved per signal
    public float duration = 0.7f;    // seconds to slow down close to stop
    public bool debugLogs = true;

    float _pushAmount;
    float _pushVelocity;
    Vector3 _lastAppliedWorldOffset;
    bool _hasAppliedOffset;

    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is SignalEmitter)
        {
            if (debugLogs)
                Debug.Log("[SignalPushBack] SignalEmitter received.", this);

            Push();
        }
    }

    // Exposed for SignalReceiver UnityEvent reaction wiring.
    public void Push()
    {
        if (target == null)
        {
            if (debugLogs)
                Debug.LogWarning("[SignalPushBack] target is not assigned.", this);

            return;
        }

        float safeDuration = Mathf.Max(0.01f, duration);
        // Exponential damping: keep ~5% speed after duration.
        float damping = -Mathf.Log(0.05f) / safeDuration;
        float impulseVelocity = distance * damping;
        _pushVelocity += impulseVelocity;

        if (debugLogs)
            Debug.Log($"[SignalPushBack] Push impulse added. velocity={_pushVelocity:F3}", this);
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        float safeDuration = Mathf.Max(0.01f, duration);
        float damping = -Mathf.Log(0.05f) / safeDuration;
        _pushAmount += _pushVelocity * Time.deltaTime;
        _pushVelocity *= Mathf.Exp(-damping * Time.deltaTime);
        if (Mathf.Abs(_pushVelocity) < 0.0001f)
            _pushVelocity = 0f;

        // Remove previous frame's additive offset first so we never accumulate drift.
        if (_hasAppliedOffset)
        {
            target.position -= _lastAppliedWorldOffset;
            _hasAppliedOffset = false;
        }

        if (Mathf.Abs(_pushAmount) <= 0.000001f)
            return;

        Vector3 worldOffset = target.TransformDirection(Vector3.back) * _pushAmount;
        target.position += worldOffset;
        _lastAppliedWorldOffset = worldOffset;
        _hasAppliedOffset = true;
    }
}
