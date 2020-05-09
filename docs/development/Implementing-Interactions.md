# Implementing Interactions

For info on the Right Click Menu, see [Right Click Menu](Right-Click-Menu.md).

This page describes the interaction system for Unitystation, also called Interaction Framework 2 (IF2), due to being a replacement of the previous approach to interactions.

All of the code lives in Scripts/Input System/InteractionV2 and is also heavily documented if you need further info.

## Overview
Interactions are implemented by way of **Interactable Components**, or **ICs** for short. These are components which implement one or more of the IF2 interfaces defined in InteractionV2/Interfaces.

Additionally, each component can support one or more different **Interaction Types**. These interaction types represent the different sorts of things a user can do to interact with the game.
Here's a brief description of the current interaction types:

  * HandApply - Click something in the game world. The item in the active hand (or empty hand) is applied to the thing that was clicked. Targets a specific object or tile.
  * HandActivate - Triggers when using the Activate (defaults to "Z") key or clicking the item while it is in the active hand.
  * InventoryApply - Triggered by clicking an inventory slot (in which case the active hand will be the from slot) or dragging from one slot to another.
  * AimApply - like hand apply, but does not have a specific targeted object (it simply aims where the mouse is) and can occur at some interval while the mouse is being held down after being clicked in the game world. For things like shooting a semi-auto or automatic weapon, spraying fire extinguisher, etc...
  * MouseDrop - Click and drag a MouseDraggable object in the game world or an item in inventory and release it to drop on something in the game world (not in inventory). Dragging between 2 slots is handled by InventoryApply.
  * PositionalHandApply - like hand apply, but also fires when clicking empty space and also stores (and transmits) the specific position on the object that was clicked - useful for large objects which have different behavior based on where they are clicked (such as tilemaps). This is separate from HandApply so that the length of netmessages can be reduced (the vector of the position is only added to the message for PositionalHandApply but can be excluded for HandApply).
  * TileApply - (special) used only for [tile interactions](#how-can-i-create-tile-interactions).
  
For example, here's a simple IC, HasNetTab, which pops up a console tab (such as the shuttle console) when the component's object is clicked:
```csharp
public class HasNetworkTab : IInteractable<HandApply>
{
	[Tooltip("Network tab to display.")]
	public NetTabType NetTabType = NetTabType.None;

    //This is invoked server side when this component's object is clicked on the client
	public void ServerPerformInteraction(HandApply interaction)
	{
		TabUpdateMessage.Send( interaction.Performer, gameObject, NetTabType, TabAction.Open );
	}
}
```

As shown above, by implementing IInteractable, the interactable component gains a few nice capabilities:
  * Automatic networking - IF2 takes care of informing the server of the interaction. All you need to implement is the server-side logic of the interaction and a way to communicate the result back to the client (if needed).
  * No mouse / keyboard logic - IF2 figures out when your component's interaction logic should be invoked.
  * Interaction info object - the HandApply class contains all the info you should need in order to decide what should happen.
  * Customization - if the default behavior doesn't meet your needs, you can override additional methods to customize how it works. This can be done to implement client side prediction, 
    or to change the circumstances under which the interaction logic should fire (reducing the amount of messages sent to the server).

## Usage and FAQs

The following sections describe IF2 in more detail for various use cases.

### How do I implement an interaction?
If your interaction involves using one object on another, you should first decide which side of the interaction you want the component to live on. For example, if you have 
a machine that starts emitting radiation when you use an Emag on it, you could put the component on the machine or the Emag object.

Once you've decided, create a new component (or modify an existing one on that object) that implements one of the IF2 interfaces, such as IInteractable, ICheckedInteractable, etc... (see InteractionV2/Interfaces) corresponding to the interaction type you want to handle. In most cases, ICheckedInteractable is the best choice (explained later):

```csharp
public class MyInteractableComponent : IInteractable<HandApply>
```

For example, you can implement IInteractable&lt;HandApply> or IInteractable&lt;HandActivate>. You can even implement both if you want your component to support both kinds of interactions.

Now you need to implement the interaction logic. Here's an example interactable component ExplodeWhenWelded, which explodes when a player tries to weld it. This demonstrates all of IF2 interfaces (except IClientInteractable which is a client-side-only one) because IPredictedCheckedInteractable implements all of the IF2 interface methods:
```csharp
public class ExplodeWhenWelded : IPredictedCheckedInteractable<HandApply>
{

  //this method is invoked on the client side before informing the server of the interaction.
  //If it returns false, no message is sent.
  //If it returns true, the message is sent to the server. The server
  //will perform its own checks to decide which interaction should trigger (which may not be this component) 
  //Then this is invoked on the server side, and if 
  //it returns true, the server finally performs the interaction.
  //We don't NEED to implement this method, but by implementing it we can cut down on the amount of messages
  //sent to the server.
  public bool WillInteract(HandApply interaction, NetworkSide side)
  {
    //the Default method defines the "default" behavior for when a HandApply should occur, which currently
	//checks if the player is conscious and standing next to the thing they are clicking.
    if (!DefaultWillInteract.Default(interaction, side)) return false;
	
    //we only want this interaction to happen when a lit welder is used
    var welder = interaction.HandObject != null ? interaction.HandObject.GetComponent<Welder>() : null;
    if (welder == null) return false;
    if (!welder.isOn) return false;
    return true;
  }
  
  //this is invoked when WillInteract returns true on the client side.
  //We can implement this to add client prediction logic to make the game feel more responsive.
  public void ClientPredictInteraction(HandApply interaction)
  {
    //display an explosion effect on this client that has no effect on their actual health
	DisplayExplosion();
  }

  //this is invoked on the server when client requests an interaction
  //but the server's WillInteract method returns false. So the server may need to tell
  // the client their prediction is wrong, depending on how client prediction works for this
  // interaction.
  public void ServerRollbackClient(HandApply interaction)
  {
    //Server should send the client a message or invoke a ClientRpc telling it to
    //undo its prediction or reset its state to sync with the server
    RollbackExplosion();
  }
  
  //invoked when the server recieves the interaction request and WIllinteract returns true
  public void ServerPerformInteraction(HandApply interaction)
  {
    //Server-side trigger the explosion and inform all clients of it
	Explode(interaction.TargetObject);
  }
}
```

An additional note about WillInteract - there are useful util methods you can use in Validations.cs which are designed to be used in WillInteract.

### Can I have multiple Interactable Components on an object?
Yes, and this is encouraged if the interaction logic makes sense as its own component. For example, all objects which can be picked up have a Pickupable component,
yet some objects also have other interactable components for their object-specific interactions. You can also implement multiple IF2 interfaces on a single component.

When there are multiple interactable components on an object for the same interaction type, IF2 checks the components in the reverse order (from the bottom up to the top) 
they appear in the GameObject's component list in Unity (drag to rearrange them). It triggers the first component whose WillInteract method
returns true.

Additionally, if the interaction involves multiple objects (such as using an item on a machine), the components are checked
on the used object first and the target object second. If ANY interactable component's WillInteract method returns true,
that component's interaction logic is triggered and no further components are checked.

To see detailed messages showing the order in which ICs are being checked, in Unity go to Logger > Adjust Log Levels and change Interactions to TRACE. Now any time there is an interaction, you will see log messages showing exactly what IF2 is checking in the order it is checking.

Refer to the section "Precedence of Interaction Components" for the full details.

### How does WillInteract work? My client does not have enough info to implement it?
When WillInteract runs on the clientside and returns true, a message will be sent to the server merely saying that
an interaction should trigger of the indicated type (HandApply, AimApply, etc...). The server will run through the
server-side version of WillInteract for all the possible components (on the used object and then the target object if there is one),
and will trigger the appropriate component regardless of which component triggered on the clientside.

Because of this, if you don't have enough info to decide if a given interaction should occur and don't want
to block another component from triggering, you can simply do as much checking as you can on the clientside and
then return true from WillInteract. Then you can add the appropriate server-side logic to WillInteract so it can properly check
the interaction.

### How do I inform the client what happened?
In ServerPerformInteraction, if the server makes some change to the game state, usually they will need to ensure the client
knows about the new state. This is currently not part of IF2, but there are various ways this can be done, many of which
can be accomplished using already implemented methods...For the most part, you can update a SyncVar, broadcast a net message to all clients or just one,
or invoke a ClientRpc. Refer to the other articles on this wiki which discuss networking for more information.


### I don't need any networking, I just want client-side interaction logic
Sometimes you may not need any of the networking features of Interactable (such as already having Cmds or other messages
that handle it for you), or you have an interaction which only has an effect client-side. In this case, you can 
instead implement IClientInteractable:
```csharp
public class MyClientSideInteraction : MonoBehavior, IClientInteractable<HandApply>
{

  //invoked when this is clicked. 
  //Return value convention is the same as WillInteract, except this is only invoked on the client side.
  //If it returns true, the interaction is "consumed" - no more components will recieve the current interaction.
  //If it returns false, the interaction is "passed on" - additional components will recieve the interaction.
  public bool Interact(HandApply interaction)
  {
     //do something client side, or send a message to the server or invoke a Cmd
	 //if we did something
		return true;
	 //if we didn't
		return false;
  }
}
```

### I implemented an interaction but my component isn't getting triggered, why?
First of all, in Unity go to Logger > Adjust Log Levels and change Interaction to TRACE. Now you will see detailed log messages showing up explaining the order in which ICs are being checked.

There's a few things to check:
1. Does your component implement WillInteract? If so, check if it is returning true when you try to interact. If it returns false, your component's interaction will not be triggered.
  If there is no Willinteract method, take a look at the DefaultWillInteract class to see what default logic
  is being used.
2. Are there any other interactable components on the object or the other object involved in the interaction? Remember that the server-side logic of WillInteract determines which components will trigger. 
The client-side logic only tells the server to check for an interaction. So, use the TRACE logs on the server side to see if the component is being blocked by another.
Some possible fixes:
    * add more server-side logic to another component's WillInteract method to properly avoid the interaction.
	* rearrange the components so your interaction is lower (thus is checked first) 
	* move your interactable component to the other object involved in the interaction
	
### Why should I implement ICheckedInteractable instead of just IInteractable?
You don't HAVE to implement WillInteract - you could just put all of your validation logic and checks in ServerPerformInteraction. This will result in the 
logic in DefaultWillInteract.cs being used for the WillInteract check. This is probably fine for many situations.

However, this can cause some problems:
  * Your component's interaction logic will always be triggered when it recieves an interaction, preventing other interactable components on the object from receiving the interaction.
  * Your component will send interaction messages to the server more often, even when the ServerPerformInteraction logic would not end up doing anything. This increases the network 
    load on the server.

Instead, if you implement ICheckedInteractable.WillInteract so that it only returns true when your interaction logic acually has something to do, then you can avoid those problems and improve network performance.

### How can I implement client side prediction and rollback?
Client side prediction makes the game appear a lot more responsive to the user, even if they are having to wait for the server
to process their action. Basically, the client predicts what the interaction will do and updates the game state accordingly for the user. However, if it later turns
out that the server disagrees with that prediction, the client needs to "roll back" the prediction, resetting its state to whatever the server says is correct.

To implement this, simply implement IPredictedInteractable or IPredictedCheckedInteractable.

For client side prediction, you will implement ClientPredictInteraction. This is invoked when the client is sending the message to the server after WillInteract returns true.
In that method, you can make your prediction and update the game state for the local client.

For rollback, you will implement ServerRollbackClient. In this method, you can inform the client (sending a net message, updating a SyncVar, invoking ClientRpc, etc...) what to roll back to.

### How can I trigger interactions manually (such as for right click options)?
Simply call InteractionUtils.RequestInteract, providing the details of the interaction and the component you want to trigger. 

### How can I create tile interactions?
Tile interactions can't be done using interactable components, because tiles are a separate kind of thing in Unity.

Tile interactions are defined by creating Scriptable Objects which subclass TileInteraction. Some existing ones are already defined which may suit your needs (see Create > Interaction > Tile Interaction). If the existing scriptable objects don't support your needs, you can create a new subclass of TileInteraction following the example of the existing TileInteraction subclasses.

To indicate that a given tile can have a particular interaction, you should locate that tile's asset (somewhere in Tilemaps/Resources/Tiles), and add your tile interaction asset to its Tile Interactions list. Interactions will be checked from top to bottom in the interactions list until one (if any) fires. As with interactable components, the client / server WillInteract logic works the same way - if any WIllInteract returns true on the client-side, the server will check all possible interactions on that tile using the server-side WillInteract logic, regardless of
which interaction triggered on the clientside.

For a thorough example of tile interactions, refer to the tile assets for ReinforcedWall and RWall in Tiles/Walls and examine their TileInteraction lists, and look at their TileInteraction assets. These define the logic for constructing and deconstructing reinforced walls (which are eventually turned into reinforced girders when deconstruction progresses far enough, which are actual prefabs instead of tiles).

## Reference
The remaining sections server as a reference for the details of IF2.

### Interaction Types
Here are the current interactions. More may be added as different objects require different use cases:
* MouseDrop - Click and drag a MouseDraggable object in the world or an item in inventory and release it to drop on something in the world (not in inventory). Dragging between 2 slots is handled by InventoryApply.
* HandApply - click something in the game world. The item in the active hand (or empty hand) is applied to the thing that was clicked. Targets a specific object or tile.
* PositionalHandApply - like hand apply, but also fires on clicking empty space and stores (and transmits) the specific position on the object that was clicked - useful for large objects which have different behavior based on where they are clicked (such as tilemaps). This is separate from HandApply so that the length of netmessages can be reduced (the vector of the position is only added to the message for PositionalHandApply but can be excluded for HandApply).
* AimApply - like hand apply, but does not have a specific targeted object (it simply aims where the mouse is) and can occur at some interval while the mouse is being held down after being clicked in the game world. For things like shooting a semi-auto or automatic weapon, spraying fire extinguisher, etc...
* HandActivate - Triggers when using the "Z" key or clicking the item while it is in the active hand.
* InventoryApply - Triggered by clicking an inventory slot (in which case the active hand will be the from slot) or dragging from one slot to another.

### Precedence of Interaction Components
This list indicates the current order of precedence for checking for an interaction on a given frame. Consider this an "abridged version" of the next section.

Remember, you can always turn on TRACE level logging for the Interaction category to see a detailed log of each component in the order it is being checked during an interaction.

Remember that there can be multiple components on the used object or the targeted object which implement IInteractable&lt;>, for multiple interaction types, so this list can help you figure out which will be invoked first. Further checking of interactions will be stopped as soon as any of these components indicates that an interaction has occurred.

1. alt click
2. throw
3. HandApply + PositionalHandApply
    1. Components on used object (for the object in the active hand, if occupied), in reverse component order.
    2. Components on target object in reverse component order.
5. AimApply (this runs last so you can still melee / click things if adjacent when a gun is in hand)
    1. Components on used object (object in the active hand), in reverse component order.

### Interaction Logic Flow
Because the mouse can do so many things, the logic for interactions is a bit complicated. This section describes it in detail.

Due to wanting to make guns more usable, there are 2 main different cases - when you have a loaded gun in the active hand vs. not.

Alt click and throw are always checked first and have no special logic.

When the active hand doesn't have a loaded gun:  

1. Mouse Clicked Down
    1. Is the mouse over an object with a MouseDraggable? We need to wait and see if we should drag it or click on it. 
       Save the MouseDraggable and wait until mouse is dragged or mouse 
       button is released.
    2. If mouse is not over a MouseDraggable...
        1. IF2 - HandApply and PositionalHandApply - check interactions in the following order until one occurs.
            1. IInteractable&lt;HandApply or PositionalHandApply> components on used object (for the object in the active hand, if occupied), in reverse
               component order.
            2. IInteractable&lt;HandApply or PositionalHandApply> components on target object in reverse component order.
        3. If no interactions have occurred, check IF2 AimApply interactions and stop as soon as one occurs. This runs 
           last so you can still melee / click things if adjacent when a gun is in hand)
           1. Checks for IInteractable&lt;AimApply> components on used object (object in the active hand), in reverse component 
              order.
