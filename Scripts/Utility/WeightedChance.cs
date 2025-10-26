using System;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

namespace Elythia;

using Godot;

public static class WeightedChance
{


	public static T Select<T>(IList<T> weightedItems, Func<T, float> getWeight)
	{
		if (weightedItems.Count == 0) return default;

		double totalWeight = weightedItems.Sum(item => getWeight(item));

		return Select(weightedItems, getWeight, totalWeight);
	}

	public static T Select<T>(IList<T> weightedItems, Func<T, float> getWeight, double totalWeight)
	{
		if (totalWeight <= 0) return default;

		double roll = GD.RandRange(0, totalWeight);
		double accumulatedWeight = 0;

		foreach (var item in weightedItems)
		{
			accumulatedWeight += getWeight(item);
			if (accumulatedWeight >= roll)
			{
				return item;
			}
		}

		return weightedItems.LastOrDefault(); // Fallback
	}
}