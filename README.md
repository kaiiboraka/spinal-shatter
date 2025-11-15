# Spinal Shatter

A retro-styled First-Person roguelite Shooter, inspired by Arcade Hard games of old. Originally developed for the BYU Game Development Club's Fall 2025 Game Jam, to the theme of "SPOOKY".

## Fantasy

You, a lone sorceror, awaken in a dark, dank dungeon. The last thing you remember is the cackling of the wicked necromancer who had cornered and captured you. With no natural light to speak of, you conclude that the only way out is through. Armed with only your knack for spellcraft, you must now venture out into the darkness to chew through an onslaught of undead hordes, and grow in your magic power enough to claim your revenge, and your freedom...

## this game is most like...

### Aesthetic Inspriation

- DOOM (1993) 
- Vampire Survivors

### Mechanic Inspiration

- Vampire Survivors
- Halo Firefight 
- DOOM 2016
- Hades

## Game loop

### Context: Map structure

A central hub connects to 4 hallways, in each of the cardinal directions, each with its own unique arena room at the other end. The entrance to each hallway is identified by a torch of a unique color. The torches being lit represents them being "open"/"active" to choose, while being unlit represents them being closed and unavailable. Also in the central hub are a number of interactable items and/or NPCs that modify game mode settings, or provide progression systems for the player.

At the start of a new run, or amid a run and directly after finishing a round and exiting its arena room, 2 randomly selected hallways torches are lit (excluding the one that was just completed, if any). Each of the lit hallways has a completion reward associated with it, displayed diegetically.

Each quadrant has a different visual and mechanical theme to it.

### Scenario walkthrough

#### Introduction

The game begins. I am in the central room. Two of the 4 hallways have their torches lit, with an icon of some sort showing what the reward would be for me were I to go down that hallway into that room and complete the challenge beyond. I choose the path in front of me simply by going down into the chamber at the end of the hallway. Once I enter the room, its Door locks behind me, and the Round starts. The Round is comprised of multiple Waves of randomly selected enemies. 

#### Enemy Spawning

Internally, the game is keeping track of the number of waves and rounds I have completed. Each successful wave increases the game's "budget" for spawning by a small amount, and each Round completed increases it by a significantly larger amount.  It then spends this budget on a randomized selection of enemies utilizing greedy Knapsack algorithm for selecting the largest, most difficult (and therefore most expensive) enemies first, and filling in the rest with smaller and smaller foes until it's filled.

#### What happens after a round is beaten?

I beat some number of enemy waves with my life intanct, so I make it back through the now-open Door. It then closes behind me and the round ends, resulting in a payout screen that gives me a bunch of money. I am also granted the reward promised in the hallway before I chose it.

Then I re-enter the central hub and visit the Shop to spend some of the money I gained on acquiring upgrades for my character on this run.

After I'm content, I then pick another room and begin the loop again.

#### TL;DR
choose a hallway, enter a room, kill the monsters, get out, get paid, get buff.

## objective



Don't Die for as long as possible, trying to last until the end

Lasting for number of waves instead of amount of time, but could have timed waves too
set of waves in a room could end with a "boss"

## Mechanics

### Controls

| KBM / Controller | action  |
| ---  | -- |
| WASD / leftStick | Move |    
| Mouse / rightStick | Aim |    
| LMB / rightTrigger | (Press) Attack / (Hold) Charge Magic |       
| RMB / leftTrigger | Secondary Weapon ability (a.k.a. "Alt fire) |    
| MB4 (maybe E? maybe Ctrl?) / rightBumper | Siphon    
| MB5 / leftBumper | EITHER : (Parry / Guard?) or (use consumable, like trap?)     
| Space / southButton | Jump (vertical) |   
| Shift / eastButton | Dodge (Horizontal) |    
| ??? / northButton | ??? |    
| F / westButton | Melee attack |    


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

Control wise, right now it IS alt-fire (other trigger/MB)
instead, it should be "another weapon", where pressing 2 will jump to it, or scrolling wheel will cycle through, 

### Powerups

There will be arcadey powerups that make you temporarily stronger. For instance, consumable traps to crowd control enemies, and buffs of various sorts.

Traps:   
- Icy floor lowers friction, lose directional control  
- Floor is Lava, take damage and lose mana   
- Goopy floor, slowed down   
- Spike ball rolls through and knocks back  

temporary buffs:  
- infinite ammo   
- invincibility   
- Invisibility while not attacking   
- super speed   

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
- ALT FIRE:  ... Globules of Tar trap to slow? Caltrops?

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
