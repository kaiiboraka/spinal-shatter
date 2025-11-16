using Godot;

namespace SpinalShatter;

[GlobalClass]
public partial class OverheadHealthBar : Sprite3D
{
    [Export] private float _renderDistance = 25.0f;

    private SubViewport viewport;
    private ProgressBar progressBar;
    private Camera3D _camera;

    public async override void _Ready()
    {
        Visible = false; // Start invisible
        viewport = GetNode<SubViewport>("HealthbarViewport");
        progressBar = viewport.GetNode<ProgressBar>("MarginContainer/LifeBar");

        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        this.Texture = viewport.GetTexture();
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

    public void OnHealthChanged(float currentHealth, float maxHealth)
    {
        if (!Visible)
        {
            Visible = true;
            progressBar.MaxValue = maxHealth;
        }
        progressBar.Value = currentHealth;
    }
}
