using Godot;

namespace SpinalShatter.Scripts.Environment
{
    public partial class HealthFountain : StaticBody3D
    {
        [Export] public int BaseHealCost { get; private set; } = 25;
        [Export] private float CostPerMissingHealthPoint { get; set; } = 1.5f;

        private Area3D _interactionArea;
        private bool _playerInRange = false;

        public override void _Ready()
        {
            _interactionArea = GetNode<Area3D>("InteractionArea3D");
            _interactionArea.BodyEntered += OnBodyEntered;
            _interactionArea.BodyExited += OnBodyExited;
        }

        public override void _Input(InputEvent @event)
        {
            if (!_playerInRange || !@event.IsActionPressed("Player_Interact")) return;

            var player = PlayerBody.Instance;
            if (player == null || player.DeadNow) return;

            TryHealPlayer(player);
        }

        private void OnBodyEntered(Node3D body)
        {
            if (body is PlayerBody player)
            {
                _playerInRange = true;
                UpdateInteractionPrompt(player);
            }
        }

        private void OnBodyExited(Node3D body)
        {
            if (body is PlayerBody player)
            {
                _playerInRange = false;
                player.HideInteractionPrompt();
            }
        }

        private void UpdateInteractionPrompt(PlayerBody player)
        {
            if (!_playerInRange || player == null || player.DeadNow)
            {
                player?.HideInteractionPrompt();
                return;
            }

            if (player.HealthComponent.CurrentHealth >= player.HealthComponent.MaxHealth)
            {
                player.ShowPromptToPress("Player_Interact", "Health Full!", "");
            }
            else
            {
                int cost = CalculateHealCost(player);
                player.ShowPromptToPress("Player_Interact", $"Heal for ${cost}", "Press");
            }
        }

        private int CalculateHealCost(PlayerBody player)
        {
            float missingHealth = player.HealthComponent.MaxHealth - player.HealthComponent.CurrentHealth;
            if (missingHealth <= 0) return 0;

            // Non-linear cost: base cost + extra cost based on how much health is missing.
            int dynamicCost = (int)(missingHealth * CostPerMissingHealthPoint);
            return BaseHealCost + dynamicCost;
        }

        private void TryHealPlayer(PlayerBody player)
        {
            if (player.HealthComponent.CurrentHealth >= player.HealthComponent.MaxHealth)
            {
                // Optionally provide feedback that health is full
                return;
            }

            int cost = CalculateHealCost(player);
            if (player.SpendMoney(cost))
            {
                player.HealthComponent.Refill();
                // TODO: Add successful healing sound/visual effect.
                UpdateInteractionPrompt(player); // Update prompt to show "Health Full!"
            }
            else
            {
                // Not enough money, show feedback
                player.ShowPromptToPress("Player_Interact", "Not Enough Money!", "Can't Buy");
                GetTree().CreateTimer(1.0f).Timeout += () => UpdateInteractionPrompt(player);
            }
        }
    }
}
