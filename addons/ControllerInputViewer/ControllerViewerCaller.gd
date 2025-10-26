@tool
class_name ControllerViewerCaller
extends Node2D

#InputMaps
@export var HideHUD: bool = false :
	get : 
		return HideHUD #ControllerHud.visible if ControllerHud else false;
	set(value):
		HideHUD = value;
		if ControllerHud: ControllerHud.visible = HideHUD;
		#else: HideHUD = value;
@export var HideHudCommand: String
@export var HideControllerListPrint: bool = true
var InputMaps = InputMap.get_actions()
@onready var Searchinput = InputMaps.find(HideHudCommand,0)

#ScaleInputs
@export var UpScallerHUD: String
@export var DownScallerHUD: String
@export var HUDScale: float = 1
@onready var SearchInputUp = InputMaps.find(UpScallerHUD, 0)
@onready var SearchInputDown = InputMaps.find(DownScallerHUD, 0)

@onready var ViewerScene : PackedScene = preload("res://addons/ControllerInputViewer/ControllerHud/ControllerInputViewer.tscn")

@onready var ControllerHud : ControllerInputViewer;

func _ready():
	if !Engine.is_editor_hint():
		var SceneInt = ViewerScene.instantiate()
			
		add_child(SceneInt)
		SceneInt.set_owner(get_tree().get_edited_scene_root())
		
	if HideControllerListPrint == true:
		print("Joypads Connected:",Input.get_connected_joypads().size())

func _process(delta):
	if !Engine.is_editor_hint():
#Hide and Show preset
		var HUDHideVar = ControllerHud.ConHUDView
		
		HUDHideVar = HideHUD
		
#Hide and Show Controller HUD Input
		if Searchinput >= 0:
			if Input.is_action_just_pressed(HideHudCommand):
				HideHUD =! HideHUD
			
			ControllerHud.visible = HideHUD
			
#HUD Scaller
		var HUDScaller = ControllerHud.scale
		var ChildExist = false
		
		ControllerHud.scale = lerp(ControllerHud.scale, Vector2(HUDScale,HUDScale), 25 * delta)
		
		if SearchInputUp >= 0:
			if Input.is_action_just_pressed(UpScallerHUD):
				if HUDScale <= 4:
					HUDScale = HUDScale + 0.3
				
		if SearchInputDown >= 0:
			if Input.is_action_just_pressed(DownScallerHUD):
				if HUDScale >= 0.3:
					HUDScale = HUDScale - 0.3
					
		var newPos : float = -128 * HUDScale;
		position = lerp(position, Vector2(newPos,newPos), 25 * delta);
