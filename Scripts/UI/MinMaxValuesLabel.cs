using Godot;

namespace SpinalShatter;

[GlobalClass, Tool]
public partial class MinMaxValuesLabel : HBoxContainer
{
    // Node references
    [ExportCategory("Components")]
    [ExportSubgroup("Labels", "label")]
    [Export] private Label labelCurrent;
    [Export] private Label labelDivider;
    [Export] private Label labelMax;
    [ExportSubgroup("Panels", "panel")]
    [Export] private Panel panelCurrent;
    [Export] private Panel panelDivider;
    [Export] private Panel panelMax;

    // Backing fields for properties
    private bool _toggleCurrent = true;
    private bool _toggleDivider = true;
    private bool _toggleMax = true;
    private string _currentText = "0";
    private string _divider = "/";
    private string _maximumText = "9999";
    private HorizontalAlignment _alignmentCurrent = HorizontalAlignment.Right;
    private HorizontalAlignment _alignmentDivider = HorizontalAlignment.Center;
    private HorizontalAlignment _alignmentMax = HorizontalAlignment.Left;


    [ExportCategory("Controls")]
    [ExportSubgroup("Toggles", "Toggle")]
    [Export]
    public bool ToggleCurrent
    {
        get => _toggleCurrent;
        set
        {
            _toggleCurrent = value;
            if (panelCurrent != null)
            {
                panelCurrent.Visible = value;
            }
        }
    }

    [Export]
    public bool ToggleDivider
    {
        get => _toggleDivider;
        set
        {
            _toggleDivider = value;
            if (panelDivider != null)
            {
                panelDivider.Visible = value;
            }
        }
    }

    [Export]
    public bool ToggleMax
    {
        get => _toggleMax;
        set
        {
            _toggleMax = value;
            if (panelMax != null)
            {
                panelMax.Visible = value;
            }
        }
    }

    [ExportSubgroup("Text", "Text")]
    [Export]
    public string TextCurrent
    {
        get => _currentText;
        set
        {
            _currentText = value;
            if (labelCurrent != null)
            {
                labelCurrent.Text = value;
            }
        }
    }

    [Export]
    public string TextDivider
    {
        get => _divider;
        set
        {
            _divider = value;
            if (labelDivider != null)
            {
                labelDivider.Text = value;
            }
        }
    }

    [Export]
    public string TextMaximum
    {
        get => _maximumText;
        set
        {
            _maximumText = value;
            if (labelMax != null)
            {
                labelMax.Text = value;
            }
        }
    }

    private int _textSize = 16;
    [Export]
    public int TextSize
    {
        get => _textSize;
        set
        {
            _textSize = value;
            labelCurrent?.AddThemeFontSizeOverride("font_size", _textSize);
            labelDivider?.AddThemeFontSizeOverride("font_size", _textSize);
            labelMax?.AddThemeFontSizeOverride("font_size", _textSize);
        }
    }

    // Alignment
    [ExportSubgroup("Alignment", "Alignment")]
    [Export]
    public HorizontalAlignment AlignmentCurrent
    {
        get => _alignmentCurrent;
        set
        {
            _alignmentCurrent = value;
            if (labelCurrent != null)
            {
                labelCurrent.HorizontalAlignment = value;
            }
        }
    }

    [Export]
    public HorizontalAlignment AlignmentDivider
    {
        get => _alignmentDivider;
        set
        {
            _alignmentDivider = value;
            if (labelDivider != null)
            {
                labelDivider.HorizontalAlignment = value;
            }
        }
    }

    [Export]
    public HorizontalAlignment AlignmentMax
    {
        get => _alignmentMax;
        set
        {
            _alignmentMax = value;
            if (labelMax != null)
            {
                labelMax.HorizontalAlignment = value;
            }
        }
    }

    public override void _Ready()
    {
        // This method ensures the nodes are found and their properties are set
        // both when the scene runs and when it's displayed in the editor.
        GetComponents();
        UpdateNodeProperties();
    }

    private void GetComponents()
    {
        // Node references are assigned via export, but we can use unique names as a fallback.
        labelCurrent ??= GetNode<Label>("%LabelCurrent");
        labelDivider ??= GetNode<Label>("%LabelDivider");
        labelMax ??= GetNode<Label>("%LabelMax");
        panelCurrent ??= GetNode<Panel>("%PanelCurrent");
        panelDivider ??= GetNode<Panel>("%PanelDivider");
        panelMax ??= GetNode<Panel>("%PanelMax");
    }

    private void UpdateNodeProperties()
    {
        // Apply all backed properties to the nodes.
        if (panelCurrent != null) panelCurrent.Visible = _toggleCurrent;
        if (panelDivider != null) panelDivider.Visible = _toggleDivider;
        if (panelMax != null) panelMax.Visible = _toggleMax;

        if (labelCurrent != null)
        {
            labelCurrent.Text = _currentText;
            labelCurrent.HorizontalAlignment = _alignmentCurrent;
        }

        if (labelDivider != null)
        {
            labelDivider.Text = _divider;
            labelDivider.HorizontalAlignment = _alignmentDivider;
        }

        if (labelMax != null)
        {
            labelMax.Text = _maximumText;
            labelMax.HorizontalAlignment = _alignmentMax;
        }
    }
}
