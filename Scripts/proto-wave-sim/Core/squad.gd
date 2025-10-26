extends Polygon2D
class_name Squad

const poly := [Vector2(10,10),Vector2(10,-10),Vector2(-10,-10),Vector2(-10,10)] 

var squad_cost:int
var timer:MaxTimer

var squad_units:Array

func _init(cost:int):
	squad_cost = cost
	squad_units = Generators.create_squad_units(cost)
	

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	timer = MaxTimer.new(squad_units.size())
	timer.timeout.connect(_spawn_unit)
	add_child(timer)
	timer.start(Settings.SQUAD_SPAWN_INTERVAL)
	set_polygon(PackedVector2Array(poly))
	global_position = Game.instance.ship.get_random_point()


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _spawn_unit():
	var unit:= Generators.create_enemy(squad_units.pop_front())
	add_child(unit)
	
