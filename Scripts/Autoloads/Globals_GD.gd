@tool
extends Node

var showDebug : bool = true

signal show_debug_toggled(show : bool)

func _ready() -> void:
	showDebug = get_tree().debug_collisions_hint
	emit_show_debug_toggled()

func toggle_show_debug() -> void:
	showDebug = not showDebug
	emit_show_debug_toggled()
	
func emit_show_debug_toggled() -> void:
	show_debug_toggled.emit(showDebug);
