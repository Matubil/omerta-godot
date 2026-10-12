using Godot;
using Omerta.Domain.Businesses;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Businesses;

public sealed partial class BarLaNieblaController : Node3D, IBusinessOperationsHost
{
    [Export]
    public BusinessDefinitionResource Definition { get; set; } = null!;

    private Label3D? _operationsLabel;
    private MeshInstance3D? _operationsVisual;
    private HudController? _hud;

    public BusinessState Business { get; private set; } = null!;

    public BusinessOperationsState Operations { get; private set; } = null!;

    public Vector3 ExteriorEntrancePosition =>
        GetNode<Node3D>("Exterior/Entrance").GlobalPosition;

    public Vector3 OperationsPosition =>
        GetNode<Node3D>("Interior/OperationsPoint").GlobalPosition;

    public bool CanInteractWithOperations => Business.OwnerId == WorkshopController.PlayerFactionId
        ? !Operations.IsCriticalAssetOperational
        : Operations.IsCriticalAssetOperational;

    public string OperationsPrompt => Business.OwnerId == WorkshopController.PlayerFactionId
        ? "Reparar punto operativo de La Niebla"
        : "Sabotear Bar La Niebla · no transfiere propiedad";

    public override void _Ready()
    {
        AddToGroup("la_niebla");
        Business = new BusinessState(
            Definition.BusinessId,
            Definition.DisplayName,
            Definition.InitialOwnerId);
        Operations = new BusinessOperationsState(Business.Id);
        Operations.Changed += OnOperationsChanged;
        _operationsLabel = GetNodeOrNull<Label3D>("Interior/OperationsPoint/Label");
        _operationsVisual = GetNodeOrNull<MeshInstance3D>("Interior/OperationsPoint/Visual");
        _hud = GetTree().GetFirstNodeInGroup("hud") as HudController;
        RefreshPresentation();
    }

    public override void _ExitTree()
    {
        if (Operations is not null)
        {
            Operations.Changed -= OnOperationsChanged;
        }
    }

    public void InteractWithOperations()
    {
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        if (!CanInteractWithOperations)
        {
            return;
        }

        if (Business.OwnerId == WorkshopController.PlayerFactionId)
        {
            Operations.RepairCriticalAsset();
            _hud?.ShowStatus("Bar La Niebla vuelve a estar operativo.", 4.0f);
        }
        else
        {
            Operations.SabotageCriticalAsset();
            _hud?.ShowStatus(
                "La Niebla quedó saboteado. La propiedad continúa en manos rivales.",
                5.0f);
        }
    }

    public void ResetBusinessEncounter()
    {
        Business.Capture(Definition.InitialOwnerId);
        Operations.Restore(true, true);
        RefreshPresentation();
    }

    public void RestorePersistentState(string ownerFactionId, bool criticalAssetOperational)
    {
        Business.Capture(ownerFactionId);
        Operations.Restore(true, criticalAssetOperational);
        RefreshPresentation();
    }

    private void OnOperationsChanged() => RefreshPresentation();

    private void RefreshPresentation()
    {
        if (Operations is null)
        {
            return;
        }

        if (_operationsLabel is not null)
        {
            _operationsLabel.Text = Operations.IsCriticalAssetOperational
                ? "PUNTO OPERATIVO · LA NIEBLA"
                : "LA NIEBLA · SABOTEADO";
            _operationsLabel.Modulate = Operations.IsCriticalAssetOperational
                ? new Color(0.45f, 0.95f, 0.78f)
                : new Color(1.0f, 0.35f, 0.28f);
        }

        if (_operationsVisual is not null)
        {
            _operationsVisual.Scale = Operations.IsCriticalAssetOperational
                ? Vector3.One
                : new Vector3(1.0f, 0.25f, 1.0f);
        }
    }
}
