namespace Elythia;

using Godot;
using System;

public partial class PropertyEntry : HBoxContainer
{

	[Export] private Label propertyLabel;
	public Label PropertyLabel
	{
		get
		{
			if (propertyLabel == null) GetPropertyLabel();
			return propertyLabel;
		}
		set => propertyLabel = value;
	}
	[Export]
	public string PropertyText
	{
		get => PropertyLabel.Text ?? string.Empty;
		set
		{
			if (PropertyLabel != null) PropertyLabel.Text = value;
		}
	}

	[Export] private Label valueLabel;
	public Label ValueLabel
	{
		get
		{
			if (valueLabel == null) GetValueLabel();
			return valueLabel;
		}
		set => valueLabel = value;
	}
	[Export]
	public string ValueText
	{
		get => ValueLabel.Text ?? string.Empty;
		set
		{
			if (ValueLabel != null) ValueLabel.Text = value;
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetPropertyLabel();
		GetValueLabel();
	}

	private void GetPropertyLabel()
	{
		if (!this.IsReady()) return;
		propertyLabel ??= GetNode<Label>("%PropertyLabel");
	}


	private void GetValueLabel()
	{
		if (!this.IsReady()) return;
		valueLabel ??= GetNode<Label>("%ValueLabel");
	}

	public void UpdateValueText(string which, string value)
	{
		if (which != PropertyText) return;
		ValueText = value;
	}
}