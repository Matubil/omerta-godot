using Godot;
using Omerta.GodotAdapters.Navigation;

namespace Omerta.GodotAdapters.Diagnostics;

public sealed partial class NavigationCrowdProbe : Node3D
{
    private Vector3 _start;
    private Vector3 _destination;
    private Vector3 _currentTarget;
    private float _speed;
    private NavigationAgent3D? _agent;
    private NavigationAvoidanceMotor? _avoidance;

    public float DistanceTravelled { get; private set; }

    public int SafeVelocitySamples => _avoidance?.SafeVelocitySamples ?? 0;

    public int DeflectionSamples => _avoidance?.DeflectionSamples ?? 0;

    public void Configure(Vector3 start, Vector3 destination, float speed)
    {
        _start = start;
        _destination = destination;
        _currentTarget = destination;
        _speed = speed;
        Position = start;
    }

    public override void _Ready()
    {
        _agent = new NavigationAgent3D
        {
            Name = "NavigationAgent3D",
            NavigationLayers = SharedNavigationWorld.PedestrianLayer,
            PathDesiredDistance = 0.3f,
            TargetDesiredDistance = 0.45f,
            Radius = 0.35f,
            Height = 1.8f
        };
        AddChild(_agent);
        _avoidance = new NavigationAvoidanceMotor(_agent, _speed);
        _agent.TargetPosition = _currentTarget;
    }

    public override void _ExitTree() => _avoidance?.Dispose();

    public override void _PhysicsProcess(double delta)
    {
        if (_agent is null || _avoidance is null ||
            NavigationServer3D.MapGetIterationId(GetWorld3D().NavigationMap) == 0)
        {
            return;
        }

        Vector3 direct = _currentTarget - GlobalPosition;
        direct.Y = 0.0f;
        if (direct.LengthSquared() <= 0.45f * 0.45f)
        {
            _currentTarget = _currentTarget.IsEqualApprox(_destination) ? _start : _destination;
            _agent.TargetPosition = _currentTarget;
        }

        Vector3 direction = NavigationMotion.DirectionTo(this, _agent, _currentTarget);
        Vector3 requestedVelocity = direction.LengthSquared() < 0.001f
            ? Vector3.Zero
            : direction.Normalized() * _speed;
        Vector3 safeVelocity = _avoidance.ResolveVelocity(requestedVelocity);
        Vector3 movement = safeVelocity * (float)delta;
        GlobalPosition += movement;
        DistanceTravelled += movement.Length();
    }
}
