using Godot;
using Omerta.Domain.Businesses;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Economy;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Businesses;

public sealed partial class WorkshopController : Node3D,
    IBusinessOperationsHost,
    IBusinessMerchantHost,
    IBusinessPressureHost
{
    public const string PlayerFactionId = "faction.player-family";

    [Export]
    public BusinessDefinitionResource Definition { get; set; } = null!;

    private BusinessIncomeService? _income;
    private EconomyController? _economy;
    private HudController? _hud;
    private Combatant? _rivalCombatant;
    private Label3D? _operationsLabel;
    private MeshInstance3D? _operationsVisual;
    private bool _suppressStatus;

    public BusinessState Business { get; private set; } = null!;

    public BusinessOperationsState Operations { get; private set; } = null!;

    public ExtortionSession Extortion { get; private set; } = null!;

    public ForcedTakeoverSession ForcedTakeover { get; private set; } = null!;

    public override void _Ready()
    {
        AddToGroup("workshop");
        Business = new BusinessState(
            Definition.BusinessId,
            Definition.DisplayName,
            Definition.InitialOwnerId);
        Operations = new BusinessOperationsState(Business.Id);
        Extortion = new ExtortionSession(
            Business,
            PlayerFactionId,
            Definition.ExtortionResistance,
            Definition.Temperament);
        ForcedTakeover = new ForcedTakeoverSession(Business, PlayerFactionId, 1);
        Business.OwnerChanged += OnOwnerChanged;
        Operations.Changed += OnOperationsChanged;
        Extortion.PressureChanged += OnPressureChanged;
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
        if (_rivalCombatant is not null)
        {
            _rivalCombatant.Defeated -= OnRivalDefeated;
        }

        if (_income is not null)
        {
            _income.IncomeCollected -= OnIncomeCollected;
        }
    }

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
                return "El comerciante no puede responder";
            }

            if (ForcedTakeover.CanDemandSurrender)
            {
                return $"Exigir rendición de {Definition.DisplayName}";
            }

            string action = Extortion.Reaction switch
            {
                MerchantExtortionReaction.Calm => "Presionar al comerciante",
                MerchantExtortionReaction.Defiant => "Redoblar la amenaza",
                MerchantExtortionReaction.AlarmedByDamage => "Aprovechar el miedo por los destrozos",
                MerchantExtortionReaction.Frightened => "Exigir acuerdo de protección",
                MerchantExtortionReaction.NearSurrender => "Dar ultimátum",
                _ => "Presionar al comerciante"
            };
            string consequence = Extortion.HasExcessiveViolence
                ? " · quedará dañado"
                : string.Empty;
            return $"{action} ({Extortion.Pressure}/{Extortion.Resistance}){consequence}";
        }
    }

    public bool CanInteractWithOperations => Business.OwnerId == PlayerFactionId
        ? !Operations.IsCriticalAssetOperational
        : Operations.IsCriticalAssetOperational;

    public string OperationsPrompt => Business.OwnerId == PlayerFactionId
        ? "Reparar punto operativo"
        : "Sabotear punto operativo";

    public void InteractWithMerchant()
    {
        if (!CanInteractWithMerchant)
        {
            return;
        }

        if (ForcedTakeover.CanDemandSurrender)
        {
            bool applyViolenceConsequence = Extortion.HasExcessiveViolence;
            if (ForcedTakeover.DemandSurrender())
            {
                Extortion.SynchronizeWithBusinessOwner();
                if (applyViolenceConsequence)
                {
                    Operations.SabotageCriticalAsset();
                }

                _hud?.ShowStatus("El rival cayó. Rafael entrega Taller El Faro.", 5.0f);
            }

            return;
        }

        bool captured = Extortion.ApplyPressure(Definition.PressurePerInteraction);
        if (captured && Extortion.HasExcessiveViolence)
        {
            Operations.SabotageCriticalAsset();
        }

        _hud?.ShowStatus(captured
            ? "Taller El Faro ahora paga protección a la familia."
            : GetMerchantResponse(), 4.0f);
    }

    public void InteractWithOperations()
    {
        if (!CanInteractWithOperations)
        {
            return;
        }

        if (Business.OwnerId == PlayerFactionId)
        {
            Operations.RepairCriticalAsset();
            _hud?.ShowStatus("Punto operativo reparado. El taller vuelve a producir.", 4.0f);
        }
        else
        {
            Operations.SabotageCriticalAsset();
            _hud?.ShowStatus("Punto operativo saboteado. La propiedad sigue siendo rival.", 4.0f);
        }
    }

    public void NotifyMerchantAvailability(bool available)
    {
        if (Operations.SetOperatorAvailable(available) && !available)
        {
            _hud?.ShowStatus("El comerciante cayó. El taller no produce ni puede rendirse.", 5.0f);
        }
    }

    public void NotifyPressurePropBroken()
    {
        int applied = Extortion.ApplyEnvironmentalPressure(Definition.PropPressureReward);
        if (applied > 0)
        {
            _hud?.ShowStatus(
                $"Destrozo: +{applied} presión. Aún hace falta una exigencia directa.",
                4.0f);
        }
    }

    public void ResetBusinessEncounter()
    {
        _suppressStatus = true;
        Business.Capture(Definition.InitialOwnerId);
        Operations.Restore(true, true);
        ForcedTakeover.SetDefenderDefeated(0, false);
        Extortion.SynchronizeWithBusinessOwner();
        _income?.ResetTiming();
        GetNodeOrNull<MerchantController>("Interior/Merchant")?.ResetMerchant();
        GetNodeOrNull<PressurePropController>("Interior/MerchandiseA")?.ResetProp();
        GetNodeOrNull<PressurePropController>("Interior/MerchandiseB")?.ResetProp();
        _suppressStatus = false;
        RefreshPresentation();
    }

    public int MerchantHealth =>
        GetNodeOrNull<MerchantController>("Interior/Merchant")?.CurrentHealth ?? 0;

    public void RestorePersistentState(
        string ownerFactionId,
        bool criticalAssetOperational,
        int merchantHealth,
        bool defenderDefeated)
    {
        _suppressStatus = true;
        Business.Capture(ownerFactionId);
        Operations.Restore(merchantHealth > 0, criticalAssetOperational);
        ForcedTakeover.SetDefenderDefeated(0, defenderDefeated);
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
                    PlayerFactionId,
                    _economy.Economy,
                    Business,
                    Operations,
                    Definition.BaseIncome,
                    Definition.IncomeIntervalSeconds);
                _income.IncomeCollected += OnIncomeCollected;
            }
        }

        if (_rivalCombatant is null)
        {
            Node? rival = GetTree().GetFirstNodeInGroup("rival");
            Combatant? combatant = rival?.GetNodeOrNull<Combatant>("Combatant");
            if (combatant?.Health is not null)
            {
                _rivalCombatant = combatant;
                _rivalCombatant.Defeated += OnRivalDefeated;
            }
        }

        RefreshPresentation();
    }

    private string GetMerchantResponse() => Extortion.Reaction switch
    {
        MerchantExtortionReaction.Defiant => "Rafael: «No van a quedarse con mi taller.»",
        MerchantExtortionReaction.AlarmedByDamage => "Rafael mira los destrozos, pero aún resiste.",
        MerchantExtortionReaction.Frightened => "Rafael empieza a considerar el acuerdo.",
        MerchantExtortionReaction.NearSurrender => "Rafael está al límite. Un ultimátum cerrará el trato.",
        _ => "La presión sobre Taller El Faro aumenta."
    };

    private void OnRivalDefeated(Domain.Combat.Damage.DamageInfo damage)
    {
        ForcedTakeover.SetDefenderDefeated(0, true);
        _hud?.ShowStatus("Defensor derrotado. Entrá al taller y exigí la rendición.", 5.0f);
        RefreshPresentation();
    }

    private void OnOwnerChanged(string previous, string current)
    {
        if (!_suppressStatus)
        {
            _hud?.ShowStatus("Propiedad transferida: Taller El Faro es de la familia.", 5.0f);
        }

        RefreshPresentation();
    }

    private void OnOperationsChanged() => RefreshPresentation();

    private void OnPressureChanged(int pressure, int resistance) => RefreshPresentation();

    private void OnIncomeCollected(int amount) =>
        _hud?.ShowStatus($"Taller El Faro generó ${amount}.", 3.0f);

    private void RefreshPresentation()
    {
        if (Business is null || Operations is null || Extortion is null)
        {
            return;
        }

        string owner = Business.OwnerId == PlayerFactionId ? "FAMILIA" : "CÍRCULO FERRO";
        string operation = Operations.IsOperational ? "OPERATIVO" : "INACTIVO";
        _hud?.SetBusiness(
            Definition.DisplayName,
            owner,
            operation,
            Extortion.Pressure,
            Extortion.Resistance);

        if (_operationsLabel is not null)
        {
            _operationsLabel.Text = Operations.IsCriticalAssetOperational
                ? "PUNTO OPERATIVO"
                : "PUNTO SABOTEADO";
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
