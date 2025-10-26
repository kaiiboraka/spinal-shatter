extends Resource
class_name Stat

signal max_changed
signal depleted
var _owner:Node
var _name:StringName
@export var initial:=0.0
@export var min_v:=0.0
@export var max_v:=NAN:
	set(v):
		var old = max_v
		max_v = v
		max_changed.emit(old, v)
		if value > max_v: value = max_v
var value:=0.0:
	set(v):
		value = clamp(v, min_v, max_v)
		emit_changed()
		if value == min_v: depleted.emit()
		
func _init():
	resource_local_to_scene = true

func link_bar(pgrs:ProgressBar):
	pgrs.max_value = max_v
	pgrs.min_value = min_v
	pgrs.value = value
	changed.connect(func():pgrs.value = value)

func _setup_local_to_scene():
	if is_nan(max_v): max_v = initial
	_owner = self.get_local_scene()
	value = initial
	for prop in _owner.get_property_list():
		if prop['type']==typeof(self) and _owner.get(prop['name']) == self:
			_name = prop.name
			break
