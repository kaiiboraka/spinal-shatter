@tool
extends OverlaidMenu

signal restart_pressed
signal main_menu_pressed

@onready var confirm_restart = $ConfirmRestart

func _ready():
	if OS.has_feature("web"):
		%ExitButton.hide()

func _on_exit_button_pressed():
	$ConfirmExit.popup_centered()

func _on_main_menu_button_pressed():
	$ConfirmMainMenu.popup_centered()

func _on_confirm_main_menu_confirmed():
	main_menu_pressed.emit()
	close()

func _on_confirm_exit_confirmed():
	get_tree().quit()

func _on_close_button_pressed():
	confirm_restart.popup_centered()

func _on_confirm_restart_confirmed():
	close()
	SceneLoader.reload_current_scene()
