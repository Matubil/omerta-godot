using System;

namespace Omerta.Domain.Combat.AI;

public enum EnemyAwareness
{
    Unaware,
    Suspicious,
    Alert
}

public sealed class EnemyPerceptionState
{
    private readonly float _confirmationSeconds;
    private readonly float _detectionLossPerSecond;

    public EnemyPerceptionState(float confirmationSeconds, float detectionLossPerSecond)
    {
        if (confirmationSeconds <= 0 || detectionLossPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(confirmationSeconds));
        }

        _confirmationSeconds = confirmationSeconds;
        _detectionLossPerSecond = detectionLossPerSecond;
    }

    public event Action? Changed;

    public EnemyAwareness Awareness { get; private set; }
    public float DetectionProgress { get; private set; }
    public bool IsAlert => Awareness == EnemyAwareness.Alert;

    public void Observe(bool isVisible, float deltaSeconds)
    {
        if (deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        if (deltaSeconds == 0 || IsAlert)
        {
            return;
        }

        float previousProgress = DetectionProgress;
        EnemyAwareness previousAwareness = Awareness;
        DetectionProgress = isVisible
            ? Math.Min(1.0f, DetectionProgress + deltaSeconds / _confirmationSeconds)
            : Math.Max(0.0f, DetectionProgress - deltaSeconds * _detectionLossPerSecond);
        Awareness = DetectionProgress >= 1.0f
            ? EnemyAwareness.Alert
            : DetectionProgress > 0.0f
                ? EnemyAwareness.Suspicious
                : EnemyAwareness.Unaware;

        if (previousProgress != DetectionProgress || previousAwareness != Awareness)
        {
            Changed?.Invoke();
        }
    }

    public void ForceAlert()
    {
        if (IsAlert)
        {
            return;
        }

        DetectionProgress = 1.0f;
        Awareness = EnemyAwareness.Alert;
        Changed?.Invoke();
    }

    public void Reset()
    {
        DetectionProgress = 0.0f;
        Awareness = EnemyAwareness.Unaware;
        Changed?.Invoke();
    }
}
