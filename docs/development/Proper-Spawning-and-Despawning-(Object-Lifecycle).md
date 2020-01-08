# Proper Spawning and Despawning
This page describes the lifecycle system, which provides ways to easily spawn / despawn arbitrary objects
and straightforward hooks for invoking logic when those things happen.


## Spawning and Despawning API
The primary API for spawning stuff is in `Spawn`. Despawning is handled in `Despawn`. You should use these classes whenever you
want to spawn or despawn anything (with one exception noted below). Refer to the documented methods for
details.

As an exception, spawn logic related to anything player-controlled, such as spawning the player,
respawning, becoming a ghost, repossessing a body, etc...are handled in `PlayerSpawn`.


## Lifecycle

Object Lifecycle refers to how objects are created and destroyed. Your component can hook into these events via our **lifecycle interfaces**. Server side spawn / despawn events can be hooked into by
implementing `IServerSpawn`, `IServerDespawn`, or `IServerLifecycle`. Client side logic can be hooked into via implementing `IClientSpawn`, `IClientDespawn`, or `IClientLifecycle`.

Unity already provides built in lifecycle methods [as documented here](https://docs.unity3d.com/Manual/ExecutionOrder.html), but there are some special circumstances in Unitystation
which necessitate these interfaces: namely **Object Pooling**.

For all objects, we use a technique called object pooling. Objects are created ahead of time and added to a pool, then simply moved into position in the game when an object of that type needs to be spawned. When a pooled object is despawned, it is simply returned to the pool. This avoids expensive object creation and destruction logic. It is particularly useful for things like bullets or casings, which spawn rapidly. But it creates some complications due to re-using objects which may be in different states from when they were originally despawned. The Unity lifecycle methods are unaware of object pooling, hence the need for our lifecycle interfaces.


### When are lifecycle hooks invoked?

In addition to the [standard Unity lifecycle](https://docs.unity3d.com/Manual/ExecutionOrder.html)...

`IServerDespawn` and `IClientDespawn` hooks are invoked when the object is going to be despawned.

With `IServerSpawn` and `IClientSpawn`, it's a slightly more complicated. They are invoked in the following circumstances:
  * When the scene loads (invokes client and server hooks)
  * When a client joins (invokes only the client hooks)
  * When an object is spawned (invokes client and server hooks)

Another way of thinking about it:
  * Client - hook gets invoked the first time that the client learns of the object's existence
  * Server - hook gets invoked when the object is first created, regardless of whether it has been mapped
into the scene or spawned after the scene has loaded.

### What lifecycle methods should your component implement?

Remember, due to object pooling **Awake() / Start() is not enough!**. Those methods will only be called when the object is first created. If your
object is spawned from the pool and doesn't implement our lifecycle hooks, it will be in whatever state it was when it last despawned.

Given that...
1. Awake() should be used for one-time things, which won't need to be re-run if the object is spawned from the pool. This is a good spot for caching components to fields via GetComponent()
1. Most components should implement at least `IServerSpawn` and should properly initialize / reset their actual mutable state there. 
1. Components on objects which are mapped and configured in the scene can check for `SpawnInfo.SpawnType == SpawnType.Mapped` in
the lifecycle hook method in order to determine if they should initialize to a "blank slate" state or preserve their mapped configuration.
1. (If using SyncVars, also see [[SyncVar Best Practices for Easy Networking]].

### Testing / Debugging Object Lifecycle
You should test to ensure that your object properly despawns / initializes itself, even when it is pooled.

You can use the Dev Spawner / Destroyer tools to spawn and despawn your object. However, this does not test how the object behaves when it is pooled, as there's no guarantee it is spawned / despawned from the pool.

You can use a special right click menu command, "Respawn" to simulate despawning the object into the pool and spawning it back out. Recommended testing procedure:

1. Use dev spawner to spawn an object that contains your component. This will ensure it gets the PoolPrefabTracker component so it will be pooled. Objects which start in the scene do not have PoolPrefabTracker thus aren't pooled.
1. Validate that your object initializes itself and works properly. In general, you mustn't rely on an object being mapped in the Scene for it to work - all objects in the game should be spawn-able (though there may be a few rare exceptions).
1. Do something to the object in the game that would modify your component's state.
2. Right click the object and choose the "Respawn" option. This will simulate putting it into the pool and taking it back out.
3. Validate that the respawned object behaves as if it was newly-spawned and initialized, and has nothing left over from its state before it was respawned.
4. If there is some leftover state or any errors with initialization, make sure you have properly implemented any necessary lifecycle interfaces and validate the logic you have for any Unity methods such as Awake, Start, OnStartClient, OnStartServer.
5. Destroy your object using Dev Destroyer.
6. Validate that the object properly destroys itself - it no longer affects the game world and there aren't any errors caused by the object being destroyed. If there are any problems, ensure you have implemented necessary destruction logic in `IServerDespawn/IClientDesapwn` hooks in your component.

### Examples

See `ItemStorage`. It implements a mix of server and client side lifecycle interfaces. You can also search through the codebase for usages of the various lifecycle interfaces.
