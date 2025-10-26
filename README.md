# Spinal Shatter
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

# Spell Slingers

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

## pitch 

It’s time to DUEL! Grow your magic power and blast away your opponents in this fast-paced first-person magic shooter. Vie for control of finite resources as you absorb, deflect, and retaliate the magic all around the arena in a contest for domination! 

High in the towers of an arcane sanctum, two apprentice sorcerers stand face to face on an arena platform suspended high in the air. The timer counts down… and with a POP, clouds of magical energy burst into the air between you two. Wielding a specialized gauntlet, you rush to absorb as much of the mana particles as you can, siphoning them in from a distance, while keeping an eye on your opponent for any sudden moves. Then you see a flash, and an aggressive wave of magic comes flying your direction. You barely manage to guard against the blast, and then it flies off to shatter on the barrier behind you, dissipating its energy back into the field. You take advantage of the opportunity by barraging your opponent with a series of smaller blasts, trying to catch them off guard. As they hide behind their shield, you lob a much larger blast at them, but right at the last second, they deflect it back at you! You retaliate with a parry of your own, and the energized ball of magic picks up speed, magnetizing nearby floating energy and growing in size as it flies. After a few more volleys of increasing intensity, the ball becomes huge! The next missed parry will decide the match, as the massive orb explodes and sends someone flying… will you fall to your shameful demise? Or will you rise to defeat your adversaries in the spell slinging showdown?!

## other thoughts

the neutral particles were purple, yours were blue, and the enemies were red. so when red and blue collide, they'd revert to purple.
There were large negation zones akin to "soccer goals" on the walls on the opposite side, where freeflying particles would be brought back into the arena's pool.
then once the pool hit a certain threshold, the same cannons that emit particles at the start of the match would fire again, releasing more neutral energy into the play space.
There were also crystals that could spawn in the arena that serve as a big "ammo refill"


# back to this game, Spinal Shatter

So, now it's going to be a PvE (player vs. "environment" i.e. non-players) DOOM like FPS. I don't need all of the complexity of the old system designs, but I do want some degree of the core essence of it somehow.
I'm wondering about how to translate it, but I think the core things that I want are a charge-able projectile attack that bounces off of walls. 
Siphoning may be cool to keep still, potentially, again very Metroid inspired.

Enemies will have 3 attributes that will define general behavior, with specific versions having more particular behaviors on top of that.

Size: Big or small. 
