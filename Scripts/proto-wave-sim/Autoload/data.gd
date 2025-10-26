extends Node

enum SHIPMENT { LETTER=-1, COAL=0, COPPER=1, PARTS=2, GEMS=3}
const wave_sizes := {
	SHIPMENT.LETTER: [1, 1, 3],
	SHIPMENT.COAL:   [1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 5, 5], # => 6(6)-2(9)
	SHIPMENT.COPPER: [1, 1, 2, 2, 2, 3, 3, 4, 4, 4, 5, 5, 6, 6, 7, 8], # => 8(8)-2(15)
	SHIPMENT.PARTS:  [2, 3, 3, 3, 3, 4, 4, 5, 6, 6, 7, 7, 8, 9, 10,10],# => 6(13)-3(33)
	SHIPMENT.GEMS:   [4, 4, 4, 5, 5, 5, 6, 6, 7, 7, 8, 9, 10,11,12,12],# => 6(27)-7(84)
}

const enemy_tier := ['copper','steel','gold']

func get_wave_cost(wave_idx):
	return 5 + round(1.1**(.9*wave_idx))

const enemy_base_cost := {
	scrounger=1,
	scrapper=1,
	lunker=2,
	buster=4,
	swooper=1,
	zapper=1,
	scorcher=1,
}

const node_name := {
	'Copper Buster':	preload("res://Scenes/EnemyTypes/DEMO.tscn"),
	'Copper Lunker':	preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Copper Scorcher':	preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Copper Scrapper':	preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Copper Swooper':	preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Copper Zapper':	preload("res://Scenes/EnemyTypes/DEMO.tscn"),
	'Copper Scrounger':	preload('res://Scenes/EnemyTypes/DEMO.tscn'),
	'Steel Buster':		preload("res://Scenes/EnemyTypes/DEMO.tscn"),
	'Steel Lunker':		preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Steel Scorcher':	preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Steel Scrapper':	preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Steel Swooper':	preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Steel Zapper':		preload("res://Scenes/EnemyTypes/DEMO.tscn"),
	'Steel Scrounger':	preload('res://Scenes/EnemyTypes/DEMO.tscn'),
	'Gold Buster':		preload("res://Scenes/EnemyTypes/DEMO.tscn"),
	'Gold Lunker':		preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Gold Scorcher':	preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Gold Scrapper':	preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Gold Swooper':		preload("res://Scenes/EnemyTypes/DEMO.tscn"), 
	'Gold Zapper':		preload("res://Scenes/EnemyTypes/DEMO.tscn"),
	'Gold Scrounger':	preload('res://Scenes/EnemyTypes/DEMO.tscn'),
}
