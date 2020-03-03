# How to use UIActions

so what you need,
A I want my action to be linked to a component
simply inherit from IActionGUI, implement the implement the interface requirements, 
when you want to show it you can manually turn it on through a command
```csharp
UIActionManager.Instance.SetAction(IActionGUI, true);
```
IActionGUI is the component, the second parameter is whether or not to show it
note that this only works for client

B 
I wanted to be static
simply make a new scriptable object inheriting from UIActionScriptableObject,
the same can be done client side for turning it off and on

Networking?

For networking you will require the UIActionScriptableObject since it only has the server interface implemented, or

in your component inheriting from IServerActionGUI,

To implement the command that will be called on server on the component, 
note that you will have to do validation since trying client can't be trusted,

NetworkIdentity GetNetworkIdentity() requires a NetworkIdentity  in the same  
game object or parents that doesn't change across server or client,

Since it uses different

For activating a UI action on the client, noting that it has to be a IServerActionGUI UIinteraction,  will be compatible with UIActionScriptableObject, derived stuff as well
from the server use  
```csharp
SetActionUI.SetAction(player, IServerActionGUI , bool)
```
bool = what state it should be in shown or not shown

