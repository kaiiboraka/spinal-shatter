Perfect analysis. To explain a little bit further, this old project code is a quick and dirty implementation of the Knapsack greedy algorithm for selecting the largest, most difficult (and therefore most expensive) enemies first, given an allotted budget based on progression, as you determined. So now consider @Scenes/Autoloads/WaveDirector.tscn @Scripts/Autoloads/WaveDirector.cs . I would like a robust and simple way to implement a similar structure of the old code, but for this game and its enemies, written in C#. This may involve a refactor of enemy that involves creating a new EnemyData resource if need be.


# Spinal Shatter

You are a lone sorceror, trapped in a decaying dungeon by a wicked necromancer. Mow down waves of undead hordes, or simply fight to survive, and grow in your magic power as you destroy

## this game is most like...

Vampire Survivors + Hades + Halo Firefight

## objective



Don't Die for as long as possible, trying to last until the end

Lasting for number of waves instead of amount of time, but could have timed waves too
set of waves in a room could end with a "boss"

## Mechanics

### Controls

KBM / Controller = action
---
WASD / leftStick = Move
Mouse / rightStick = Aim
LMB / rightTrigger = (Press) Attack / (Hold) Charge Magic
RMB / leftTrigger = Secondary Weapon ability (a.k.a. "Alt fire)
MB4 (maybe E? maybe Ctrl?) / rightBumper = Siphon 
MB5 / leftBumper = EITHER : (Parry / Guard?) or (use consumable, like trap?)
Space / southButton = Jump (vertical)
Shift / eastButton = Dodge (Horizontal)
??? / northButton = ???
F / westButton = Melee attack

### Mana System

The player is a spell caster that uses mechanics from other FPS games. we want physics based projectiles that you can charge to make bigger/stronger, with enemies dropping the mana you need to cast.
We draw from DOOM 2016, and be able to beat enemies in melee if you're out of ammo. 

IDEA: a special melee attack and/or takedown type move that will cause the enemy to eject a bunch of extra mana, refilling your "ammo" and sending you on your way to doom and glory. 

IDEA: melee always ejects mana from an enemy, so if you're out of ammo, you can beat it out of them no matter what. Then you still have to siphon to draw it in.

But how do you pick up all of these floating mana particles everywhere? That's where Siphon comes in.

#### Siphon

While the siphon button is held, firing new attack is disabled. Instead, you get a large vacuuming cone in front of you that draws in collectibles and pickups of any kind and slurps them in towards the player. I am considering it functioning akin to another of your weapons, but that seems like an unideal solution.

TBD: If Siphon, like any other weapon, has a main "charge" functionality, and a secondary Alt Fire. If it does, its alt fire would be in the form of:
Expel / Guard / Parry / Reflect: Instead of inhaling, release a burst of energy outward that will convert any successfully reflected enemy projectiles into your own projectiles and get sent back in the direction you're aiming.

## game loop

enter a room, kill some dudes, get paid, get out.

finishing a room, come out, your hallway candle dark, 2 other random ones lit
each has a reward associated with it, like Hades
ensures variety of rooms played, not just picking your favorite every SINGLE time, because it could be the one missing

I would like this game to have a structure that allows for the following gameplay loop, allow me to explain with a hypothetical scenario:

The game begins. I am in the central room. Two of the 4 hallways have their torches lit, with an icon showing what the reward would be for me were I to go down that hallway into that room and complete the challenge beyond. I choose the path in front of me simply by going down into the chamber at the end of the hallway. Once I enter the room, the Door locks behind me, and the Round starts. The Round is comprised of multiple Waves of randomly selected enemies. Internally, the game is keeping track of the number of waves and rounds I have completed. Each successful wave increases the game's "budget" for spawning by a small amount, and each Round completed increases it by a significantly larger amount.  It then spends this budget on a randomized selection of enemies utilizing greedy Knapsack algorithm for selecting the largest, most difficult (and therefore most expensive) enemies first, and filling in the rest with smaller and smaller foes until it's filled.

I beat some number of waves, I make it back through the open door, it closes behind me and the round ends, resulting in a payout screen that gives me a bunch of money. I am also granted the reward promised in the hallway before I chose it.

Then I may visit the Shop in the center, then pick another room and begin again.

### Challenges

There are a number of ways to increase the risk/reward factor quite literally. Make the game harder, and increase reward multipliers. 

#### Base game difficulty	

This is a global modifer to enemy damage dealt, health, potentially an AI aggressiveness factor, and an increase of a global reward multiplier.

	- D0_Braindead,
	- D1_Easy,
	- D2_Normal,
	- D3_Hard,
	- D4_Expert,
	- D5_Brutal

THOUGHT: Tiers within the secondary challenges... such as "no damage" --> "1 hp", or timer having less and less time and reaper getting faster, 10m -> 5m -> 2m.

#### Timed mode

A cursed Hourglass sits on a shelf in the central hub room. Activating this will enable a timer for the main combat arenas. Completing a round within the time limit will award increasing bonuses based on how much time remained, with less bonus the closer to 0, and more bonus the less time it took. If the timer runs out before the player clears all waves, a larger reaper-like enemy will appear and slowly move towards you, permanently accelerating its movespeed at a very slow rate (with time), but susceptible to all the player's crowd-control effects (knockback, stun, slow, root in place, etc.). If the player manages to clear the rest of the waves and finish the round, without getting one-shot killed by the Reaper, then they may try to escape out the room's door, locking the reaper in behind them. They get no time bonus if this happens, but their run may continue. The acceleration bonus of the reaper stops accruing once the round is over, but it persists to what it ended at the next time it spawns at a timeout (i.e. speed starts at 1. time runs out, speed accelerates up to 5 before the player escapes, then freezes. the next time the reaper spawns, it starts at 5 where it left off).

Timed mode can be enabled from the central room any time it is disabled. It can only be disabled with a small (increasing) gold fee.


#### Masochist mode

1-HP challenge. If you get hurt, you die. Instantly, period. Must be enabled before the first round starts, and cannot be disabled until the whole run is over from success or death failure. Highest reward multiplier.

#### Streaks and bonuses

If you have a challenge enabled, there is another multiplier that starts to accrue on the SECOND time getting its bonus, that increases (likely to a cap) for continuing to get it round over round. The moment you fail the challenge, the bonus is lost/resets. For instance, clearing 3 timer challenges in a row might build up to a 1.3x reward multiplier, but if the timer runs out and the reaper spawns, then the bonus is reset to 1x.

In addition to timer, there is also damage-taken as a "penalty" type bonus. In other words, you start with a max amount of bonus that gets subtracted for each point of damage you take, making it zero out once you have taken 100% of your max life in damage (healing mechanics would let you persist, despite bonus lost). But if you did the challenge perfectly, with zero infractions, then you get a fixed "PERFECT!" bonus on top of that.

## The Shop

Shop accessible in the central room   
randomizes stock every time a wave is completed   
2 permanent shop items: refill health/mana, maybe something else   
3-5 randomized items (TBD), with the option to freeze one between rounds so you can save for something you like  

meta upgrades to unlock more freezing.

the more rounds you have completed, the higher the quality of shop items, including finding items at higher ranks, but at a discount.

Be able to sell existing perks in your inventory, allowing you to "trade-in" for replacements

## Item Types

### Spells a.k.a. Weapons a.k.a. Attacks

in this game the weapons / abilities are all spells.
every spell changes how your attack works.

Main weapon: spammable, can alternatively charge
Secondary slot: can only Alt-fire, but can use main charge (interrupt main charge with alt-fire press)

Orb: bounces, main attack 
- charge: increases size, damage 
- ALT FIRE: Explodes on impact, effectively a rocket launcher with high knockback

Slash: horizontal slice wave 
- charge: increases width: individual hit chunks, decreases damage 
- ALT FIRE: Spin attack / Nova, sends everything out away from you

Force Wall: upright and flat, offensive shield 
- charge: increases size, lowers damage, higher defense: individual hit chunks. lower charge is denser, higher damage 
- ALT FIRE: Shield Bash/Charge moves quickly, massively increases knockback directly away from you, lowers damage 

DICE: shotgun, shatters on impact into smaller projectiles 
- charge: increases ball size->number of shatter "generations", child, grand, etc. 
- ALT FIRE:  ... Globulea of Tar trap to slow? idk

Lance: 3-hit spear thrust in a wide 90 deg cone (left 45 mid 45 right); can be sniper-ish 
- charge: Zoom-in, cone width narrows, delay between strikes shrinks, length of spears increases. precision damage--high crit, smaller hit box. full charge becomes one large piercing beam.
- ALT FIRE: ... stun beam? charge attack?

GARLIC: passive AoE damage 
- charge: continual drain to empower it temporarily, Maybe like Bible visual with spinners 
- ALT FIRE: Energy stream, continuous steady damage in a cone in front of you, pushes or slows slightly

Chakram / Glaive: boomerang, bounces between targets 
- charge: increases number of bounces before returning 
- ALT FIRE: Bolas, roping together bounce targets, drawing them into their central location upon the "return" trip

Missiles: volley of 3 small high damage pts, a la Model PX or VS Knife; charge Arcane Mage, locks on to targets 
- charge: turns into lock on, increases missile / lock-on count 
- ALT FIRE: Lift / pull from ME, zero gravity bubble from KH, suspends targets in the air helplessly
visual differences to differentiate

 
mana drain?
self buffs?


### Stats

Up to 3 passive slots -- select them again to rank up

max health
max mana
move speed
defense
money drop rate
pickup radius 
Jump Height / Air Jumps
Siphon Range / Speed
Projectile Speed
Projectile Size

## Meta Upgrades

"Account" progression, grows slower than boosts in-game, stacks with in-game boosts.

Unsure of the design of how these are unlocked, if it's a "meta" currency, or if it's the same as Gold.

### RNG 

Reroll hallways
Reroll shop
Banish ?
Freeze count in the shop
Sell Value - up to at most 100% of original cost, TBD

### permanent upgrades to minimum stats (individually toggleable)

max health
max mana
move speed
defense
money drop rate
pickup radius 
Jump Height / Air Jumps
Siphon Range / Speed
Projectile Speed
Projectile Size


## Enemies

enemies are described by a combination of several attributes:   

SIZE: small(or medium, counts in the same size category) or large   

MOVEMENT: GROUND or FLYING

RANGE: MELEE or RANGED(variable distances, but all projectile type attacks greater than melee range)   
2 * 2 * 2 = 8   

That is all that defines the core unit types / varieties, is these combinations. 4 Small types, and 4 large types. Then ALL of these units can have simple "artificial-difficulty" ranks, or difficulty tiers. Rank 1 is easy, 2 is normal, 3 is hard, and 4 is brutal. They will be simple reskins that make them measurably stronger, with more health and damage, probably speed, and an aggressiveness factor. Perhaps they may even unlock extra abilities.

RANK:    
	- Tier1_Bone,   
	- Tier2_Cloth,   
	- Tier3_Iron,   
	- Tier4_Obsidian,   


| Unit Name | Priority Objective | Speed  | Size / Armor / HP | Movement | Attack Range   | Attack Rate | Attack Damage | Spawn Cost | Description                                        | Inspiration Reference                                                            |
|-----------|--------------------|--------|-------------------|----------|----------------|-------------|---------------|------------|----------------------------------------------------|----------------------------------------------------------------------------------|
| Scrounger | Ship/Cargo         | Fast   | Light             | Grounded | Melee          | Fast        | Low           | 0.5        | Tenacious pawns, brainless cannon fodder           | WarCraft Ghouls, Destiny Hive Thralls                                            |
| Scrapper  | Ship/Engine        | Medium | Medium            | Grounded | Variable Range | Medium      | Medium        | 1          | Assault solider. Can wield any ranged weapon.      | SW Battle Droid, Destiny Hive Acolytes, Destiny Vex Goblins                      |
| Lunker    | Player             | Medium | Heavy             | Grounded | Melee          | Slow        | High          | 2          | Front-line Bruiser, Normal walk, SLOW melee attack | LotR Troll, Overwatch Reinhardt, Warcraft 3 Mountain Giant, Destiny Hive Knights |
| Buster    | Ship/Engine        | Slow   | Heavy             | Grounded | Mid Ranged     | Fast        | Medium        | 4          | Heavy Artillery, SLOW walk, fast midrange attack   | Team Fortress 2 The Heavy, Overwatch Mauga                                       |
| Swooper   | Ship/Cargo         | Fast   | Light             | Flying   | Melee          | Medium      | Low           | 0.5        | Buzzing flies, vultures, pests, thieves            | SW Geonosian, Stormgate Spriggan, Warcraft Harpies                               |
| Zapper    | Player             | Medium | Medium            | Flying   | Long Ranged    | Slow        | High          | 2          | Flying Snipers.                                    | Mass Effect Geth Hopper                                                          |
| Boomer    | Player             | Slow   | Heavy             | Flying   | Mid Ranged     | Fast        | High          | 3          | Death from above, the Flame Comes.                 | SW Flametrooper, Yer average firebreathing dragon                                |
| Shredder  | Ship/Hull          | Medium | Medium            | Flying   | Melee          | Medium      | Medium        | 3          | my what huge CLAWS YOU HAVE                        | Metroid Dread Emmi with Wings, tears things apart with ease                      |



## powerups

I still want some kind of arcadey powerups that make you temporarily stronger. I imagine, changing your main attack to a different shape, consumable traps to crowd control enemies, and buffs   


Traps:   
Icy floor lowers friction   
Floor is Lava, take damage and lose mana   
Goopy floor, slowed down   
Spike ball rolls through and knocks back  

temporary buffs:  
infinite ammo   
invincibility   
Invisibility while not attacking   
super speed   

Avowed? might have good first-person magic reference
A mix of guns and spells
Borderlands - Dragon Keep expac?
Tiny Tina's Wunderlands? - Spellshot class


---

old information

# History and Original Design Pitch (outdated concept info)

The Game Jam theme was spooky
Because next week is Halloween
Aaaaaaand all I could think of was this maniacal cackle: Spinal from the fighting game "Killer Instinct".
And then Joseph, of course he did, had a whole section in his master doc of game ideas dedicated to Spooky
And one of them sounded cool, but it really just made me think of the original DOOM
so then I had an image of pixel art skeleton and a jump scare
But I was stumped, because I was doing it top down design (theme first) and not bottom up (mechanics first)
So I didn't have a clue as to actual mechanics. 
I had a DOOM aesthetic but not necessarily those mechanics.
Aaaaaaand then BRAIN BLAST

I'm gonna freaking make "spell slingers"

# Wait, what's Spell Slingers?

## overview

Spell Slingers was a First Person shooter idea I had for my school's capstone game project.
It was originally going to be a 1v1 PvP game (player versus player) in an arena that gets smaller and more chaotic the longer the match goes on.
The main mechanic was based on Metroid Prime's charge beam. Particles of magic would scatter into the arena, and you had two main verbs: siphon, and expel.
If you charge your siphon, you will vacuum in magic energy, in a zero-sum contest for the raw material needed to fight.
Then that would become your ammo reserves. then you could charge up your expel move which would create a ball-like projectile that would get bigger, using more stored energy, the longer you charged it.
There were to be different properties, all physics based, to the projectiles. They were to bounce around the arena, ricocheting off walls based on the reflection of their collision angle.
Then there were going to be powerups that temporarily changed the shape and behavior of your projectiles.
For instance, instead of making a high density orb that does a lot of damage, you could get a wide spread wave, or a flat sheet of particles that would serve to "block" enemy projectiles.
the denser, the fast and more deadly. it was extremely emergent based on a simple set of rules for how the density of particles affected the interactions. in short, though, more particles would beat less particles.
Just to reiterate, particles were the pieces of magic that would be vacuumed up.

A note about these particles: they are supposed to "neutralize" each other. So when your particles collided with enemy particles, they both dissipate. 
If you fire an orb, and the enemy launches a wall, the particles that collide turn neutral, the orb loses mass and slows down, dealing less damage on impact.

here's a piece of the design info I wrote down

## original projectile notes

rightTrigger = Charge / Fire Magic
leftTrigger = Siphon Magic
rightBumper = Eject (impulse) / Cast Equipped Spell. impulse in front, can be charged
leftBumper = Parry / Guard
aButton = Jump (vertical)
leftStick = Move
rightStick = Aim
bButton = Dodge (Horizontal). dodge using the impulse in your current direction of input. Charging with RB also empowers dodge, costing more energy.
yButton = Switch Spells
xButton = Punch

Siphoning - happens passively in a small radius around you when not doing any other action (except moving). Heavy siphon becomes a strong focused dedicated channel as original

Eject - impulse in the direction you're aiming at the cost of some energy
It will do its math on all projectiles in its hitbox, and also push you in the opposite direction, including backwards, so be careful

Size thresholds in particle counts
So 0-10 small
Medium 11-60
61-90 Large
91-100 Huge

Larger things have lower acceleration
Smaller things have high acceleration
Projectiles hue shifts with damage for visual clarity

Parry flies in the direction you're aiming  

Orbs bounce on the walls and eject 5-10% of current mass on collision   

Small under 10 explode on collision and don't bounce    

Shape:   
	Line   
	Plane    
	Orb    

Wall: +defense -pierce -dmg   
Arrow: +Pierce(armor pen) -Defense -dmg   
Orb: +Explode(dmg) -defense -pierce   
Wave: + Spread(trample) - Pierce   
Shotgun: +Count - Size(dmg)   
Big: +Size -UpCost   

Orb / Point: 0   
Wave / Slash: +X (width)   
Slice / Chop: +Y (height)   
Lance / Arrow / Spear: +Z (pierce)   
Sheet / Card: +XZ (niche application)   
Plane / Slip: +YZ (niche application)   
Barrier / Wall: +XY (defense)   
Box / Cube / Field: +XZY (lower skill, simple to use, decent defense, low damage)   

## Siphon

right now it IS alt-fire (other trigger/MB)

instead, it could be "another weapon", where pressing 2 or scroll wheel will switch, 
then it can have a primary and alt mode for firing as well
maybe push vs pull
OH YEAH SPELL SLINGERS HAD PARRY

## Spell Slinger's pitch script

It’s time to DUEL! Grow your magic power and blast away your opponents in this fast-paced first-person magic shooter. Vie for control of finite resources as you absorb, deflect, and retaliate the magic all around the arena in a contest for domination! 

High in the towers of an arcane sanctum, two apprentice sorcerers stand face to face on an arena platform suspended high in the air. The timer counts down… and with a POP, clouds of magical energy burst into the air between you two. Wielding a specialized gauntlet, you rush to absorb as much of the mana particles as you can, siphoning them in from a distance, while keeping an eye on your opponent for any sudden moves. Then you see a flash, and an aggressive wave of magic comes flying your direction. You barely manage to guard against the blast, and then it flies off to shatter on the barrier behind you, dissipating its energy back into the field. You take advantage of the opportunity by barraging your opponent with a series of smaller blasts, trying to catch them off guard. As they hide behind their shield, you lob a much larger blast at them, but right at the last second, they deflect it back at you! You retaliate with a parry of your own, and the energized ball of magic picks up speed, magnetizing nearby floating energy and growing in size as it flies. After a few more volleys of increasing intensity, the ball becomes huge! The next missed parry will decide the match, as the massive orb explodes and sends someone flying… will you fall to your shameful demise? Or will you rise to defeat your adversaries in the spell slinging showdown?!

## other details about Spell Slingers

the neutral particles were purple, yours were blue, and the enemies were red. so when red and blue collide, they'd revert to purple.
There were large negation zones akin to "soccer goals" on the walls on the opposite side, where freeflying particles would be brought back into the arena's pool.
then once the pool hit a certain threshold, the same cannons that emit particles at the start of the match would fire again, releasing more neutral energy into the play space.
There were also crystals that could spawn in the arena that serve as a big "ammo refill"


## objective

the original Spell Slingers had a PVP objective: last man standing, literally. the platform would shrink as the battle continued. the projectiles have knockback properties. once you blast someone off the stage, you win the round.

In this game, I imagine waves of enemies that spawn. You just have to defeat them.
I don't know what health looks like yet in this game, maybe it's just like Vampire Survivors where it really is just about survival until it's over. So last until time, or die. So we could have rare consumable pickups that heal.
