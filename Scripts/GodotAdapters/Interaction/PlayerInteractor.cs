using Godot;
using Omerta.Application.Interaction;
using Omerta.GodotAdapters.Input;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class PlayerInteractor : Node
{
    private Area3D? _sensor;
    private PlayerInputAdapter? _input;
    private HudController? _hud;
    private IInteractable? _current;

    public override void _Ready()
    {
        _sensor = GetParent().GetNode<Area3D>("InteractionSensor");
        ResolveDependencies();
    }

    public override void _PhysicsProcess(double delta)
    {
        ResolveDependencies();
        IInteractable? nearest = FindNearestInteractable();
        if (!ReferenceEquals(nearest, _current))
        {
            _current = nearest;
            _hud?.SetInteractionPrompt(
                _current is not null && _current.CanInteract
                    ? $"[E] {_current.InteractionPrompt}"
                    : string.Empty);
        }

        if (_input?.InteractPressed == true && _current?.CanInteract == true)
        {
            _current.Interact();
        }
    }

    private void ResolveDependencies()
    {
        _input ??= GetTree().GetFirstNodeInGroup("player_input") as PlayerInputAdapter;
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
    }

    private IInteractable? FindNearestInteractable()
    {
        if (_sensor is null)
        {
            return null;
        }

        IInteractable? nearest = null;
        float nearestDistance = float.PositiveInfinity;
        Vector3 origin = (_sensor.GetParent() as Node3D)?.GlobalPosition ?? Vector3.Zero;
        foreach (Area3D area in _sensor.GetOverlappingAreas())
        {
            if (area is not IInteractable candidate || !candidate.CanInteract)
            {
                continue;
            }

            float distance = origin.DistanceSquaredTo(area.GlobalPosition);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearest = candidate;
            nearestDistance = distance;
        }

        return nearest;
    }
}
