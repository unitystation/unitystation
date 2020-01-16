Game that relies on information isolation a lot, like ss13, needs to have a proper network system for that.
Player shouldn't know about what happens behind the wall he can't see through, at all. He shouldn't even receive that information. Popular UNet approaches like SyncVar, Command/ClientRPC don't really allow that (sending information on a need-to-know basis). That's where Network Messages come in handy!

There are two types of net messages:
## Client (to server)
"Request doing something, passing some information". 
Create new class (clone InteractMessage) at Scripts/Messages/Client to proceed.

Client: 
> _**I**_ want to pick up object _**x**_ with my _**left hand**_

If you wish to reference some GameObject, you need to make sure that it is networked (has NetworkInstanceId) and pass these netIds as fields in the message. 
They will be turned back into GameObjects in Process() method after WaitFor.

**Process()** method is executed on server upon arrival.

First you should WaitFor all NetInstanceId's. _SentBy_ is always being sent (implicitly).
If there's just one, you can access its GameObject as NetworkObject.
If there's more, use NetworkObjects[index].

Process() method is meant for validating ability to fulfil client's request (using data existing on server) and do requested actions.

Server: 
> Ok, that guy (NetworkObjects[1]) requested to pick up object (NetworkObjects[0]). I'll check their positions if they're not too far away from each other, as I never trust client. If his _left hand_ slot is occupied I'll also ignore that request. If it's all ok I'll add that item to his inventory and send him messages to:
1. Make item disappear from world and
1. Update his inventory slot

You should override Serialize and Deserialize methods for ClientMessages, mentioning base method first.

To send a message just do YourClientMessage.Send(parameters) inside any GameObject with netId

## Server (to client(s))
"Informs clients about something, providing information". 
Create new class (Clone UpdateUIMessage) at Scripts/Messages/Server to proceed

Server:
> You are receiving that information from me, but it doesn't mean that everyone does.

You can use structs here.

**Process()** method is executed on client upon arrival.
Just tell client what to do here, like updating UI/printing chat message/anything clientside to visualise received message.

To send a message just do YourServerMessage.Send(recipient, parameters) or YourServerMessage.SendToAll(parameters) anywhere. It's advisable to send them from [Server]-annotated methods so that client wouldn't attempt to send a server message by accident.

Note that currently server can send messages to one person (using ServerMessage.SendTo), or everybody at once (using ServerMessage.SendToAll)
Network proximity technology is not yet implemented, so if you want to send messages to receivers in limited area (i.e. within a room), just use SendToAll for now and it will be moved later.

You don't need to override serialization for ServerMessages.

____
When you learn how to think messages it becomes easy. This technology is robust and makes client tampering effectively useless.


TODO: Sync and Prediction/Rollback