2. Mouse held down.
    1. If we saved a MouseDraggable during the initial click and the mouse has been dragged far enough (past MouseDragDeadzone), initiate a drag and drop (show the drag shadow of the object being dragged).
       Until the object is dropped, no further interactions will occur.
3. Mouse Button Released
    1. If we are dragging something, drop it and trigger MouseDrop interactions in the following order... 
        1. IInteractable&lt;MouseDrop> components on dropped object in reverse
               component order.
        2. IInteractable&lt;MouseDrop > components on target object in reverse component order.
    2. If we saved a MouseDraggable during the initial click but the mouse never moved past the drag deadzone
       and we have not held the mouse button down longer than MaxClickDuration...
        1. IF2 - HandApply and PositionalHandApply - check interactions in the following order until one occurs.
            1. IInteractable&lt;HandApply or PositionalHandApply > components on used object (for the object in the active hand, if occupied), in reverse 
               component order.
            2. IInteractable&lt;HandApply or PositionalHandApply > components on target object in reverse component order.
        2. If no HandApply or PositionalHandApply  interactions occurred, check the old system to see if a click interaction occurs - uses 
           InputTrigger and stop as soon as one occurs.


When there is a loaded gun in the active hand.  

1. Mouse Clicked Down
    1. Are we on Harm intent? If so, shoot (trigger IInteractable&lt;AimApply> components on Gun).
    2. If not on Harm intent...
        1. IF2 - HandApply and PositionalHandApply - check interactions in the following order until one occurs.
            1. IInteractable&lt;HandApply or PositionalHandApply> components on used object (for the object in the active hand, if occupied), in reverse 
               component order.
            2. IInteractable&lt;HandApply or PositionalHandApply> components on target object in reverse component order.
        2. If no HandApply interactions occurred, check the old system to see if a click interaction occurs - uses 
           InputTrigger and stop as soon as one occurs.
        3. If no interactions have occurred, check IF2 AimApply interactions and stop as soon as one occurs. This runs 
           last so you can still melee / click things if adjacent when a gun is in hand)
           1. Checks for IInteractable&lt;AimApply> components on used object (object in the active hand), in reverse component 
              order.
2. Mouse held down - continue shooting if we have an automatic (keep triggering IInteractable&lt;AimApply> components on Gun).
