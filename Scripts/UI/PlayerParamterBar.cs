using Godot;

public partial class PlayerParamterBar : TextureProgressBar
{
    public void OnParameterChanged(float currentValue, float maxValue)
    {
        MaxValue = maxValue;
        Value = currentValue;
    }
}
