# Understanding Unitystation's pathfinding solution.

- Unitystation uses two methods for pathfinding, a slightly modified version of BFS and A*.

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


# MobTraversal and A*

!!! warning
    While A* is generally considered faster than BFS, It does generate lower quality paths that are inconsistent.

Every MobV2, including players, has a component called `MobTraversal`. This component is responsible for automatically finding paths and moving the mob across the map.

### Key Features:
- **Automated movement**: Mobs can be directed to move to a target location using the same movement players use.
- **Queue system**: Multiple movement targets can be queued, ensuring fluid transitions between destinations.
- **Retry logic**: If movement fails due to obstacles, the system will retry up to a set number of times; and programmers can hook into events emitted on traversal steps.
- **Traversal strategies**: Custom logic can be implemented to handle special cases (e.g., opening doors, climbing tables, punching windows, etc).

### Using MobTraversal
To move a mob to a new target location, use:
```csharp
bool success = mobTraversal.QueueMovementGoal(targetPosition);
```

#### Parameters:
```csharp
QueueMovementGoal(Vector3Int newTarget,
    Action onTraversalFinalStep = null,
    Action onRetryMoveToDirection = null,
    List<ITraversalStrat> strategies = null,
    bool cancelOnSlip = false)
```

- `onTraversalFinalStep`: Callback when traversal is complete.
- `onRetryMoveToDirection`: Callback when retrying movement.
- `strategies`: List of traversal strategies (e.g., opening doors).
- `cancelOnSlip`: Stops traversal if the mob slips.

All these parameters are considered optional, you don't need an reaction for every event; but these offer you the ability to do "run and gun" scenarios without having to wait for the entire traversal process to be done for you to take more actions.

### Movement Process
1. The pathfinder is called, and will attempt to return a list of positions that the mob will attempt to traverse through.
2. If the path is valid, movement begins.
3. If the mob is unable to reach the next position on the `path` list, retries are attempted, and `OnTraversalFailedAndRetrying` will be triggered.
4. If more than one retry is attempted, `AttemptStrategies()` will be called.
5. Strategies will attempt to call for a pause period for them to finish their actions.
6. If the final destination is reached, `OnDoneTraversalToLocation` is triggered.
7. If movement completely fails, `OnTraversalFailedCompletely` is triggered.

### Example Usage
```csharp
mobTraversal.QueueMovementGoal(new Vector3Int(10, 5, 0),
    onTraversalFinalStep: () => Debug.Log("Traversal complete!"),
    onRetryMoveToDirection: () => Debug.Log("Retrying movement..."),
    cancelOnSlip: true);
```

### Handling Movement Failures
If movement is unsuccessful:
- `OnTraversalFailedAndRetrying` is invoked before retrying again.
- If all retries are exhausted, `OnTraversalFailedCompletely` is called.

### Movement Execution
The `Move()` method is responsible for moving the mob toward its target:
```csharp
Tuple<bool, bool> Move(Vector3Int targetPosition)
```
**Return Values:**
- `Item1 (bool)`: Whether movement was successful.
- `Item2 (bool)`: Whether the mob slipped.

If movement fails repeatedly, traversal strategies (`ITraversalStrat`) are attempted.

# Traversal Strategies

Traversal strategies are used to help mobs navigate obstacles dynamically. These strategies define checks and actions that allow mobs to overcome objects or terrain that would otherwise block their movement.

## Implementing a Traversal Strategy

A traversal strategy is defined by implementing the `ITraversalStrat` interface, which includes:

- `ObsticalCheck(Vector3Int obsticalPosition, PlayerScript mob)`: Determines whether the strategy can be applied to a specific obstacle.
- `TraverseObstical(Vector3Int direction, Component obsticalObject, LayerTile obsticalTile, PlayerScript mob)`: Executes the strategy to overcome the obstacle.

### Example: Climbing Tables

Here is an example of a traversal strategy that allows mobs to climb over tables.

```c#
using System;
using Tiles;
using UnityEngine;

namespace Mobs.Traversal.Strategies
{
    /// <summary>
    /// Traversal strategy for climbing tables.
    /// </summary>
    public class ClimbTable : ITraversalStrat
    {
        public int ClimbSpeedInMilliseconds = 3135;

        public Tuple<bool, Component, LayerTile> ObsticalCheck(Vector3Int obsticalPosition, PlayerScript mob)
        {
            var table = mob.RegisterPlayer.Matrix.IsTableAt(obsticalPosition);
            return new Tuple<bool, Component, LayerTile>(table != null, null, table);
        }

        public int TraverseObstical(Vector3Int direction, Component obsticalObject, LayerTile obsticalTile, PlayerScript mob)
        {
            if (obsticalTile is BasicTile table)
            {
                foreach (var interaction in table.TileInteractions)
                {
                    if (interaction is TableInteractionClimb climbInteraction)
                    {
                        climbInteraction.StartClimbing(false, mob, direction.ToWorld(mob.RegisterPlayer.Matrix), direction, table,
                            mob.ObjectPhysics, mob.RegisterPlayer.Matrix.TileChangeManager);
                        return ClimbSpeedInMilliseconds;
                    }
                }
            }
            return 0;
        }
    }
}
```

### How Traversal Strategies Work

1. `ObsticalCheck` determines whether the strategy can be applied to the current obstacle.
2. `TraverseObstical` executes the movement logic if the strategy is applicable.
3. If a strategy is successful, the mob proceeds; otherwise, alternative strategies may be attempted.

This system allows for dynamic and modular traversal solutions, making it easy to add new behaviors like jumping over gaps, breaking down obstacles, or squeezing through tight spaces.

### Assigning strategies in the editor.

As we want to allow designers to easily create new mobs from scratch, we want to allow them to also reuse already made strategies across different mobs very easily without touching code.

When creating a list of strategies, you can allow everyone to fill the strategies they need in the editor by simply adding these attributes to the list:

```cs
[SerializeReference, SelectImplementation(typeof(ITraversalStrat))]
public List<ITraversalStrat> TraversalStrategies = new List<ITraversalStrat>();
```