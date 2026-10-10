using Godot;
using Omerta.Domain.Businesses;

namespace Omerta.GodotAdapters.Businesses;

[GlobalClass]
public sealed partial class BusinessDefinitionResource : Resource
{
    [Export]
    public string BusinessId { get; set; } = "business.el-faro";

    [Export]
    public string DisplayName { get; set; } = "Taller El Faro";

    [Export]
    public string InitialOwnerId { get; set; } = "faction.circle-ferro";

    [Export]
    public int BaseIncome { get; set; } = 50;

    [Export]
    public float IncomeIntervalSeconds { get; set; } = 5.0f;

    [Export]
    public int ExtortionResistance { get; set; } = 100;

    [Export]
    public int PressurePerInteraction { get; set; } = 25;

    [Export]
    public int PropPressureReward { get; set; } = 15;

    [Export]
    public MerchantTemperament Temperament { get; set; } = MerchantTemperament.Balanced;
}
