extends Timer
class_name MaxTimer

signal halted

var _count:=0
var max_count:=9_223_372_036_854_775_806:
	set(v):
		max_count = v
		_is_maxed()


func _init(max_c:=self.max_count) -> void:
	max_count = max_c
	one_shot = false
	timeout.connect(_is_maxed)


func _is_maxed():
	_count += 1
	if _count > max_count:
		_count = 0
		halted.emit()
		stop.call_deferred()
