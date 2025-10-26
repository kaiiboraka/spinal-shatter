extends Node
class_name Trip

signal wave_completed
signal trip_completed

var waves:Array
var wave_count = 0
var wave_idx = 0

func _init(start_idx, waves_info:Array):
	wave_idx = start_idx
	waves = waves_info
	create_wave()
	
func create_wave():
	if (wave_count == waves.size()):
		trip_completed.emit()
		queue_free.call_deferred()
		return
	var wave := Generators.create_wave(wave_idx, waves[wave_count])
	wave_count += 1
	Game.wave_count += 1
	add_child(wave)
	wave.wave_ended.connect(create_wave)
