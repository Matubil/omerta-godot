using System;
using Godot;

namespace Omerta.GodotAdapters.Navigation;

public sealed class NavigationAvoidanceMotor : IDisposable
{
    private readonly NavigationAgent3D _agent;
    private Vector3 _requestedVelocity;
    private Vector3 _safeVelocity;
    private bool _hasSafeVelocity;
    private bool _disposed;

    public NavigationAvoidanceMotor(
        NavigationAgent3D agent,
        float maximumSpeed,
        bool startsActive = true)
    {
        _agent = agent;
        _agent.AvoidanceLayers = SharedNavigationWorld.PedestrianLayer;
        _agent.AvoidanceMask = SharedNavigationWorld.PedestrianLayer;
        _agent.AvoidancePriority = 0.5f;
        _agent.NeighborDistance = 3.5f;
        _agent.MaxNeighbors = 8;
        _agent.TimeHorizonAgents = 1.0f;
        _agent.TimeHorizonObstacles = 0.5f;
        _agent.MaxSpeed = maximumSpeed;
        _agent.VelocityComputed += OnVelocityComputed;
        SetActive(startsActive);
    }

    public int SafeVelocitySamples { get; private set; }

    public int DeflectionSamples { get; private set; }

    public Vector3 ResolveVelocity(Vector3 requestedVelocity)
    {
        requestedVelocity.Y = 0.0f;
        _requestedVelocity = requestedVelocity;
        _agent.Velocity = requestedVelocity;
        if (!_agent.AvoidanceEnabled || requestedVelocity.LengthSquared() < 0.0001f)
        {
            _safeVelocity = Vector3.Zero;
            return requestedVelocity;
        }

        return _hasSafeVelocity ? _safeVelocity : requestedVelocity;
    }

    public void ResetAfterTeleport()
    {
        _requestedVelocity = Vector3.Zero;
        _safeVelocity = Vector3.Zero;
        _hasSafeVelocity = false;
        _agent.SetVelocityForced(Vector3.Zero);
        _agent.Velocity = Vector3.Zero;
    }

    public void SetActive(bool active)
    {
        _agent.AvoidanceEnabled = active;
        ResetAfterTeleport();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _agent.VelocityComputed -= OnVelocityComputed;
        _agent.AvoidanceEnabled = false;
    }

    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        safeVelocity.Y = 0.0f;
        _safeVelocity = safeVelocity;
        _hasSafeVelocity = true;
        SafeVelocitySamples++;
        if (_requestedVelocity.DistanceSquaredTo(safeVelocity) > 0.01f)
        {
            DeflectionSamples++;
        }
    }
}
