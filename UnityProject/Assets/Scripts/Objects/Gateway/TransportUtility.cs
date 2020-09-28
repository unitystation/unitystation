using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Gateway
{
	/// <summary>
	/// Utility class for Transporting objects.
	/// </summary>
	public class TransportUtility : NetworkBehaviour //Would be a regular static class, but Weaver complains if it doesn't inherit NetworkBehaviour
	{
		/// <summary>
		/// <para>Transports a <paramref name="pushPullObject"/> to <paramref name="transportTo"/> without lerping.</para>
		/// <para>Objects pulled by <paramref name="pushPullObject"/> are not transported. To transport pulled objects as well, use <seealso cref="TransportObjectAndPulled(PushPull, Vector3)"/>.</para>
		/// <para>Supports PlayerSync and CustomNetTransform.</para>
		/// </summary>
		/// <param name="pushPullObject">Object to transport to <paramref name="transportTo"/>.</param>
		/// <param name="transportTo">Destination to transport <paramref name="pushPullObject"/> to.</param>
		[Server]
		public static void TransportObject(PushPull pushPullObject, Vector3 transportTo)
		{
			if (pushPullObject == null)
				return; //Don't even bother...

			//Handle PlayerSync and CustomNetTransform (No shared base class, so terrible duping time)

			//Player objects get PlayerSync
			var playerSync = pushPullObject.GetComponent<PlayerSync>();
			if (playerSync != null)
			{
				playerSync.DisappearFromWorldServer();
				playerSync.AppearAtPositionServer(transportTo);
				playerSync.RollbackPrediction();
			}
			//Object and Item objects get CustomNetTransform
			var customNetTransform = pushPullObject.GetComponent<CustomNetTransform>();
			if (customNetTransform != null)
			{
				customNetTransform.DisappearFromWorldServer();
				customNetTransform.AppearAtPositionServer(transportTo);
				customNetTransform.RollbackPrediction();
			}
		}

		/// <summary>
		/// <para>Transports a <paramref name="pushPullObject"/> to <paramref name="transportTo"/> alongside anything it might be pulling without lerping.</para>
		/// <para>Objects pulled by <paramref name="pushPullObject"/> are transported. To not transport  pulled objects as well, use <seealso cref="TransportObject(PushPull, Vector3)"/>.</para>
		/// <para>Supports PlayerSync and CustomNetTransform.</para>
		/// </summary>
		/// <param name="pushPullObject">Object to transport to <paramref name="transportTo"/>.</param>
		/// <param name="transportTo">Destination to transport <paramref name="pushPullObject"/> to.</param>
		[Server]
		public static void TransportObjectAndPulled(PushPull pushPullObject, Vector3 transportTo)
		{
			if (pushPullObject == null)
				return; //Don't even bother...

			var linkedList = new LinkedList<PushPull>();

			//Iterate the chain of linkage
			//The list will be used to rebuild the chain of pulling through the teleporter.
			//Ensure that no matter what, if some object in the chain is pulling the original object, the chain is broken there.

			//Start with the start object
			linkedList.AddFirst(pushPullObject);

			//Add all the things it pulls in a chain
			for (var currentObj = pushPullObject; currentObj.IsPullingSomething && currentObj.PulledObject != pushPullObject; currentObj = currentObj.PulledObject)
			{
				linkedList.AddLast(currentObj.PulledObject);
			}

			//Each object in the chain needs to be transported first, and re-establish pull later
			for (var node = linkedList.First; node != null; node = node.Next)
			{
				var currentObj = node.Value;
				var previous = node.Previous?.Value;

				//Disconnect pulling to make it not be a problem
				currentObj.CmdStopPulling();

				//Transport current
				TransportObject(currentObj, transportTo);

				if (previous != null && currentObj.gameObject != null)
				{
					//There was another object before this one, pulling it. Re-establish pulling. (But only if the current object's gameObject is not null)
					previous.CmdPullObject(currentObj.gameObject);
				}

				//TODO: Make pulling acutally continue working across teleporters for clients, not just server
				//TODO: Find a way to make the teleporter the teleport not all be on the same tile.
			}
		}
	}

}