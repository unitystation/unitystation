using Mirror;
using System.Collections.Generic;
using UnityEngine;
using Objects;
using Systems.Explosions;

namespace Gateway
{
	/// <summary>
	/// Utility class for Transporting objects.
	/// </summary>
	public class TransportUtility : NetworkBehaviour //Would be a regular static class, but Weaver complains if it doesn't inherit NetworkBehaviour
	{
		public static List<GameObject> MaintRoomLocations { get; set; } = new List<GameObject>();

		/// <summary>
		/// <para>Transports a <paramref name="objectPhysics"/> to <paramref name="transportTo"/> without lerping.</para>
		/// <para>Objects pulled by <paramref name="objectPhysics"/> are not transported. To transport pulled objects as well, use <seealso cref="TransportObjectAndPulled(UniversalObjectPhysics, Vector3)"/>.</para>
		/// <para>Supports UniversalObjectPhysics.</para>
		/// </summary>
		/// <param name="objectPhysics">Object to transport to <paramref name="transportTo"/>.</param>
		/// <param name="transportTo">Destination to transport <paramref name="objectPhysics"/> to.</param>
		/// <param name="doTileStep">Whether step interactions should trigger on teleport</param>
		[Server]
		public static void TransportObject(UniversalObjectPhysics objectPhysics, Vector3 transportTo, bool doTileStep = true, float maintRoomChanceModifier = 1)
		{
			if (objectPhysics == null) return; //Don't even bother...

			var dest = transportTo;
			if (SubSceneManager.Instance.IsMaintRooms && DMMath.Prob(0.0025 * maintRoomChanceModifier)) //1 in 400 chance for events like portals. 1 in 4000 for quantum pads and the like.
			{
				dest = MaintRoomLocations.PickRandom().RegisterTile().WorldPositionServer;
			}

			objectPhysics.DisappearFromWorld();
			objectPhysics.AppearAtWorldPositionServer(dest, doStepInteractions: doTileStep);
		}

		/// <summary>
		/// <para>Transports a <paramref name="objectPhysics"/> to <paramref name="transportTo"/> alongside anything it might be pulling without lerping.</para>
		/// <para>Objects pulled by <paramref name="objectPhysics"/> are transported. To not transport  pulled objects as well, use <seealso cref="TransportObject(PushPull, Vector3)"/>.</para>
		/// <para>UniversalObjectPhysics.</para>
		/// </summary>
		/// <param name="objectPhysics">Object to transport to <paramref name="transportTo"/>.</param>
		/// <param name="transportTo">Destination to transport <paramref name="objectPhysics"/> to (worldPos).</param>
		/// <param name="doTileStep">Whether step interactions should trigger on teleport</param>
		[Server]
		public static void TransportObjectAndPulled(UniversalObjectPhysics objectPhysics, Vector3 transportTo,
			bool doTileStep = true, float maintRoomChanceModifier = 1f)
		{
			if (objectPhysics == null) return; //Don't even bother...

			var linkedList = new LinkedList<UniversalObjectPhysics>();

			//Iterate the chain of linkage
			//The list will be used to rebuild the chain of pulling through the teleporter.
			//Ensure that no matter what, if some object in the chain is pulling the original object, the chain is broken there.

			//Start with the start object
			linkedList.AddFirst(objectPhysics);

			//Add all the things it pulls in a chain
			for (var currentObj = objectPhysics; currentObj.Pulling.HasComponent && currentObj.Pulling.Component != objectPhysics; currentObj = currentObj.Pulling.Component)
			{
				linkedList.AddLast(currentObj.Pulling.Component);
			}

			//Each object in the chain needs to be transported first, and re-establish pull later
			for (var node = linkedList.First; node != null; node = node.Next)
			{
				var currentObj = node.Value;
				var previous = node.Previous?.Value;

				//Disconnect pulling to make it not be a problem
				currentObj.PullSet(null, false); //TODO Test without

				//Transport current
				TransportObject(currentObj, transportTo, doTileStep, maintRoomChanceModifier);

				if (previous != null && currentObj.gameObject != null)
				{
					//There was another object before this one, pulling it. Re-establish pulling. (But only if the current object's gameObject is not null)
					previous.CmdPullObject(currentObj.gameObject);
				}

				//TODO: Make pulling acutally continue working across teleporters for clients, not just server
				//TODO: Find a way to make the teleporter the teleport not all be on the same tile.
			}
		}

		public static void TeleportToObject(GameObject objectToTeleport, GameObject objectTeleportedTo,
			Vector3? worldPos = null, bool calibrated = true, bool doTileStep = true)
		{
			//TODO more uncalibrated accidents, e.g turn into fly people, mutate animals? (See IQuantumReaction)

			//Prevent teleporting loops from teleporting connected tracking device
			if (objectToTeleport == objectTeleportedTo) return;

			var hasQuantum = objectToTeleport.TryGetComponent(out IQuantumReaction reaction);

			if (calibrated == false && hasQuantum)
			{
				reaction.OnTeleportStart();
			}

			var newWorldPosition = worldPos ?? objectTeleportedTo.AssumedWorldPosServer();
			var isGhost = false;

			if (objectToTeleport.TryGetComponent<UniversalObjectPhysics>(out var uop) && uop.Intangible == false)
			{
				//Transport objects and players
				TransportUtility.TransportObjectAndPulled(uop, newWorldPosition, doTileStep);
			}

			//Ghosts dont have uop so check for ghost move
			else if (objectToTeleport.TryGetComponent<GhostMove>(out var ghost))
			{
				isGhost = true;
				//TriggerStepInterface is false to stop looping teleports on things like portals
				ghost.ForcePositionClient(newWorldPosition, false);
			}

			if (calibrated == false && hasQuantum)
			{
				reaction.OnTeleportEnd();
			}

			//Dont spark for ghosts :(
			if (isGhost) return;

			SparkUtil.TrySpark(objectTeleportedTo, expose: false);
		}
	}
}
