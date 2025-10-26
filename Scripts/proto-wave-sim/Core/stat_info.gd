extends Resource
class_name StatBlock

enum ObjType {Player, Engine, Cargo}
enum Locmotion {Flying, Walking}
enum GunRange {Long, Medium, Melee, Variable}

@export var hp:float=100
## scaling factor. don't reduce below 1 unless you wanna nerf copper.
@export var hp_growth:float=1
func calc_hp(mul): return hp * mul * hp_growth
@export var dmg:float=2
@export var dmg_growth:float=1
func calc_dmg(mul): return dmg * mul * dmg_growth
@export var def:float=2
@export var def_growth:float=1
func calc_def(mul): return def * mul * def_growth
@export var spd:float=100
@export var spd_growth:float=1
func calc_spd(mul): return spd * mul * spd_growth

@export var atk_spd:float=1.0

@export var objective:=ObjType.Engine
@export var move_type:=Locmotion.Walking
@export var gun_range:=GunRange.Medium
