// namespace Elythia;
// using Godot;
//
// public partial class FloatingTextManager : ObjectPoolManager<FloatingParameterText>
// {
//     public static FloatingTextManager Instance { get; private set; }
//
//     public override void _Ready()
//     {
//         if (Instance != null)
//         {
//             QueueFree(); // Another instance exists, so destroy this one.
//             return;
//         }
//         Instance = this;
//     }
//
//     public void CreateCombatText(Vector2 spawnPos, ChangeMeterContext context)
//     {
//         if ( // is enemy healing, and don't show enemy healing
//             (!context.Component.IsPlayer && context.Amount > 0)
//             && !GlobalGameState.Instance.ShowEnemyRestoringText
//             )
//         {
//             return;
//         }
//
//         FloatingParameterText textInstance = Get();
//         if (textInstance == null) return; // Pool is full
//
//         textInstance.DisplayText(context, this, spawnPos);
//     }
// }
