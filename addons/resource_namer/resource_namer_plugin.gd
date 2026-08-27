@tool
extends EditorInspectorPlugin

func _can_handle(object):
	# We handle any Resource that has a path and a 'resource_name' property.
	return object is Resource and object.resource_path != "" and "resource_name" in object

func _parse_begin(object):
	# This is called at the beginning of the inspector for an object.
	var button = Button.new()
	button.text = "Set Resource Name to Filename"
	# Connect the pressed signal. We need to pass the resource object to the callback.
	button.icon = EditorInterface.get_base_control().get_theme_icon("StringName", "EditorIcons");
	button.pressed.connect(_on_set_name_button_pressed.bind(object))
	add_custom_control(button)

func _on_set_name_button_pressed(resource):
	if not resource is Resource or resource.resource_path.is_empty():
		return

	var file_name = resource.resource_path.get_file()
	var base_name = file_name.get_basename()

	if resource.get("resource_name") != base_name:
		resource.set("resource_name", base_name)
		print("Resource name for '"+ file_name + "' set to '" + base_name + "'")
		EditorInterface.get_inspector().queue_redraw();
