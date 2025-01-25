!!! warning
    While the pathfinding solution has been implemented, there are no standardized traversal components that utilize the paths generated from the pathfinding system.
    Until a proper traversal component is created for mobs/ships, you're on your own.


# Understanding Unitystation's pathfinding solution.

- Unitystation uses a slightly modified version of BFS.

- Each matrix comes with a `Pathfinder` object attached to their MetaDataLayer, and is designed to automatically use the cached chunk data that tilemaps hold. We don't need to generate a secondary grid, as we already reuse data that exists.

- To allow for extra flexibility, the pathfinder only treats nodes that are registered as `Occupied` as obstacles; and will ignore stuff like tables, windowed doors, and specific unpassable objects, to allow advanced traversal behaviors for future MobV2s (such as climbing tables, or smashing windows).

- Pathfinding does not work across two or more matrices. If a path strays out of a matrix's bounds, it will return an empty path.

- While the current pathfinding solution has minimal impact on performance, it is still advised to avoid spamming functions related to it; especially on larger matrices.

# How to generate a path from A to B.

First, you must determine which matrix you are going to pathfind on, so we can grab the pathfinder object that's on it.

!!! Tip
    You can quickly grab the matrix a mob/object is standing on by simply doing this:
    `Matrix matrix = gameObject.GetMatrixRoot();`

You can access the pathfinder by checking the `MetaDataLayer` component that is associated with the matrix. It will look something like this: `matrix.MetaDataLayer.Pathfinder`

after finding the pathfinder, you can generate a path using `FromTo()`.

### FromTo

```c#

List<Vector3Int> path = Pathfinder.FromTo(terrain, from, to);

```

Let's break down the arguments.

- Terrain (ChunkedTileMap<MetaDataNode>): The tilemap the traversing game object will be moving on. (Both must be on the same one)
- From / To(Vector3Int): Must be in local coordinates!! World coordinates will not work.

the returning path will be a list of vector3s that will find the most optimal path to where you're trying to go. This list will be null/empty if no viable path is ever found.


!!! warning
    Airlocks are considered walls if they're closed, thus preventing paths from fully generating if rooms are enclosed.

After receiving the path, you can then pass it to a traversal component that handles the movement of game objects.

If you want to bruteforce current MobV2s to move in a path for quick testing, the `PathfindingUtils` class has a function called `ShoveMobToPosition` that can be used to quickly push mobs.

Example:

```c#

List<Vector3Int> path = Pathfinder.FromTo(matrix.MetaDataLayer.Nodes, mob.gameObject.GetLocalTilePosition(), MouseUtils.MouseToWorldPos().ToLocal());

foreach(var point in path)
{
    PathfindingUtils.ShoveMobToPosition(mob, point, 12f);
}

```

If you want to also visualize that path before acting on it, you can use `Visualize()`, which is also inside the `PathfindingUtils`