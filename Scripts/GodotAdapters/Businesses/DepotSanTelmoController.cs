using Godot;
using Omerta.Domain.Businesses;
using Omerta.GodotAdapters.Economy;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Businesses;

public sealed partial class DepotSanTelmoController : Node3D,
    IBusinessOperationsHost,
    IBusinessMerchantHost,
    IBusinessPressureHost
{
    [Export]
    public BusinessDefinitionResource Definition { get; set; } = null!;

    private BusinessIncomeService? _income;
    private EconomyController? _economy;
    private HudController? _hud;
    private Label3D? _businessLabel;
    private Label3D? _operationsLabel;
    private MeshInstance3D? _operationsVisual;
    private bool _suppressStatus;

    public BusinessState Business { get; private set; } = null!;

    public BusinessOperationsState Operations { get; private set; } = null!;

    public ExtortionSession Extortion { get; private set; } = null!;

    public bool CanInteractWithMerchant =>
        !Extortion.IsComplete && Operations.IsOperatorAvailable;

    public string MerchantPrompt
    {
        get
        {
            if (Extortion.IsComplete)
            {
                return $"{Definition.DisplayName} ya pertenece a la familia";
            }

            if (!Operations.IsOperatorAvailable)
            {
                return "El encargado no puede responder";
            }

            string action = Extortion.Reaction switch
            {
                MerchantExtortionReaction.Calm => "Presionar al encargado",
                MerchantExtortionReaction.Defiant => "Redoblar la amenaza",
                MerchantExtortionReaction.AlarmedByDamage => "Aprovechar los destrozos",
                MerchantExtortionReaction.Frightened => "Exigir el acuerdo de protección",
                MerchantExtortionReaction.NearSurrender => "Dar el ultimátum",
                _ => "Presionar al encargado"
            };
            return $"{action} ({Extortion.Pressure}/{Extortion.Resistance})";
        }
    }

    public bool CanInteractWithOperations => Business.OwnerId == WorkshopController.PlayerFactionId
        ? !Operations.IsCriticalAssetOperational
        : Operations.IsCriticalAssetOperational;

    public string OperationsPrompt => Business.OwnerId == WorkshopController.PlayerFactionId
        ? "Reparar punto operativo del depósito"
        : "Sabotear punto operativo del depósito";

    public int MerchantHealth =>
        GetNodeOrNull<MerchantController>("Interior/Merchant")?.CurrentHealth ?? 0;

    public override void _Ready()
    {
        AddToGroup("san_telmo");
        Business = new BusinessState(
            Definition.BusinessId,
            Definition.DisplayName,
            Definition.InitialOwnerId);
        Operations = new BusinessOperationsState(Business.Id);
        Extortion = new ExtortionSession(
            Business,
            WorkshopController.PlayerFactionId,
            Definition.ExtortionResistance,
            Definition.Temperament);
        Business.OwnerChanged += OnOwnerChanged;
        Operations.Changed += OnOperationsChanged;
        Extortion.PressureChanged += OnPressureChanged;
        _businessLabel = GetNodeOrNull<Label3D>("Exterior/BusinessLabel");
        _operationsLabel = GetNodeOrNull<Label3D>("Interior/OperationsPoint/Label");
        _operationsVisual = GetNodeOrNull<MeshInstance3D>("Interior/OperationsPoint/Visual");
        ResolveRuntimeLinks();
        RefreshPresentation();
    }

    public override void _Process(double delta)
    {
        ResolveRuntimeLinks();
        _income?.Tick((float)delta);
    }

    public override void _ExitTree()
    {
        Business.OwnerChanged -= OnOwnerChanged;
        Operations.Changed -= OnOperationsChanged;
        Extortion.PressureChanged -= OnPressureChanged;
        if (_income is not null)
        {
            _income.IncomeCollected -= OnIncomeCollected;
        }
    }

    public void InteractWithMerchant()
    {
        if (!CanInteractWithMerchant)
        {
            return;
        }

        bool captured = Extortion.ApplyPressure(Definition.PressurePerInteraction);
        _hud?.ShowStatus(captured
            ? "Depósito San Telmo ahora paga protección a la familia."
            : GetMerchantResponse(), 4.0f);
    }

    public void InteractWithOperations()
    {
        if (!CanInteractWithOperations)
        {
            return;
        }

        if (Business.OwnerId == WorkshopController.PlayerFactionId)
        {
            Operations.RepairCriticalAsset();
            _hud?.ShowStatus("El depósito vuelve a despachar mercadería.", 4.0f);
        }
        else
        {
            Operations.SabotageCriticalAsset();
            _hud?.ShowStatus(
                "Depósito San Telmo quedó saboteado, pero continúa en manos rivales.",
                5.0f);
        }
    }

    public void NotifyMerchantAvailability(bool available)
    {
        if (Operations.SetOperatorAvailable(available) && !available)
        {
            _hud?.ShowStatus("El encargado cayó. El depósito dejó de producir.", 5.0f);
        }
    }

    public void NotifyPressurePropBroken()
    {
        int applied = Extortion.ApplyEnvironmentalPressure(Definition.PropPressureReward);
        if (applied > 0)
        {
            _hud?.ShowStatus(
                $"Mercadería destruida: +{applied} presión. Falta una exigencia directa.",
                4.0f);
        }
    }

    public void ResetBusinessEncounter()
    {
        _suppressStatus = true;
        Business.Capture(Definition.InitialOwnerId);
        Operations.Restore(true, true);
        Extortion.SynchronizeWithBusinessOwner();
        _income?.ResetTiming();
        GetNodeOrNull<MerchantController>("Interior/Merchant")?.ResetMerchant();
        GetNodeOrNull<PressurePropController>("Interior/MerchandiseA")?.ResetProp();
        GetNodeOrNull<PressurePropController>("Interior/MerchandiseB")?.ResetProp();
        _suppressStatus = false;
        RefreshPresentation();
    }

    public void RestorePersistentState(
        string ownerFactionId,
        bool criticalAssetOperational,
        int merchantHealth)
    {
        _suppressStatus = true;
        Business.Capture(ownerFactionId);
        Operations.Restore(merchantHealth > 0, criticalAssetOperational);
        Extortion.SynchronizeWithBusinessOwner();
        _income?.ResetTiming();
        GetNodeOrNull<MerchantController>("Interior/Merchant")?.RestoreHealth(merchantHealth);
        GetNodeOrNull<PressurePropController>("Interior/MerchandiseA")?.ResetProp();
        GetNodeOrNull<PressurePropController>("Interior/MerchandiseB")?.ResetProp();
        _suppressStatus = false;
        RefreshPresentation();
    }

    private void ResolveRuntimeLinks()
    {
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        if (_income is null)
        {
            _economy ??= GetTree().GetFirstNodeInGroup("economy") as EconomyController;
            if (_economy?.Economy is not null)
            {
                _income = new BusinessIncomeService(
                    WorkshopController.PlayerFactionId,
                    _economy.Economy,
                    Business,
                    Operations,
                    Definition.BaseIncome,
                    Definition.IncomeIntervalSeconds);
                _income.IncomeCollected += OnIncomeCollected;
            }
        }
    }

    private string GetMerchantResponse() => Extortion.Reaction switch
    {
        MerchantExtortionReaction.Defiant => "Vittorio: «Este depósito no cambia de manos.»",
        MerchantExtortionReaction.AlarmedByDamage => "Vittorio mira las cajas rotas y pierde firmeza.",
        MerchantExtortionReaction.Frightened => "Vittorio empieza a considerar el acuerdo.",
        MerchantExtortionReaction.NearSurrender => "Vittorio está al límite. Falta un ultimátum.",
        _ => "La presión sobre Depósito San Telmo aumenta."
    };

    private void OnOwnerChanged(string previous, string current)
    {
        if (!_suppressStatus)
        {
            _hud?.ShowStatus("Propiedad transferida: Depósito San Telmo es de la familia.", 5.0f);
        }

        RefreshPresentation();
    }

    private void OnOperationsChanged() => RefreshPresentation();

    private void OnPressureChanged(int pressure, int resistance) => RefreshPresentation();

    private void OnIncomeCollected(int amount) =>
        _hud?.ShowStatus($"Depósito San Telmo generó ${amount}.", 3.0f);

    private void RefreshPresentation()
    {
        if (Business is null || Operations is null || Extortion is null)
        {
            return;
        }

        string owner = Business.OwnerId == WorkshopController.PlayerFactionId
            ? "FAMILIA"
            : "CÍRCULO FERRO";
        if (_businessLabel is not null)
        {
            _businessLabel.Text = $"DEPÓSITO SAN TELMO\n{owner} · {Extortion.Pressure}/{Extortion.Resistance}";
            _businessLabel.Modulate = Business.OwnerId == WorkshopController.PlayerFactionId
                ? new Color(0.55f, 1.0f, 0.58f)
                : new Color(1.0f, 0.66f, 0.24f);
        }

        if (_operationsLabel is not null)
        {
            _operationsLabel.Text = Operations.IsCriticalAssetOperational
                ? "PUNTO OPERATIVO · SAN TELMO"
                : "SAN TELMO · SABOTEADO";
            _operationsLabel.Modulate = Operations.IsCriticalAssetOperational
                ? new Color(0.55f, 1.0f, 0.58f)
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
