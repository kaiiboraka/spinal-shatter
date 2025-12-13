using Godot;

namespace SpinalShatter;

public partial class WeaponSlotter : CanvasLayer
{
    public static WeaponSlotter Instance;

    [Signal]
    public delegate void SlotSelectedEventHandler(SlotType slot, SpellData spell);

    private RichTextLabel _promptLabel;
    private SpellData _spellToSlot;

    public override void _Ready()
    {
        if (Instance == null) Instance = this;
        else QueueFree();
        
        _promptLabel = GetNode<RichTextLabel>("%Prompt_RichTextLabel");
        Hide();
        ProcessMode = ProcessModeEnum.Disabled;
    }

    public void BeginSlotSelection(SpellData spell)
    {
        if (spell == null)
        {
            GD.PrintErr("WeaponSlotter: BeginSlotSelection called with null spell.");
            return;
        }

        _spellToSlot = spell;

        string primaryKey = "Player_Shoot".GetActionKeyName();
        string secondaryKey = "Player_AltFire".GetActionKeyName();
        string automaticKey = "Player_Siphon".GetActionKeyName();

        _promptLabel.Text = $"[center]Press [{primaryKey}] for Primary, [{secondaryKey}] for Secondary, or [{automaticKey}] for Automatic slot.[/center]";
        Show();
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Input(InputEvent @event)
    {
        if (ProcessMode == ProcessModeEnum.Disabled)
        {
            return;
        }

        SlotType chosenSlot = SlotType.None;

        if (@event.IsActionPressed("Player_Shoot"))
        {
            chosenSlot = SlotType.Primary;
        }
        else if (@event.IsActionPressed("Player_AltFire"))
        {
            chosenSlot = SlotType.Secondary;
        }
        else if (@event.IsActionPressed("Player_Siphon"))
        {
            chosenSlot = SlotType.Automatic;
        }

        if (chosenSlot != SlotType.None)
        {
            EmitSignal(SignalName.SlotSelected, (int)chosenSlot, _spellToSlot);
            _spellToSlot = null;
            Hide();
            ProcessMode = ProcessModeEnum.Disabled;
        }
    }
}