// using System;
// using Godot;
// using Godot.Collections;
//
// namespace Elythia;
//
// public partial class ItemDropSpawner : ObjectPoolManager<ItemInstance>
// {
//     public static ItemDropSpawner Instance { get; private set; }
//
//     private DebugLogger DEBUG;
//     private Node _currentItemParent;
//
//     [ExportGroup("Item Spawning Parameters")]
//     [Export] public PhysicsMaterial BouncyMaterial { get; set; }
//     [Export(PropertyHint.Range, "-180,180")] public float LaunchAngle { get; set; } = -90f;
//     [Export(PropertyHint.Range, "0,360")] public float LaunchAngleSpread { get; set; } = 60f;
//     [Export] public FloatValueRange LaunchDistance { get; set; }
//     [Export] public FloatValueRange AirTime { get; set; }
//     [Export] public FloatValueRange SpinMagnitude { get; set; }
//     private FloatValueRange initialAngleRange = new(80f, 100f);
//
//     [Export] private float gravityMultiplier = 1f;
//
//     [Export] public Dictionary<ItemInstanceSize, float> SizeRatios = new()
//     {
//         { ItemInstanceSize.Large, 16.0f / 16.0f },
//         { ItemInstanceSize.Medium, 9.0f / 16.0f },
//         { ItemInstanceSize.Small, 6.0f / 16.0f },
//         { ItemInstanceSize.Tiny, 4.0f / 16.0f }
//     };
//
//     // [ExportGroup("Bubble Data")]
//     // [Export] private Dictionary<BubbleType, Dictionary<ItemInstanceSize, PrizeBubbleItemData>> prizeBubbleData = new();
//
//     private PlayerBody Player => GlobalGameState.Instance.CurrentPlayer;
//
//
//     public override void _Ready()
//     {
//         if (Instance is null)
//         {
//             Instance = this;
//         }
//         else
//         {
//             QueueFree();
//         }
//
//         DEBUG = new DebugLogger(this);
//         // LoadPrizeBubbleData();
//     }
//
//     // private void LoadPrizeBubbleData()
//     // {
//     //     prizeBubbleData.Clear();
//     //     foreach (BubbleType type in Enum.GetValues(typeof(BubbleType)))
//     //     {
//     //         var dataDict = new Dictionary<ItemInstanceSize, PrizeBubbleItemData>();
//     //         var resources = FileSystem.LoadAllResourcesOfType<PrizeBubbleItemData>(
//     //             "/Items/Bubbles/",
//     //             $"{type}");
//     //         foreach (var resource in resources)
//     //         {
//     //             PrizeBubbleItemData bubbleItemData = resource as PrizeBubbleItemData;
//     //             if (bubbleItemData != null)
//     //             {
//     //                 dataDict[bubbleItemData.ItemInstanceSize] = bubbleItemData;
//     //             }
//     //         }
//     //
//     //         prizeBubbleData[type] = dataDict;
//     //     }
//     // }
//
//     public void CalculateLootDrops(DeathContext context)
//     {
//         Vector2 spawnLocation;// with {Y = body.GlobalPosition.Y - height};
//         spawnLocation = context.DyingActor.AsCombatant != null
//             ? context.DyingActor.AsCombatant.BodyCollision.GlobalPosition
//             : context.DyingActor.AsNode2D.GlobalPosition;
//
//         var lootResults = context.DyingActor.AsIDropItems.LootPool.GetLoot();
//         SpawnLoot(lootResults, spawnLocation);
//
//
//     }
//
//     public void SpawnLoot(Array<LootDropItemData> lootResults, Vector2 position)
//     {
//         if (lootResults == null) return;
//
//         foreach (var result in lootResults)
//         {
//             if (result.ItemData.Prefab == null)
//             {
//                 DEBUG.Warning($"Cannot spawn item without a scene: {result.ItemData.Name}");
//                 continue; // Cannot spawn without a scene
//             }
//
//             // For now, we instantiate directly. Pooling can be added back later.
//             var quant =  result.Count.GetRandomValue(); // 10
//             while (quant > 0)
//             {
//                 ItemInstance itemInstance = Get();
//                 itemInstance.Initialize(result.ItemData, this);
//
//                 int maxStackSize = result.ItemData.MaxStackSize; // 20
//                 itemInstance.StackQuantity = maxStackSize < quant ? maxStackSize : quant;
//                 quant -= maxStackSize; // 30
//                 ApplyLaunchPhysics(itemInstance, position);
//             }
//
//         }
//     }
//
//     // private void SpawnPrizeBubbles(PrizeBubbleQuantityData prizeBubbleQuantity, Vector2 spawnLocation)
//     // {
//     //     if (prizeBubbleQuantity == null) return;
//     //
//     //     float jackpotBonus = 1+Player.StatsComponent[StatType.BonusJackpot];
//     //     int life = Mathf.CeilToInt(prizeBubbleQuantity.MinAmountOfLife.RandomRangeFromMin(prizeBubbleQuantity.MaxAmountOfLife) * jackpotBonus);
//     //     int mana = Mathf.CeilToInt(prizeBubbleQuantity.MinAmountOfMana.RandomRangeFromMin(prizeBubbleQuantity.MaxAmountOfMana) * jackpotBonus);
//     //     int super = Mathf.CeilToInt(prizeBubbleQuantity.MinAmountOfSuper.RandomRangeFromMin(prizeBubbleQuantity.MaxAmountOfSuper) * jackpotBonus);
//     //     int money = Mathf.CeilToInt(prizeBubbleQuantity.MinAmountOfMoney.RandomRangeFromMin(prizeBubbleQuantity.MaxAmountOfMoney) * jackpotBonus);
//     //
//     //     SpawnBubbles(BubbleType.Life, life, spawnLocation);
//     //     SpawnBubbles(BubbleType.Mana, mana, spawnLocation);
//     //     SpawnBubbles(BubbleType.Super, super, spawnLocation);
//     //     SpawnBubbles(BubbleType.Money, money, spawnLocation);
//     // }
//
//
//     // private void SpawnBubbles(BubbleType type, int totalAmount, Vector2 position)
//     // {
//     //     if (totalAmount <= 0) return;
//     //
//     //     int remainingAmount = totalAmount;
//     //
//     //     while (remainingAmount > 0)
//     //     {
//     //         PrizeBubbleItemData bestFit = null;
//     //         // foreach (var itemData in dataForType)
//     //         foreach (var itemData in prizeBubbleData[type].Values)
//     //         {
//     //             if (itemData.Value <= remainingAmount)
//     //             {
//     //                 bestFit = itemData;
//     //                 break; // Found the largest denomination that fits
//     //             }
//     //         }
//     //
//     //         if (bestFit != null)
//     //         {
//     //             SpawnSingleBubble( bestFit, position);
//     //             remainingAmount -= bestFit.Value;
//     //         }
//     //         else
//     //         {
//     //             // This will break the loop if the smallest denomination is still larger than the remaining amount.
//     //             break;
//     //         }
//     //     }
//     // }
//
//     private void SpawnSingleBubble(PrizeBubbleItemData data, Vector2 position)
//     {
//         var bubble = Get();
//         if (bubble == null) return;
//
//         bubble.Initialize(data, this);
//         ApplyLaunchPhysics(bubble, position);
//     }
//
//     private void ApplyLaunchPhysics(ItemInstance item, Vector2 position)
//     {
//         Vector2 newPos = position with
//         {
//             X = position.X + ((float)Constants.PX_PER_TILE).RandomRangeFromMax(centerAtZero:true),
//             Y = position.Y - (2 * item.ItemRadius) - 2f
//         };
//
//         if (BouncyMaterial != null)
//         {
//             item.PhysicsMaterialOverride = BouncyMaterial;
//         }
//
//         item.GlobalPosition = newPos; // Start at the source position
//         item.GravityScale = gravityMultiplier;
//
//         float distanceInPixels = LaunchDistance.GetRandomValue() * Constants.PX_PER_TILE;
//         float airTime = AirTime.GetRandomValue();
//         float speed = airTime > 0 ? distanceInPixels / airTime : 0;
//
//         float spread = Mathf.DegToRad(LaunchAngleSpread);
//         //FIXME: it looks like they're all at the same angle
//         float angle = Mathf.DegToRad(LaunchAngle) + (GD.Randf() * spread - spread / 2f);
//         var launchVector = Vector2.FromAngle(angle);
//
//         item.ApplyLaunchImpulse(launchVector * speed);
//
//         // A generic way to handle spin for items that might want it
//         if (item.CanTumble)
//         {
//             float spinDirection = Mathf.Sign(GD.Randf() - 0.5f);
//             item.AngularVelocity = spinDirection * SpinMagnitude.GetRandomValue();
//             item.RotationDegrees = initialAngleRange.GetRandomValue();
//         }
//     }
//
//     public void SetItemParent(Node newParent)
//     {
//         _currentItemParent = newParent;
//     }
// }