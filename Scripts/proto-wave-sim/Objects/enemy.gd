extends CharacterBody2D
class_name Enemy

signal died
signal atk(dmg:float)


@export var stats:StatBlock
@export var hp:Stat

@onready var nav := $NavigationAgent2D

var multiplier:=1
var start_time = Time.get_ticks_msec()

func _ready():
	hp.max_v = stats.calc_hp(multiplier)
	hp.value = hp.max_v
	hp.depleted.connect(func(): died.emit())
	hp.link_bar($HPBar)
	died.connect(func():queue_free())
	
	nav.target_position = Game.instance.goal.global_position
	#nav.debug_enabled = true
	
func _process(_delta: float) -> void:
	var direction:Vector2 = nav.get_next_path_position()-global_position
	velocity = direction.normalized() * stats.calc_spd(multiplier)
	move_and_slide()
	if (Time.get_ticks_msec() - start_time) >= 1000 * stats.atk_spd: 
		hp.value -= 1
		attack()

func dmg(x): 
	hp -= min(0, x - stats.calc_def(multiplier))

func attack():
	start_time += 1000 * stats.atk_spd
	atk.emit(stats.calc_dmg(multiplier))
