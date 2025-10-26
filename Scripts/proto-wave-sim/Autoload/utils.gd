extends Node


func rand_list(l:Array):
	if (len(l) == 0): return null
	return l[randi() % len(l)]
	

func test_avg(f:Callable, iter:int=100):
	return range(iter).map(func (_x): return f.call()).reduce(func(a,v):return a+v, 0.0)/float(iter)
	

func to_roman(n):
	var digits = [[1000, 'M'], [900, 'CM'], [500, 'D'], [400, 'CD'],
			[100, 'C'], [90, 'XC'], [50, 'L'], [40, 'XL'],
			[10, 'X'], [9, 'IX'], [5, 'V'], [4, 'IV'], [1, 'I']]
	var result = ""
	while len(digits) > 0:
		var val = digits[0][0]
		var romn = digits[0][1]
		if n < val:
			digits.remove_at(0) # Removes first element
		else:
			n -= val
			result += romn
	return result
