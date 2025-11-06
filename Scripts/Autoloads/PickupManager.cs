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

    private ObjectPoolManager<Pickup> _pool;

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

        _pool = new ObjectPoolManager<Pickup>();
        _pool.PoolParent = this; // Assign this manager as the parent for pooled objects
        _pool.Name = "PickupPool";
        AddChild(_pool);
    }

    public void Release(Pickup pickup)
    {
        if (pickup == null) return;

        _pool.Release(pickup);
    }

    public Array<Pickup> SpawnPickupAmount(PickupType type, int totalAmount, Vector3 position)
    {
        double newTotal = totalAmount * (1 + _moneyAmountRandomization); // 1.15 x 100 = 115
        double range = (_moneyAmountRandomization * 2.0f);

        newTotal -= (totalAmount * GD.RandRange(0, range)); // 100 * .2 = 20
        Array<Pickup> spawnedPickups = new Array<Pickup>();
        int roundUpTotal = newTotal.CeilingToInt();
        switch (type)
        {
            case PickupType.Mana:
                spawnedPickups = SpawnGreedy(ManaPickupData, roundUpTotal, position);
                break;
            case PickupType.Money:
                foreach (MoneyData moneyData in GenerateVariedMoneyDrop(roundUpTotal))
                {
                    spawnedPickups.Add(SpawnPickup(moneyData, position));
                }
                break;
            default:
                return new Array<Pickup>();
        }
        return spawnedPickups;
    }

    private Array<Pickup> SpawnGreedy<[MustBeVariant]T>(Array<T> sortedPickupData, int totalAmount, Vector3 position) where T: PickupData
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
        _pool.Scene = _pickupScenes[data.PickupType];
        Pickup pickup = _pool.Get();
        if (pickup == null) return null; // Pool is full

        // Add a random offset to the spawn position
        Vector3 offset = new Vector3().RandomRange(.5f) + Vector3.Up;
        pickup.GlobalPosition = position + offset;
        pickup.Initialize(data);
        return pickup;
    }

    private Array<MoneyData> GenerateVariedMoneyDrop(int totalAmount)
    {
        Array<MoneyData> generatedCoins = new Array<MoneyData>();
        int currentAmount = totalAmount;

        // Use the pre-sorted MoneyPickupData array directly
        Dictionary<int, MoneyData> moneyDataMap = new Dictionary<int, MoneyData>();
        foreach (MoneyData md in MoneyPickupData)
        {
            moneyDataMap[md.Value] = md;
        }

        foreach (MoneyData currentDenominationData in MoneyPickupData)
        {
            int currentDenominationValue = currentDenominationData.Value;

            // Smallest denomination (1-coin) is always added greedily for any remaining amount
            if (currentDenominationValue == 1)
            {
                while (currentAmount >= 1)
                {
                    if (moneyDataMap.TryGetValue(1, out var moneyData))
                    {
                        generatedCoins.Add(moneyData);
                    }
                    currentAmount -= 1;
                }
                break; // All remaining amount is now 1-coins, so we are done.
            }

            // For larger denominations, apply the randomized breakdown logic
            while (currentAmount >= currentDenominationValue)
            {
                if (GD.Randf() < _moneyBreakdownChance)
                {
                    // Decide to 'break down' this coin.
                    // Its value remains in currentAmount to be processed by smaller denominations.
                    currentAmount -= currentDenominationValue; // Remove the value of the coin that *would have been* dropped
                }
                else
                {
                    // Decide to drop this coin as is
                    generatedCoins.Add(currentDenominationData);
                    currentAmount -= currentDenominationValue;
                }
            }
        }
        return generatedCoins;
    }
}
