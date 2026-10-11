using Godot;
using Omerta.Domain.Combat.AI;

namespace Omerta.Presentation;

public sealed partial class HudController : CanvasLayer
{
    private Label? _interactionLabel;
    private Label? _statusLabel;
    private Label? _stanceLabel;
    private Label? _healthLabel;
    private Label? _weaponLabel;
    private Label? _enemyLabel;
    private Label? _aimLabel;
    private Label? _economyLabel;
    private Label? _businessLabel;
    private Label? _missionLabel;
    private Label? _squadLabel;
    private Label? _policeLabel;
    private Label? _counterattackLabel;
    private float _statusRemaining;

    public override void _Ready()
    {
        AddToGroup("hud");
        _interactionLabel = GetNode<Label>("InteractionPrompt");
        _statusLabel = GetNode<Label>("StatusMessage");
        _stanceLabel = GetNode<Label>("MovementState");
        _healthLabel = GetNode<Label>("Health");
        _weaponLabel = GetNode<Label>("Weapon");
        _enemyLabel = GetNode<Label>("EnemyState");
        _aimLabel = GetNode<Label>("AimState");
        _economyLabel = GetNode<Label>("Economy");
        _businessLabel = GetNode<Label>("BusinessState");
        _missionLabel = GetNode<Label>("MissionState");
        _squadLabel = GetNode<Label>("SquadState");
        _policeLabel = GetNode<Label>("PoliceState");
        _counterattackLabel = GetNode<Label>("CounterattackState");
    }

    public override void _Process(double delta)
    {
        if (_statusRemaining <= 0.0f || _statusLabel is null)
        {
            return;
        }

        _statusRemaining -= (float)delta;
        if (_statusRemaining <= 0.0f)
        {
            _statusLabel.Text = string.Empty;
        }
    }

    public void SetInteractionPrompt(string text)
    {
        if (_interactionLabel is not null)
        {
            _interactionLabel.Text = text;
        }
    }

    public void ShowStatus(string text, float duration = 3.0f)
    {
        if (_statusLabel is not null)
        {
            _statusLabel.Text = text;
            _statusRemaining = duration;
        }
    }

    public void SetMovementState(bool crouched, bool sprinting)
    {
        if (_stanceLabel is null)
        {
            return;
        }

        _stanceLabel.Text = crouched
            ? "POSTURA: AGACHADO"
            : sprinting
                ? "POSTURA: CORRIENDO"
                : "POSTURA: DE PIE";
    }

    public void SetHealth(int current, int maximum)
    {
        if (_healthLabel is not null)
        {
            _healthLabel.Text = $"SALUD  {current}/{maximum}";
        }
    }

    public void SetWeapon(string name, int magazine, int reserve, bool reloading)
    {
        if (_weaponLabel is not null)
        {
            _weaponLabel.Text = reloading
                ? $"{name.ToUpperInvariant()}  RECARGANDO…"
                : $"{name.ToUpperInvariant()}  {magazine} / {reserve}";
        }
    }

    public void SetAimState(bool aiming)
    {
        if (_aimLabel is not null)
        {
            _aimLabel.Text = aiming ? "ADS" : "CADERA";
        }
    }

    public void SetEconomy(int balance)
    {
        if (_economyLabel is not null)
        {
            _economyLabel.Text = $"FONDOS  ${balance}";
        }
    }

    public void SetBusiness(
        string displayName,
        string owner,
        string operation,
        int pressure,
        int resistance)
    {
        if (_businessLabel is not null)
        {
            _businessLabel.Text =
                $"{displayName.ToUpperInvariant()}\nDUEÑO: {owner} · {operation}\nPRESIÓN: {pressure}/{resistance}";
        }
    }

    public void SetEnemyState(int current, int maximum, EnemyAwareness awareness, float progress)
    {
        if (_enemyLabel is null)
        {
            return;
        }

        string state = awareness switch
        {
            EnemyAwareness.Alert => "ALERTA",
            EnemyAwareness.Suspicious => $"SOSPECHA {Mathf.RoundToInt(progress * 100.0f)}%",
            _ => "SIN ALERTA"
        };
        _enemyLabel.Text = $"RIVAL  {current}/{maximum} · {state}";
    }

    public void SetMission(string objective, string status)
    {
        if (_missionLabel is not null)
        {
            _missionLabel.Text = $"ENCARGO · {status.ToUpperInvariant()}\n{objective}";
        }
    }

    public void SetSquad(string order, int activeCount)
    {
        if (_squadLabel is not null)
        {
            _squadLabel.Text = $"CREW {activeCount} · {order}";
        }
    }

    public void SetPolice(string status, int wantedLevel, float remainingSeconds)
    {
        if (_policeLabel is null)
        {
            return;
        }

        bool active = wantedLevel > 0;
        _policeLabel.Visible = active;
        _policeLabel.Text = active
            ? $"POLICÍA · ALERTA {wantedLevel} · {status.ToUpperInvariant()} · {remainingSeconds:0.0}s"
            : string.Empty;
    }

    public void SetCounterattack(string status, float remainingSeconds)
    {
        if (_counterattackLabel is null)
        {
            return;
        }

        bool active = status is "Warning" or "InProgress";
        _counterattackLabel.Visible = active;
        _counterattackLabel.Text = active
            ? $"REPRESALIA · {status.ToUpperInvariant()} · {remainingSeconds:0.0}s"
            : string.Empty;
    }
}
