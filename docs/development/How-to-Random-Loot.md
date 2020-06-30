# How to use random spawners

Ok, so you want to include some *surprise mechanics* in your map? Wonderful, but first let's define a couple concepts so we're all in tune and you can understand this guide.



### Understanding the system

The Random spawner system, spawn loot or however you want to call it uses two critical elements to make it work:

1. **Random Spawn Spot** (name subject to change): is a [**Prefab**](https://docs.unity3d.com/Manual/Prefabs.html) that contains all the logic to spawn random stuff at its position. You will use this object to define a position in your map to spawn something when the game is running. Simply drag it from the project view to the desired location in the scene view.
2. **Random Item Pool** (name subject to change): is a [**Scriptable object**](https://docs.unity3d.com/Manual/class-ScriptableObject.html) that contains a list of values. This is just a data holder, but it is important because it has all the possible items this pool can spawn and their probabilities, among other cool stuff we will inspect later.

Alright, so how do we get started? Let's take a look at a spawner that has already been done before we make our own.

1. Navigate to the folder where the Random Spawn Spots are, at the time of writing this guide that is ``Assets/Resources/Prefabs/Items/Spawners``
   ![spawners folder](https://i.imgur.com/GggkXhZ.png)

2. Left click on the ``RandomTool`` and take a look at the inspector view.

3. If you go to the bottom of the component list, you will find the ``RandomItemSpot`` component.

   ![component](https://i.imgur.com/dQEt0iG.png)

Let's go from Top to bottom,

- Loot count (number) is the amount of items this spawner should spawn. 
- Fan out (checkbox) tells the spawner if it should spread out the items on spawn.
- Pool List (list of pools) is, as its name implies, a list of the possible pools this spawner can choose to spawn a random something. It has a couple properties too, let's look at those.
  - Size (number) is the size of the list. A size 0 is an empty list, a size of 2 has 2 elements. Each element is a pool, which also have some properties.
    - Probability (number from 0 to 100) is actually more like a weight, how likely is it to choose this pool when we pick a random one.
    - Random Item Pool (RandomItemPool) is the reference to the scriptable object that contains all the data we need to spawn one item.

4. Do a left click on CommonTools, so the project view will change to its location in the folder, at the time of writing this guide ``Assets/Scripts/RandomItemPools``, then take a look at the inspector view.

   ![commonTools inspector](https://i.imgur.com/PjY4G5x.png)
   

Again, let's go from top to bottom defining what each property does.

- Size (number) is how many items we want to define in this pool. 0 is an empty pool, 8 means 8 elements in this pool and each element has these properties:
  - Prefab (GameObject) is the prefab of the item we want to spawn
  - Max amount (number) is how many items we want to spawn, this goes from 1 to whatever number we input here. If we set 10 as Max Amount, then the system will spawn anything from 1 to 10 of this item.
  - Probability (number from 0 to 100) is again, a weight of this item in this particular pool. How likely is this item to be spawned instead of another if this item is chosen randomly.

That's pretty much it. Let's see how  it operates in action...

### Spawning random loot cycle

1. Map a spawner.
2. The game starts, the spawner will run its cycle which is...
3. Pick a random pool from the list.
4. Roll the pool probability, if it doesn't pass, jump to step 3, else...
5. Pick a random item from the pool.
6. Roll the item probability, if it doesn't pass, don't spawn anything, else spawn the item.
7. The spawner deletes itself.



### Creating your own spawner

The intention behind this design is to make the loot spawners very flexible and to cover any need you might have. If you think the spawner you want to make can be reused by other mappers, go to the spawner folder and make a **prefab variant** of it. Fill the required data as you wish.

![prefab variant](https://i.imgur.com/pLNs6i0.png)



Wait but, the available pools don't solve my requirements, how do I create a new one?

### Creating your own pool

Go to the pools folder and do a left click then ``Create/ScriptableObjects/RandomItemPool``, lastly fill the required data (and assign this pool to your previously created spawner).

![create pool](https://i.imgur.com/rqlVEtB.png)



### Interesting facts about Random Spawners

- You can use spawners in pools and make a spawner of spawners.
- You can spawn spawners with the admin tools.
- You can use spawners in crates and lockers.

- You can use spawners in populators **if the items are not meant to be equipped by a player** once spawned (for example, the occupation populators).

- You can use mobs in pools.

- You can change the sprite of a spawner so mappers know at a glance what is it.

- If you set low probabilities in your pools, the spawner will do 5 attempts before interrupting everything and it won't spawn anything. Try group low prob pools with a 100 pool, so if the low prob fails it defaults to the 100 one.

- If you want your spawner to spawn something with low probs or nothing, use a 100 weight on its pool but make the items itself have low prob. An example of this is the Xeno Egg spawner, with 100 weight on its only pool but 2% chances of spawning the egg.

  