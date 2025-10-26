extends Node2D
class_name Ship

var edges = []
var lengths = []
var total_length = 0.0

func _ready():
	var collision_polygon := $CollisionShape2D/Ship
	var points = collision_polygon.polygon
	edges.clear()
	lengths.clear()
	total_length = 0.0
	var gs = collision_polygon.global_scale
	var gp = collision_polygon.global_position

	for i in range(points.size()):
		var start_point = (points[i]*gs) + gp
		var end_point = (points[(i + 1) % points.size()] * gs)+ gp
		var edge = [start_point, end_point]
		edges.append(edge)
		var length = start_point.distance_to(end_point)
		lengths.append(length)
		total_length += length
	
	print(total_length)

func get_random_point() -> Vector2:
	var random_length = randf() * total_length
	var accumulated_length = 0.0

	for i in range(edges.size()):
		accumulated_length += lengths[i]
		if random_length <= accumulated_length:
			var edge = edges[i]
			var edge_length = lengths[i]
			var t = (random_length - (accumulated_length - edge_length)) / edge_length
			return edge[0].lerp(edge[1], t).move_toward($NavigationRegion2D.global_position, 50)
			
	return Vector2.ZERO  # Fallback, should not reach here

   
