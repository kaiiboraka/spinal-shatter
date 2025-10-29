using Godot;

public partial class HealthBar : Sprite3D
{
    [Export] private float _renderDistance = 25.0f;

    private ProgressBar _progressBar;
    private Camera3D _camera;

    public override void _Ready()
    {
        _progressBar = GetNode<ProgressBar>("SubViewport/ProgressBar");
        Visible = false; // Start invisible
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        
        if (_camera == null && PlayerBody.Instance != null)
        {
            _camera = PlayerBody.Instance.GetViewport().GetCamera3D();
        }
        if (_camera == null) return;

        var distance = this.GlobalPosition.DistanceTo(_camera.GlobalPosition);
        float alpha = Mathf.Clamp(1.0f - (distance / _renderDistance), 0.0f, 1.0f);
        
        // Fade and darken with distance
        this.Modulate = new Color(alpha, alpha, alpha, alpha);
    }

    public void Initialize(float maxHealth)
    {
        _progressBar.MaxValue = maxHealth;
        _progressBar.Value = maxHealth;
    }

    public void OnHealthChanged(float currentHealth, float maxHealth)
    {
        if (!Visible && currentHealth < maxHealth)
        {
            Visible = true;
            Initialize(maxHealth);
        }
        _progressBar.Value = currentHealth;
    }
}
