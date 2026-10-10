using Godot;
using Omerta.Domain;

namespace Omerta.GodotAdapters;

public sealed partial class GameBootstrap : Node3D
{
    public override void _Ready()
    {
        GD.Print($"{GameIdentity.CommercialName} [{GameIdentity.TechnicalName}] — Etapas 0 y 1");
    }
}
