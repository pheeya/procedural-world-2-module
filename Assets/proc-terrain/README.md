## Procedural generation in unity
This is an old project that I picked up again, originally based on Sabastian Lague's "Procedural Landmass Generation" series on YouTube.
Currently, this project is mainly being developed for my upcoming game (unnamed and unannounced as of 12/03/2024).


## Goals
Contrary to the original version, the project is now focused on generating a single finite map upon starting the game as opposed to generating endless terrains at run time. Adding support for endless terrains will not be difficult but since my game does not need that, the project will remain focused on pre-generating the terrain at load time. 

With that in mind, here are the goals:

- Generate chunks of seamless terrains with LODs and distance culling
- Add procedurally generated roads using another, highly configurable, noise layer
- Place props and vegetation with physics collision check
- Triplaner shading 
- Stamping pre-made heightmap textures (brushes) at random locations with constraints such as min distance from other stamps and collision avoidance
- Fixed areas that override the terrain at desired locations with smooth blending in existing heightmap, for example to add a flat spawn point for the player
- Multithreading
- No plans for GPU computation currently since I lack the experience with that and I'm not sure if doing that will introduce limitations and complexity.

## Current Status
Not ready for production. I am developing this alongside the game it is being developed for so I don't expect this to be production ready earlier than late 2024.
Already implemented: 
- Generate chunks with a single LOD level
- Add single road 
- Set number of chunks to generate


