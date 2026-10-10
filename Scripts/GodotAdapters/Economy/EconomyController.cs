using Godot;
using Omerta.Domain.Economy;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Economy;

public sealed partial class EconomyController : Node
{
    [Export]
    public int StartingMoney { get; set; } = 100;

    private HudController? _hud;

    public EconomyService Economy { get; private set; } = null!;

    public override void _Ready()
    {
        AddToGroup("economy");
        Economy = new EconomyService(StartingMoney);
        Economy.BalanceChanged += OnBalanceChanged;
        UpdateHud();
    }

    public override void _Process(double delta)
    {
        if (_hud is null)
        {
            UpdateHud();
        }
    }

    public override void _ExitTree()
    {
        if (Economy is not null)
        {
            Economy.BalanceChanged -= OnBalanceChanged;
        }
    }

    public void ResetEconomy() => Economy.RestoreBalance(StartingMoney);

    public void RestoreBalance(int balance) => Economy.RestoreBalance(balance);

    private void OnBalanceChanged(int previous, int current) => UpdateHud();

    private void UpdateHud()
    {
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        _hud?.SetEconomy(Economy?.Balance ?? StartingMoney);
    }
}
