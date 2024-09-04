using Logs;
using Mirror;
using Objects;
using Util.Independent.FluentRichText;
using UnityEngine;

namespace Core
{
	public partial class UniversalObjectPhysics
	{
		private ObjectContainer cachedContainedInContainer;

		public ObjectContainer ContainedInObjectContainer
		{
			get
			{
				if (parentContainer is not (NetId.Invalid or NetId.Empty) && (cachedContainedInContainer == null ||
				                                                              cachedContainedInContainer.registerTile
					                                                              .netId != parentContainer))
				{
					var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
					if (spawned.TryGetValue(parentContainer, out var net))
					{
						cachedContainedInContainer = net.GetComponent<ObjectContainer>();
					}
					else
					{
						cachedContainedInContainer = null;
					}
				}

				if (parentContainer is (NetId.Invalid or NetId.Empty))
				{
					return null;
				}

				return cachedContainedInContainer;
			}
		}

		public GameObject GetRootObject
		{
			get
			{
				if (ContainedInObjectContainer != null)
				{
					return ContainedInObjectContainer.registerTile.ObjectPhysics.Component.GetRootObject;
				}
				else if (pickupable.HasComponent && pickupable.Component.StoredInItemStorageNetworked != null)
				{
					return pickupable.Component.StoredInItemStorageNetworked.GetRootGameObject();
				}
				else
				{
					return gameObject;
				}
			}
		}

		public void StoreTo(ObjectContainer newParent)
		{
			//TODO Sometime Handle stuff like cart riding
			//would be basically just saying with updates with an offset to the thing that parented to the object
			if (newParent.OrNull()?.gameObject == this.gameObject)
			{
				global::Chat.AddGameWideSystemMsgToChat(
					$"Anomoly Detected.. {gameObject.ExpensiveName()} has attempted to store itself within itself"
						.Color(Color.red));
				Loggy.LogError("Tried to store object within itself");
				return; //Storing something inside of itself, what?
			}

			PullSet(null, false); //Presume you can't Pulling stuff inside container
			//TODO Handle non-disappearing containers like Cart riding
			parentContainer = newParent == null ? NetId.Empty : newParent.registerTile.netId;
			cachedContainedInContainer = newParent;
			SynchroniseVisibility(isVisible, newParent == null);
		}
	}
}