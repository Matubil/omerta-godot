using Godot;
using Omerta.Domain.Businesses;
using Omerta.Domain.Territories;
using Omerta.GodotAdapters.Businesses;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Territories;

public sealed partial class TerritoryController : Node
{
    private WorkshopController? _workshop;
    private BarLaNieblaController? _bar;
    private DepotSanTelmoController? _depot;
    private HudController? _hud;

    public TerritoryState State { get; private set; } = null!;

    public override void _Ready()
    {
        AddToGroup("territory");
        _workshop = GetTree().GetFirstNodeInGroup("workshop") as WorkshopController;
        _bar = GetTree().GetFirstNodeInGroup("la_niebla") as BarLaNieblaController;
        _depot = GetTree().GetFirstNodeInGroup("san_telmo") as DepotSanTelmoController;
        if (_workshop is null || _bar is null || _depot is null)
        {
            GD.PushError("South District territory requires El Faro, La Niebla and San Telmo.");
            SetProcess(false);
            return;
        }

        BusinessState[] businesses =
        {
            _workshop.Business,
            _bar.Business,
            _depot.Business
        };
        State = new TerritoryState(
            "territory.south-district",
            "South District",
            businesses,
            _workshop.Definition.InitialOwnerId);
        State.ControlChanged += OnControlChanged;
        foreach (BusinessState business in businesses)
        {
            business.OwnerChanged += OnBusinessOwnerChanged;
        }

        ResolveHudAndRefresh();
    }

    public override void _Process(double delta)
    {
        if (_hud is null)
        {
            ResolveHudAndRefresh();
        }
    }

    public override void _ExitTree()
    {
        if (State is null)
        {
            return;
        }

        State.ControlChanged -= OnControlChanged;
        foreach (BusinessState business in State.Businesses)
        {
            business.OwnerChanged -= OnBusinessOwnerChanged;
        }

        State.Dispose();
    }

    private void OnControlChanged(string previousController, string currentController)
    {
        ResolveHudAndRefresh();
        string owner = GetOwnerLabel(currentController);
        _hud?.ShowStatus(
            $"Control territorial: {State.DisplayName} ahora responde a {owner}.",
            6.0f);
    }

    private void OnBusinessOwnerChanged(string previousOwner, string currentOwner) =>
        ResolveHudAndRefresh();

    private void ResolveHudAndRefresh()
    {
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        if (_hud is null || State is null)
        {
            return;
        }

        _hud.SetTerritory(
            State.DisplayName,
            GetOwnerLabel(State.ControllerId),
            State.CountControlledBy(WorkshopController.PlayerFactionId),
            State.Businesses.Count,
            State.RequiredBusinessCount);
    }

    private static string GetOwnerLabel(string factionId) =>
        factionId == WorkshopController.PlayerFactionId ? "FAMILIA" : "CÍRCULO FERRO";
}
