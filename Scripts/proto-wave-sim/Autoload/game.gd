extends Node2D
class_name Game

static var instance:Game
static var difficulty := Settings.INITIAL_DIFFICULTY
static var city_count:= 0
static var wave_count:= 0
static var wave_idx:int:
	get: return (difficulty * Settings.DIFFICULTY_WAVE_VAL) + (city_count*Settings.CITY_WAVE_VAL) + wave_count

var enemies: Array[Enemy] = []
@export var player : Player
@export var ship: Ship
@export var goal: Node2D
@export var progressBar:HSlider
@export var lvl_info:RichTextLabel
@export var wave_info:RichTextLabel
@export var sqd_info:RichTextLabel

func _init():
	Game.instance = self

func _ready():
	Engine.time_scale = 10
	player.died.connect(restart)
	var t:=Generators.create_trip()
	t.trip_completed.connect(trip_over)
	add_child(t)
	#%LevelInfo.text = '%s   Cities: %d W%d [%s] %d%%'%[GameUtils.difficulty_name,
		#city_count, wave_count,GameUtils.time_stamp,%Progress.value]
		
	#%WaveInfo.text = 'Wave %d: $%d'%[wave.num,wave.cost]
	#%SquadsInfo.get_children().any(func(c):c.queue_free())
	#for n in wave.count:
		#var lbl = RichTextLabel.new()
		#lbl.text = 'Squad %d - %d:\n- %s' %[n,len(wave.squads[n]),'\n- '.join(wave.squads[n].map(func(x):return x.info))]
		#lbl.size_flags_horizontal |= lbl.SIZE_EXPAND
		#lbl.bbcode_enabled = true
		#%SquadsInfo.add_child(lbl)
	#GameUtils.avg_test(func():return len(WaveCalc.create()[0]))

func trip_over():
	print("Next trip :D")
	Game.wave_count = 0
	Game.city_count += 1
	var t:=Generators.create_trip()
	t.trip_completed.connect(trip_over)
	add_child(t)

func restart():
	print('Game Over')
	get_tree().paused = true
	await get_tree().create_timer(2).timeout
	get_tree().paused = false
	get_tree().reload_current_scene()
