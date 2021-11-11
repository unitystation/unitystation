
# What is a matrix
So, A matrix is something like a station or asteroid or shuttle,
so, In the route of the world there are game objects, that are the matrices, They contain the game object for the matrix, and network identity,
the children of the matrix, Consist of the tile maps, such as floor, windows and base, That handle rendering of the tiles present,

then there is the object layer, That is a game object and stores every single object/Item on that matrix, 


since every Matrix is a Separate object they can all move independently, 
For the purpose of tile placement and object movement, 

There are to coordinate systems, 
world position,  that is Calculated from the route of the world
and then local position, that is the position on the Matrix, so if you are at the centre and it was properly 0,0 you'd be at the centre of the matrix

Matrices also contain a meta data layer,
Per tile, it stores data like, atmospheric Gasmix, Pipe data for each tile, Electrical data for each tile (If it's a cable) and Any extra data you want stored per tile