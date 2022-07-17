DEPENDENCIES
-MBSTools (https://github.com/DevJamesC/MBSTools)
-Addressables

To create something that launches projectiles, 
1. Extend ProjectileEmitter.cs
2. Set localTimescaleValue and localGravityValue in start or update (presumibly to Time.Timescale and Physics.Gravity)
3. Call the Launch() function to emit a projectile
4. Override the OnHit() method to add functionality when a projecile hits something

To create a projectile
1. Create a projectile scriptable object using the context menu (left click in project tab)
2. Adjust settings and add projectile graphics
3. Add stages for advanced projectile functionality

Look into TestLauncher for an example of how to set up some projectile launching functionality

Methods to Override
OnHit()
OnProjectileDie()
OnProjectileRicochet()
OnProjectilePenetrationExit()

Add MaterialToughness.cs to a trigger or collider to give it slowdown data for projectiles. Useful for things like water. Put the script with the collider, or on its parent.
Add ProjectileColliderHandler.cs to the root of any extremely fast moving object (or regular moving object against slow projectiles). It will destroy and be hit by any projectiles that are enveloped by the object in a single frame.

Create a MaterialTag for each type of material (such as water, wood, metal, ect.) and a ProjectileTag for each type of projectile (such as explosive, laser, solid projectile, ect.)...
Then create a ProjectileMaterialEffectDictonary to link MaterialTags to ProjectileTags so different types of projectiles can have differnt sounds and particle effects when they are launched from within a material or when they hit a material

Particle systems should have Play on Awake set to True, Stop Action set to Disable, and Looping set to False.
To give a particle system a rotational or position offset, change the rotation or position of its first child in the prefab (the root rotation will be overridden, so use the first child instead). Position offset will be relative to the hit direction

To make a nearMiss sound, create a NearMiss materialTag, and set it in the dictionary with only the nearmiss field set. The attach the nearmissListener to the listener object, with its collider set to trigger being the radius where it can hear the nearmiss sound. Assign the nearmiss material to the MaterialTagComponent that comes with the NearMissListener.

To have a trigger which interacts with projectiles, but does not incure hit FX or sounds, attach a MaterialTagComponnet to the trigger and assign a NoFX material tag, or any material tag with no FX or sounds set in the EffectDictonaryLookup.
