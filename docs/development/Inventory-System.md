# Inventory System

This page describes the new inventory system. This system is designed to be a general purpose way to allow ANY object to store one or more items. That includes players, backpacks, fire cabinets, paper bins, and even consoles which accept an ID card as input. There should be no more need to implement custom "storage" logic for each object - they can just hook into the inventory system, and immediately gain the functionality provided by the system.

More detailed information is available in the various inventory system classes and comments. 

If you have any questions, difficulties with using the system, or encounter any bugs, please reach out to @chairbender on Discord. This is a new system, so we'd like to address any
usability concerns you might have as soon as possible.


## Inventory System Overview
An object gains the ability to store items by attaching an `ItemStorage` component. An ItemStorage provides one or more `ItemSlot`s, accessible via the ItemStorage API. `ItemSlot` can store one item or be empty. 
The `ItemStorage` component has 3 critical editor fields:

* Storage Structure - (required) defines which slots are available in this object. It can have 0 or more **indexed slots** (slots which are identified by a number, starting from 0) and **named slots** (identified by a NamedSlot enum value).
These SOs (Scriptable Objects) currently live in Resources/ScriptableObjects/Inventory/Structure
* Storage Capacity - (required) defines what is allowed to fit in the various slots of this object.  
* Storage Populator - (optional) defines how the slots should be populated when the item spawns.


### Storage Capacity
Storage capacity is any ScriptableObject which subclasses `ItemStorageCapacity`. For example, `DefinedStorageCapacity` defines which kinds of items can fit in each slot, and SlotCapacity provides a simple global rule for what can fit in any slot on the object. If these aren't enough, you can simply implement your own subclass. The currently capacity classes allow you to define capacity based on the item's size and/or via a combination of whitelisted, blacklisted, and required `ItemTrait`s (defined on each prefab via the ItemAttributes component). For more info, refer to the wiki page on the [Trait system](Item-Traits-System.md).

These SOs live in Resources/ScriptableObjects/Inventory/Structure


### Storage Populator
Storage populator is any ScriptableObject which subclasses `ItemStoragePopulator`, which defines how to populate an ItemStorage. These are used to define the prefabs that should go in each slot. For example, `NamedSlotStoragePopulator` defines which prefab go in each named slot. As another example, the `AutoOccupationStoragePopulator` populates a player character's storage when they spawn based on the player's occupation (and each occupation's storage is defined using a NamedSlotStoragePopulator, in addition
 to a standard populator used across all occupations).

Storage Populators can also be used in other situations, not just when an object spawns. Simply call the `ItemStorage.ServerPopulate` method and provide the instance of `ItemStoragePopulator` that should be used.

These SOs currently live in Resources/ScriptableObjects/Inventory/Populators.

### Visibility
The server knows all of the contents of every slot at all times. However, each client only knows the contents of the slots that the server has informed
them about. Each `ItemSlot` has a list of observer players which will recieve updates when the slot's contents change on the server. Generally,
a player is always an observer of every slot in their inventory or within any ItemStorage carried in their inventory. 

Using the `ItemSlot.SererAddObserverPlayer` and `ItemSlot.ServerRemoveObserverPlayer`, a client can be added/removed as an observer for a particular item slot.
This can be used if the client should be allowed to look into the contents of an object.

### UI Slots
The UI provides a number of objects with UI_ItemSlot components. These provide a view into a particular ItemSlot (set using `UI_ItemSlot.LinkSlot`). The image
displayed in the slot is based on the object's current sprite, and can be refreshed using `UI_ItemSlot.RefreshImage`, or manually set to a particular image
using `UI_ItemSlot.UpdateImage`. For convenience, you can also use `Pickupable.RefreshUISlotImage` and other methods to update the UI slot image
for an object via the `Pickupable` component of an object.

### Logging

The inventory system provides very detailed logging describing everything that is happening during population, inventory movement, etc... Just turn on TRACE level logging for the Inventory category to 
see these detailed messages.


## Usage and FAQs

The following sections describe the inventory system in more detail for various use cases.


### How do I move stuff into / out of / between slots?

The `Inventory` class provides static methods for performing all possible inventory operations. Refer to the method documentation there for details. Briefly, there are 3 main kinds of inventory movements:
  * Add - item isn't in any slots, and will be added to a slot.
  * Remove - item is currently in a slot, and will be removed from that slot (can be dropped, despawned, or even thrown)
  * Transfer - item is in one slot and will be moved directly to another slot


### How do I check on the current status of things in inventory?

An `ItemSlot` has everything that is possible to know about a particularl item slot. You can obtain these directly using the
static factory methods on `ItemSlot`, but it's usually more convenient to access ItemSlots using the methods on `ItemStorage` and `Pickupable` components.

The `ItemStorage` component on an object provides a way to access the item slots of that object.

The `Pickupable` component on an object lets you see the slot that object is currently in (if it's in a slot).

Both components also provide various utility methods for performing other common tasks relating to inventory.


### How can I know where an object is in the world when it's in a slot?

Use our `GameObject.AssumedWorldPositionServer` extension or `ObjectBehaviour.AssumedWorldPositionServer` - this will return the correct position of the object regardless of if it's stored in something. If you
have any sort of position-based logic which should still function when the object is in inventory, you should use this. Any other means of getting the position may return HiddenPos (off in lala land) or
an inaccurate position.


### How can I react to an object being moved in inventory?

Simply have your component implement `IServerInventoryMove` for server side logic or `IClientInventoryMove` for client side logic.
The hook methods will be called when your component's object is moved into, out of, or within the inventory system. Note that the
client-side inventory movement methods are only invoked for movements which the client is aware of (generally, only the local player's 
own inventory and anything they are currently looking into).


### How can I react to the contents of a particular ItemSlot changing?

You can subscribe to the events `ItemSlot.OnSlotContentsChangeServer` and `ItemSlot.OnSlotContentsChangeClient`. These will be fired
whenever the contents of that item slot changes. Note that the client-side version is only called for slots that the client
is aware of (generally, only the local player's own inventory and anything they are currently looking into).


### How do I update the sprite shown in the UI slots?

If your sprite has changed and you want to reflect that change in the UI, simply get the `Pickupable` component of your object
and call `Pickupable.RefreshUISlotImage`. This will automatically update the sprite based on your object's current sprite. It's safe
to call this even if your object is not currently in a slot, so go ahead and just call this whenever your object's sprite changes.

### How do I create instances of these various Scriptable Objects?

In the Project pane in Unity, right click and go to Create > Inventory to see all the currently defined SOs relating to inventory.

### My inventory movement isn't working, why?

Turn the Inventory logging level up to TRACE to see detailed explanations of everything happening in the inventory system, including
why a particular movement has been rejected.

Some common things to check:
  * What storage capacity / slot capacity is being used for the slot you are trying to move the item into?
  * Are the item's ItemTraits properly defined in its ItemAttributes component?
  * Does the item storage you are trying to move the item into actually have the slot?



