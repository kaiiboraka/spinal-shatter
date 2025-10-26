extends Node


func to_time_stamp(elapsed:int):
	@warning_ignore("integer_division")
	return '%02d:%02d'%[elapsed/60000,(elapsed/1000)%60]

static var difficulty_name:
	get: return Data.SHIPMENT.keys()[Game.difficulty]
	

func get_tiers(cost):
	var tiers = []
	var n = 0
	while cost >= tier_cost_multi(n):
		if cost < tier_threshold_max(n): tiers.append(n)
		n += 1
	#print(cost, tiers,': ',tier_cost_multi(n-1),'-',tier_threshold_max(n-2))
	return tiers
	

#if cost < multi then stop
func tier_cost_multi(n): return pow(Settings.TIER_COST,n)


# if cost >= max ignore it.
func tier_threshold_max(n): return tier_cost_multi(n) * (Settings.TIER_COST + 4)
