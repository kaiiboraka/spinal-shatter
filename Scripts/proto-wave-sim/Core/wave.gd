extends Node
class_name Wave

signal spawned_subwave
signal wave_ended

var wave_cost:int
var timer:Timer
var squads:Array[Squad]=[]
var squad_costs: Array
var length:int

func _init(cost:int, size:int):
	wave_cost = cost
	@warning_ignore("integer_division")
	squad_costs = range(size).map(func(x): return (cost/size + (1 if (cost%size > x) else 0)))
	length = Settings.WAVE_INFO.START_DELAY + (Settings.WAVE_INFO.INTERVAL * size) + Settings.WAVE_INFO.TIME_WITH_LAST_WAVE
	
func _ready() -> void:
	timer = MaxTimer.new(squad_costs.size())
	timer.timeout.connect(spawn_subwave)
	timer.wait_time = Settings.WAVE_INFO.INTERVAL
	add_child(timer)
	_start_timer.call_deferred()
	_end_wave.call_deferred()


#func _process(_delta: float) -> void:
	#print(timer.is_stopped(), timer.time_left)

func _end_wave():
	await get_tree().create_timer(length).timeout
	wave_ended.emit()
	queue_free.call_deferred()

func _start_timer():
	await get_tree().create_timer(Settings.WAVE_INFO.START_DELAY).timeout
	timer.start()
	timer.timeout.emit()
	
func spawn_subwave():
	var s := Generators.create_squad(squad_costs.pop_front())
	get_parent().add_child(s)
	spawned_subwave.emit()
