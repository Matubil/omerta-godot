using Godot;

namespace Omerta.Presentation;

public sealed partial class HudController : CanvasLayer
{
    private Label? _interactionLabel;
    private Label? _statusLabel;
    private Label? _stanceLabel;
    private float _statusRemaining;

    public override void _Ready()
    {
        AddToGroup("hud");
        _interactionLabel = GetNode<Label>("InteractionPrompt");
        _statusLabel = GetNode<Label>("StatusMessage");
        _stanceLabel = GetNode<Label>("MovementState");
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
}
