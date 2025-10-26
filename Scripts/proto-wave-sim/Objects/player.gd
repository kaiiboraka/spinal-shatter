extends CharacterBody2D
class_name Player
const SPEED = 500.0

signal atk
signal died
@export var hp:Stat

func _ready():
	hp.depleted.connect(func(): died.emit())
	hp.link_bar(%HPBar)
	

var start_time = Time.get_ticks_msec()
var elapsed_msecs:
	get: return (Time.get_ticks_msec() - start_time)

var shoot_speed = 1_000

func dmg(x): hp -= x

func attack():
	start_time += shoot_speed
	atk.emit()


func _physics_process(_delta: float) -> void:
	var direction := Input.get_vector("move_left", "move_right", 'move_up', "move_down")
	velocity = direction * SPEED
	move_and_slide()
	if elapsed_msecs >= shoot_speed: attack()

const BULLET = preload("res://Scenes/bullet.tscn")
func shoot():
	var b := BULLET.instantiate()
	b.global_position = global_position
	b.vel = (get_global_mouse_position() - global_position).normalized()
	b.rotation = b.vel.angle()
	add_sibling(b)
