using Godot;

namespace Omerta.GodotAdapters.Navigation;

public static class NavigationMotion
{
    public static Vector3 DirectionTo(
        Node3D actor,
        NavigationAgent3D agent,
        Vector3 destination,
        float repathDistance = 0.35f)
    {
        Vector3 direct = destination - actor.GlobalPosition;
        direct.Y = 0.0f;
        Rid map = actor.GetWorld3D().NavigationMap;
        if (NavigationServer3D.MapGetIterationId(map) == 0)
        {
            return direct;
        }

        bool needsPath = agent.GetCurrentNavigationPath().Length == 0 ||
            agent.TargetPosition.DistanceSquaredTo(destination) > repathDistance * repathDistance;
        if (needsPath)
        {
            agent.TargetPosition = destination;
        }

        if (agent.IsNavigationFinished() &&
            direct.LengthSquared() > agent.TargetDesiredDistance * agent.TargetDesiredDistance)
        {
            agent.TargetPosition = destination;
        }

        Vector3 next = agent.GetNextPathPosition();
        Vector3 direction = next - actor.GlobalPosition;
        direction.Y = 0.0f;
        return direction;
    }
}
