<h1>Movement</h1>


There are 2 types of movement

1. Tile movement
2. Newtonian movement


<h1>Tile movement</h1>

TryTilePush Is the initial Step into tile pushing, This does all the Checks with can move, 
And then goes into ForceTilePush, This allows you to do some cheeky stuff like pushing into walls, 
But also allows you to skip validation checks.

Once this is done it sets SetLocalTarget, and adds AnimationUpdateMe to the Update manager,
this function handles moving towards LocalTarget, This will move the game object towards the target until reached,
it has options to apply momentum once it has reached the location



<h2>Newtonian movement</h2>

newtonianMovement Is the vector2 xy of the current momentum of the object, 
All the calculations for it are handled in side of FlyingUpdateMe, 
There are two extra attributes Important to momentum,


airTime = How long the object will travel with no drag

slideTime = How long the object will travel with reduced drag

FlyingUpdateMe Also handles the collisions and bouncing for objects.

<h2>Notes</h2>

IsStickyMovement changes how The object,
 Calculates if it has gravity, ( it checks all adjacent tiles Instead of just underneath it),
 And also brings the momentum to a stop when it reaches the threshold of maximumStickSpeed,

<h2>MovementSynchronisation</h2>

This handles all the player Prediction and stuff,

How does it work? so
it works on the principle of,

Client Side, Requests move, starts moving, Finish moving , Requests move, starts moving

Ping -------------------------------------------------------------------------------------------------------------------

Server Side ping, ________, Receives move, starts moving, Finish moving _, Receives move

so, What happens on the client is the same on what happens on the server then you're okay, 
but every so often there are times where that's not true, What the server does is,
Check the

Distance of the client location and server location ( they should be exactly on top of each other since, the requests are exactly aligned with server )
Reset if they're too far apart, 

Checks if the player can move to the specified Place, Reset if not,

If the player move, Checks if the player had push The same objects as on client,
If not reset those objects,

Checks if the player slipped or not, Resets if it doesn't match Client

This means that anything that affects movement has to happen client side to, 
If you want it to be responsive
