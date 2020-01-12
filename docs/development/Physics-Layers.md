# Physics Layers

This page is a continual WIP and tries to be an up-to-date reference on the physics layers (NOT the sorting layers) in the game and what objects should be assigned to each one.

As layers have evolved over time, there are definitely opportunities to clean these up, so don't take this as set in stone.

1. Default - no prefabs should use this for their top-level object (but it seems okay to use in child objects)
2. TransparentFX - unused?
3. Ignore Raycast - unused?
4. Water - unusued?
5. UI - for UI elements
6. Players - for the local player's game object.
7. Walls - for walls in the station
8. Items - for things that can be picked up - guns, ammo, paper, pen, etc...
9. Machines - Seems to be for things which are impassable and should collide with bullets but don't fit into the other layers. It's called "machines" but actually lots of things that aren't machines use this. Canisters, closed closets, consoles, etc...
10. OtherPlayers - for player objects other than the local player.
11. Lighting - I think it's for things which interact with the lighting system.
12. Furniture - seems to be basically the same as Machines. Probably can be merged with Machines.
13. Bullets - for projectiles.
14. Door Open - open doors.
15. Door Closed - closed doors.
16. Windows - for windows.
17. WallMounts - for wall mounts - the things that go on walls.
18. HiddenWalls - not sure, might be leftover.
19. LightingSource - Not sure.
20. Floor - for things that aren't items yet are on the floor, such as wires or pipes that have been placed.
21. Unshootable Machines - just like machines, but doesn't collide with bullets. Currently only used by chairs.
22. Matrix - for the Matrix object itself.
23. Objects - doesn't appear to be used widely. If it is, can probably be merged with Machines.
24. Ghosts - for all ghosts.

