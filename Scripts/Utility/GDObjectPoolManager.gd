class_name GDObjectPoolManager extends Node

@export var scene: PackedScene
@export var object_pool_max_size := 10000

var pool_parent: Node = null  # A dedicated parent for pooled objects
var ready_pool: Array[CanvasItem] = []
var active_objects := 0

func _enter_tree() -> void:
	pool_parent = Node.new()
	pool_parent.name = "ObjectPool"
	
	var game_world := get_tree().root
	await game_world.ready
	
	# add a parent node so we don't have thousands of floating nodes in the game world
	game_world.add_child(pool_parent)

func ClearPool() -> void:
	for obj in ready_pool:
		if is_instance_valid(obj):
			obj.queue_free()
	ready_pool.clear()
	active_objects = 0

func RemoveToPool(object: CanvasItem) -> void:	
	# edge case if called before pull and null check
	if active_objects <= 0 || !is_instance_valid(object):
		return
	
	active_objects -= 1
	
	# Fast deactivation
	object.set_process(false)
	object.set_physics_process(false)
	object.hide()
	
	# Only add back if under pool size
	# prevent adding the object back to the pool if max size is reached
	# this can only occur if max pool size changes during gameplay
	# this will cause the pool to slowly drain until max size is reached
	if ready_pool.size() < object_pool_max_size:
		# Fast append without bounds check
		ready_pool.resize(ready_pool.size() + 1)
		ready_pool[ready_pool.size() - 1] = object
	else:
		# Free if pool is full
		object.queue_free()

func GetFromPool() -> CanvasItem:
	# Don't return anything if active items are full
	if active_objects >= object_pool_max_size:
		return null
	
	var obj: CanvasItem
	var last_index := ready_pool.size() - 1
	
	if last_index >= 0:
		# Fast pop without array resize
		obj = ready_pool[last_index]
		ready_pool.resize(last_index)
	else:
		# Create new instance if pool is empty
		obj = scene.instantiate()
		pool_parent.add_child(obj)
	
	# Reactivate object
	obj.set_process(true)
	obj.set_physics_process(true)
	obj.show()
	
	active_objects += 1
	
	return obj
