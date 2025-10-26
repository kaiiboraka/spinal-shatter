extends Node

func create_trip() -> Trip:
	print('Trip ', Game.city_count,' - ', Data.wave_sizes[Game.difficulty])
	var trip:= Trip.new(Game.wave_idx, Data.wave_sizes[Game.difficulty])
	return trip

func create_wave(wave_idx, size) -> Wave:
	print('\tWave ', Game.wave_count+1,' - ',Data.get_wave_cost(wave_idx))
	var wave:= Wave.new(Data.get_wave_cost(wave_idx), size)
	return wave

func create_squad(cost:int)->Squad:
	var squad:= Squad.new(cost)
	print('\t\tSquad (',cost,'): ', ', '.join(squad.squad_units.map(func(i):return i.node_name)))
	return squad

func create_squad_units(cost)->Array[EnemyInfo]:
	var choices:Array[EnemyInfo] = []
	var teirs = GameUtils.get_tiers(cost)
	while cost > 0:
		var enemy_type:String = Utils.rand_list(Data.enemy_base_cost.keys())
		var tier:int = Utils.rand_list(teirs)
		var e_cost = GameUtils.tier_cost_multi(tier) * Data.enemy_base_cost[enemy_type]
		if(e_cost > cost): continue
		var enemy:= EnemyInfo.new(enemy_type, tier)
		cost -= e_cost
		teirs = GameUtils.get_tiers(cost)
		choices.append(enemy)
	choices.sort_custom(func(a,b):return a.rank>b.rank)
	return choices

func create_enemy(info:EnemyInfo)->Enemy:
	info.type += '1'
	if !Data.node_name.has(info.node_name):
		return null
	var n:Enemy = Data.node_name[info.node_name].instantiate()
	n.multiplier = info.multi
	return n
