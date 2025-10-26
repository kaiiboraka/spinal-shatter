extends Object
class_name EnemyInfo

var type:String
var rank:int
var multi:int
var cost:int
var node_name:String
var color_map ={'brass':'chocolate','gold':'gold','steel':'silver'}
func _init(e_type:String,e_rank:int) -> void:
	type = e_type
	rank = e_rank
	cost = GameUtils.tier_cost_multi(rank) * Data.enemy_base_cost[type]
	multi = GameUtils.tier_cost_multi(rank)
	#prestige=floor(t/len(Data.enemy_tier))
	var grade = Data.enemy_tier[rank%len(Data.enemy_tier)]
	node_name = ('%s_%s'%[grade,type]).capitalize()
	#enemy.name='%s %s'%[enemy.node_name,GameUtils.to_roman(enemy.prestige+1)]
	#enemy.info = '[color=%s]%s[/color]'%[color_map[enemy.grade],enemy.name]
