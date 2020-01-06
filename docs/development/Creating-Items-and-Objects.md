# Creating Items and Objects

Want to create a new item or object? Read on to find out how.

## Cheat Sheet

1. Determine the best parent prefab for your object and create a prefab variant of it. See [Picking the Best Parent Prefab](#picking-the-best-parent-prefab)
1. Find a good place to put it in the folder structure.
2. Fill in its ItemAttributes and Integrity settings.
3. If creating clothing, create the cloth data asset and select it in the Clothing component.
3. If creating a non-clothing item, set up the inhand sprites in the SpriteDataHandler component.
3. In the hierarchy for the prefab, select the Sprite child object and set the Sprite Renderer sprite to an icon representing the object. Make sure the SpriteRenderer's sorting layer and order in layer are correct.
3. Ensure the object's physics layer is correct. The child Sprite's physics layer can be left at default (but NOT the SpriteRenderer sorting layer).
3. Edit the fields on the other components as necessary.
4. Develop new components or modify the code for existing ones as needed. See [Component Development Checklist](Component-Development-Checklist.md)
5. Test! Don't forget to test the following common sources of issues:
   * Test in multiplayer with at least 1 client and 1 server, trying to use the object as client and as server player.
   * [Test using Respawn](Proper-Spawning-and-Despawning-(Object-Lifecycle).md)
   * Test with round restarts.
   * Test on moving / rotating / rotated-before-joining shuttles.

## Items and Objects
We usually call things "items" if they can be picked up, and "objects" if they can't.

## Prefabs, Prefab Variants, and you

[**Prefabs**](https://docs.unity3d.com/Manual/Prefabs.html) are the Unity way of creating templates of objects that will be re-used several times in a scene or across several scenes, such as scenery, items or NPC's.

Almost all prefabs in unitystation are (or eventually will be) [**prefab variants**](https://docs.unity3d.com/Manual/PrefabVariants.html), which means they get their default settings / behavior from a parent prefab and have some of their own modifications on top of that.
If you are familiar with DM, these work pretty much the same way as DM inheritance does.

Any time the prefab variant's parents are changed, the variant will automatically update with those changes unless it has overridden the particular change with its own setting.

Prefab variants are ESSENTIAL to unitystation because there are lots of objects and items in the game. If we had to edit each one individually every time there was a change to the way certain "kinds" of objects should function, it would be a huge time sink. They also naturally fit into the same structure as the DM inheritance in tgstation.

For example, in the past we have had to make changes to how lockers work, which required manually going in and modifying the 30+ different locker prefabs that had been created. If we had instead set up lockers to be prefab variants of a common "Locker" parent, we would only have to modify the parent prefab and all of the 30+ variants would automatically receive these new changes. This can get very tedious and demotivating, so let's try to make sure we never have to do these sorts of things ever again.

To create a prefab, you first need to determine the best parent prefab to use. You shouldn't create a parentless prefab unless you really know what you're doing.

### Picking the Best Parent Prefab

This is a bit of an art. The goal here is to pick a parent that  

1. Has the stuff your prefab needs already, no more and no less.
2. Logically makes sense as a child, so if the parent's configuration is updated you would want your prefab to gain those updates as well.


Tips for how to find the best parent prefab:  

1. Try to match the hier structure in the tgstation DM codebase. Each node in the hier string should roughly match up with a node in the prefab variant hierarchy. For example, plasteel tiles have a hier of /obj/item/stack/tile/plasteel, so the best place in the prefab variant hierarchy for this object would be something like Item.prefab > Stackable.prefab > Tile.prefab > Plasteel.prefab.
1. Find an existing prefab that is similar to what you want to create, then...
   1. Go to the top of the prefab to see its parent. If the parent makes sense for your prefab, you can create your prefab as a variant of the parent or simply duplicate the similar prefab and rename the duplicate.
      * If the parent doesn't make sense as a parent for your prefab, continue up the hierarchy, check its parent, its parent's parent, etc...until you find a good fit.
   1. If your prefab is actually a special case of this existing prefab, use this existing prefab as the parent!
1. If unsure, the safest option is to use one of the base prefabs:
   * One of the "Clothing" prefabs, such as Beltwear, Earwear, Eyewear, etc... if your prefab will be something wearable.
   * Item if your prefab can be picked up
   * Object if your prefab can't be picked up

It's usually best to err on the side of having a more "basic" parent. Don't choose a parent just because it has what you want. Keep in mind, **if someone changes the parent, those changes will be applied to your variant**!

However, if you create a lot of prefabs under a top-level parent (like Item), **it can make certain changes very tedious because none of them share any configuration**! Therefore it's best to try to structure your prefabs
so that similar prefabs have the same parent. 

If you find yourself in a situation where you need to edit a lot of prefabs all together, you should consider restructuring the prefabs to share a common parent or creating a shared
configuration in the form of a [ScriptableObject (advanced topic, not covered here)](https://docs.unity3d.com/Manual/class-ScriptableObject.html). If you want to learn more about ScriptableObjects, [here's the video that got us all excited about them](https://youtu.be/raQ3iHhE_Kk).

For example, I want to create a new belt. I found the "Toolbelt", see that its parent is "Belt", so I create a variant of that as demonstrated below.
![New Belt](https://i.imgur.com/B2AVrBl.gif)

Here's a different example. I want to create a Janitor Belt that is populated with a clown horn. I found the "Janitor Belt", and I know I want mine to be exactly the same as this but with some initial items rather than nothing,
 so I'll create a variant of the Toolbelt as shown below.
![New Belt](https://i.imgur.com/RzVeVsp.gif)

### Where to Put Your New Prefab

Use your best judgement, but in general your new prefab should usually go in the same folder as the other prefab variants of its parent.

Your prefab will need to be somewhere under a folder called "Prefab", otherwise it won't be spawnable in game.

## Clothing
If your item is something that can be worn, it needs to have sprites defined for how it looks on the player. You do this by creating
a clothing data asset. You can set this up like so:

1. Find the corresponding sprite sheet for your item under Textures/clothing.
1. Right click > Create > Scriptable Objects > HeadsetData (for headsets), BeltData (for belts), ContainerData (for backpacks), or ClothData (for everything else).
1. Fill in the fields under Base:
   * Equipped - sprite sheet for how it looks when worn
   * In Hands Left - sprite sheet for how it looks when in left hand 
   * In Hands Right - sprite sheet for how it looks when in right hand
   * Item Icon - icon for this item when it's in inventory or dropped in the world.
1. In your prefab's Clothing component, select this asset you just created.
1. It's usually recommended to create a subfolder to hold your clothing asset and its associated sprites. 
Make sure to move the corresponding json file for the sprite sheet along with the sprite sheet itself.

For reference, see any of the assets created under Textures/clothing, such as Textures/clothing/head/beret/beret.asset

## Non-clothing Hand Sprites
If you're creating an item that isn't clothing, you still need to define what it looks like when in hand. You can leave this blank, but we strongly encourage you to set this up as it will still need to be done eventually.

To do this, go to the SpriteDataHandler component, expand Sprite List, set element 0 to the left hand sprite sheet and element 1 to the right hand sprite sheet **and press SetUpSheet button**.

For reference, see another prefab which already does this, such as the ID card prefab (search "ID t:Prefab").

## Layers
There are 2 kinds of layers - physics layers (used to determine what should collide with each other) and sorting layers (used to determine what should be drawn on top). Order in layer is a tiebreaker for things in the same sorting layer.

When creating a new object, **you must ensure the prefab's root object has the correct physics layer** (top right in the Inspector) based on what that object is. If you know of an object which functions similarly, simply use the same physics layer as it. Otherwise, [refer to the physics layer reference here.](Physics-Layers.md)

Additionally, **you must ensure the Sprite's SpriteRenderer has the correct sorting layer and order in layer**. Sorting layer should generally match the kind of object it is (check other similar objects if you aren't sure), and order in layer should just be an arbitrary value that doesn't match any other objects' order in layer within that sorting layer (otherwise the order will be arbitrary and the objects might flicker between each other). In the future we might make it automatic via a script, so just do your best to pick a random number for order in layer.

## Working with Components
Components define how the prefab will behave. We have a lot of them! If one doesn't suit your needs, you can [develop new functionality in it or even develop your own](Component-Development-Checklist.md)

This section discusses some commonly used components to give you an idea of how to configure them and how components work in general.

**If you aren't sure what a field does, mouse over its name to view its tooltip** (if it has one). We try to be consistent about adding tooltips, but sometimes you might need to open up the component script or ask on Discord to better understand it.
We are constantly working to make our components and prefabs more user-friendly!

### ItemAttributes
This defines the basic characteristics of an item (though it is also used on some objects at the moment).

The component exposes the following fields, they're mostly self explanatory:
* Item Description: A string describing the item.
* Item Name: The name of the item.
* Size: The size of the item.
* Initial Traits: [Read about Traits](https://github.com/unitystation/unitystation/wiki/Item-Traits-System)
* Damage and throw stats, affect throw speed, range and damage in various scenarios

### Integrity
This defines an object's response to damage.

Some fields:
* Armor - an object's protection against different types of damage. Works the same as the DM codebase.
* Resistances - an object's resistances or weaknesses to certain things. Works the same as ResistanceFlags in the DM codebase.
* Heat Resistance - below this temperature object will take no damage from heat exposure.

### ItemStorage
This allows an object to store items. It defines what slots the storage has, what is allowed to fit, and what initial contents it spawns with. See the [Inventory System documentation](Inventory-System.md) for details.

### Pickupable
The **Pickupable** component allows an item to be picked up. Contains some additional server-side functionality for predicting the success or failure of an attempt to grab the item.

### Untouchables
Some components should almost never be messed with! You will find these on almost all parent prefabs so you should never have to
think about them.

#### Network Identity
Don't touch this!

This is an essential component for working with [Mirror Networking](https://mirror-networking.com/docs/)

It ensures that the GameObject has a unique identity (called the Network Instance Id) and is synced across clients and servers, and as such most GameObject used in UnityStation will need a Network Identity component.

The Network Identity component only has two toggleable properties: Server Only and Local Player Authority. 

References:
* https://docs.unity3d.com/Manual/UNet.html
* https://docs.unity3d.com/Manual/class-NetworkIdentity.html
* https://docs.unity3d.com/Manual/UNetConcepts.html
* https://docs.unity3d.com/Manual/UNetAuthority.html
* https://docs.unity3d.com/Manual/UNetUsingHLAPI.html

#### Custom Net Transform
Don't touch this!

This allows items to be moved in various ways, such as being pushed, appear and disappear.

![Custom net transform](https://i.imgur.com/APfCmFX.png)

References:
* https://docs.unity3d.com/Manual/class-NetworkBehaviour.html
* https://docs.unity3d.com/ScriptReference/Networking.NetworkBehaviour.html

## Putting your object in the game
The easiest way to make your object show up is to create it using the Dev Spawner in game (top right, Dev, Dev Spawner, search for your prefab by name).

You can also map your object into the scene. This requires [adding it to the tile palette](Creating-and-Using-Tile-Palettes.md)

Once you've done that, you can [map it into the scene](How-to-Map.md).


### A note about changing a prefab
Prefabs in the scene (prefab instances) will be independent. This means that if you make any changes to it within the scene, no other prefab will be changed.

However, you might want to apply a change to all prefabs in your scene. 

To assist you, the inspector will show an additional three buttons at the top of the inspector when you inspect a prefab: select, revert and apply.
1. Select will select the prefab asset in your assets.
2. Revert will revert any changes to that of the prefab in your assets.
3. Apply will apply any changes to that prefab to all prefabs of the same type.

Any changes made to the asset prefab will also apply to all prefabs instantiated from it.

References:
* https://docs.unity3d.com/Manual/Prefabs.html
* https://docs.unity3d.com/Manual/InstantiatingPrefabs.html
* https://unity3d.com/learn/tutorials/topics/interface-essentials/prefabs-concept-usage

## Testing
Spawn your item and play around with it as client and server.

Don't forget to test the following common sources of issues:
  * Test in multiplayer with at least 1 client and 1 server, trying to use the object as client and as server player.
  * [Test using Respawn](Proper-Spawning-and-Despawning-(Object-Lifecycle).md)
  * Test with round restarts.
  * Test on moving / rotating / rotated-before-joining shuttles.
