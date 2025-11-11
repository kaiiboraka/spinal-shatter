using System.Linq;
using Elythia;
using Godot;
using Godot.Collections;

[Tool]
public partial class PickupManager : Node
{
	public static PickupManager Instance { get; private set; }

	[Export] public Array<ManaParticleData> ManaPickupData { get; private set; } = new();
	[Export] public Array<MoneyData> MoneyPickupData { get; private set; } = new();
	[Export] private float _moneyBreakdownChance = 0.3f; // 30% chance to break down a coin
	[Export] private float _moneyAmountRandomization = 0.15f; // +- 15% of total
	[Export] private Dictionary<PickupType, PackedScene> _pickupScenes = new(); // The scene for all pickups

	private readonly Dictionary<PickupType, ObjectPoolManager<Pickup>> _pools = new();

	[ExportToolButton("Sort Lists", Icon = "Array")]
	public Callable SortListsButton => Callable.From(SortLists);

	public void SortLists()
	{
		ManaPickupData = new Array<ManaParticleData>(ManaPickupData.OrderByDescending(d => d.Value).ToList());
		MoneyPickupData = new Array<MoneyData>(MoneyPickupData.OrderByDescending(d => d.Value).ToList());
	}

	public override void _Ready()
	{
		if (Instance != null)
		{
			QueueFree();
			return;
		}

		Instance = this;

		foreach (var type in _pickupScenes.Keys)
		{
			var pool = new ObjectPoolManager<Pickup>
			{
				PoolParent = this,
				Name = $"{type}Pool",
				Scene = _pickupScenes[type]
			};
			AddChild(pool);
			_pools[type] = pool;
		}
	}

	public void Release(Pickup pickup)
	{
		if (pickup == null) return;

		if (_pools.TryGetValue(pickup.Type, out var pool))
		{
			pool.Release(pickup);
		}
		else
		{
			// Failsafe for pickups not managed by a pool
			pickup.QueueFree();
		}
	}

	public Array<Pickup> SpawnPickupAmount(PickupType type, int totalAmount, Vector3 position)
	{
		Array<Pickup> spawnedPickups = new Array<Pickup>();
		switch (type)
		{
			case PickupType.Mana:
				spawnedPickups = SpawnGreedy(ManaPickupData, totalAmount, position);
				break;
			case PickupType.Money:
				double newTotal = totalAmount * (1 + _moneyAmountRandomization); // 1.15 x 100 = 115
				double range = (_moneyAmountRandomization * 2.0f);
				newTotal -= (totalAmount * GD.RandRange(0, range)); // 100 * .2 = 20
				int roundUpTotal = newTotal.CeilingToInt();
				Array<MoneyData> generateVariedMoneyDrop = GenerateVariedMoneyDrop(roundUpTotal);
				DebugManager.Debug(
					$"{type}: original {totalAmount}, new {newTotal}, rounded {roundUpTotal} -- {generateVariedMoneyDrop.Count} coins");
				foreach (MoneyData moneyData in generateVariedMoneyDrop)
				{
					spawnedPickups.Add(SpawnPickup(moneyData, position));
				}

				break;
			default:
				return new Array<Pickup>();
		}

		return spawnedPickups;
	}

	private Array<Pickup> SpawnGreedy<[MustBeVariant] T>(Array<T> sortedPickupData, int totalAmount, Vector3 position)
		where T : PickupData
	{
		Array<Pickup> spawnedPickups = new Array<Pickup>();
		int remaining = totalAmount;

		foreach (T pickupData in sortedPickupData)
		{
			if (pickupData.Value <= 0) continue;
			int numToSpawn = remaining / pickupData.Value;
			for (int i = 0; i < numToSpawn; i++)
			{
				spawnedPickups.Add(SpawnPickup(pickupData, position));
			}

			remaining %= pickupData.Value;
		}

		// Spawn remaining amount as smallest denomination
		if (remaining > 0 && sortedPickupData.Count > 0)
		{
			PickupData smallestDenomination = sortedPickupData.Last();
			for (int i = 0; i < remaining; i++)
			{
				spawnedPickups.Add(SpawnPickup(smallestDenomination, position));
			}
		}

		return spawnedPickups;
	}

	private Pickup SpawnPickup(PickupData data, Vector3 position)
	{
		PickupType type;
		if (data is ManaParticleData)
		{
			type = PickupType.Mana;
		}
		else if (data is MoneyData)
		{
			type = PickupType.Money;
		}
		else
		{
			GD.PrintErr($"Unknown PickupData type: {data.GetType().Name}");
			return null;
		}

		if (!_pools.TryGetValue(type, out var pool))
		{
			return null;
		}

		Pickup pickup = pool.Get();
		if (pickup == null) return null; // Pool is full

		// Add a random offset to the spawn position
		Vector3 offset = new Vector3().RandomRange(.5f) + Vector3.Up;
		pickup.GlobalPosition = position + offset;
		pickup.Initialize(data);
		pickup.Sprite.RandomizeAnimation();
		return pickup;
	}

	private Array<MoneyData> GenerateVariedMoneyDrop(int totalAmount)
	{
		int currentAmount = totalAmount;

		// Use the pre-sorted MoneyPickupData array directly
		// Dictionary<int, MoneyData> moneyDataMap = new Dictionary<int, MoneyData>();
		// foreach (MoneyData md in MoneyPickupData)
		// {
		//     moneyDataMap[md.Value] = md;
		// }

		Array<MoneyData> generatedCoins = new Array<MoneyData>();
		foreach (MoneyData moneyData in MoneyPickupData)
		{
			int currentDenominationValue = moneyData.Value;

			if (GD.Randf() < _moneyBreakdownChance && currentDenominationValue > 1)
			{
				// Decide to 'break down' this coin.
				// Its value remains in currentAmount to be processed by smaller denominations.
			}
			else
			{
				int numCoins = currentAmount / currentDenominationValue;
				for (int i = 0; i < numCoins; i++)
				{
					generatedCoins.Add(moneyData);
				}

				currentAmount %= currentDenominationValue;
			}
		}

		return generatedCoins;
	}
}