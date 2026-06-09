using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Core.Sprite_Handler;
using US13.Managers.NetworkManagement;
using US13.Messages.Server.SpritesMessages;

public class SpriteHandlerItemReplicatorNet : NetworkBehaviour
{

	public GameObject Container;

	public SpriteHandler SpriteHandlerPrefab;

	public readonly SyncList<string> SynchronisedHandlers = new SyncList<string>();

	public readonly List<SpriteHandler> LocalHandlers = new List<SpriteHandler>();

	public GameObject TrackingObject; //TODO Update depending on what the object is doing, For now just take a snapshot

	public void Awake()
	{
		if (CustomNetworkManager.IsServer == false)
		{
			SynchronisedHandlers.OnAdd += OnItemAdded;
			SynchronisedHandlers.OnRemove += OnItemRemoved;
			SynchronisedHandlers.OnClear += OnListCleared;

			// Run add logic for items that already exist on spawn
			for (int i = 0; i < SynchronisedHandlers.Count; i++)
			{
				OnItemAdded(i);
			}
		}
	}

	public override void OnStopClient()
	{
		SynchronisedHandlers.OnAdd -= OnItemAdded;
		SynchronisedHandlers.OnRemove -= OnItemRemoved;
		SynchronisedHandlers.OnClear -= OnListCleared;
	}

	void OnItemAdded(int index)
	{
		string item = SynchronisedHandlers[index];
		OnItemAddedString(item);
		// Your code here
		Debug.Log($"ADD {item} at {index}");
	}

	SpriteHandler OnItemAddedString(string value)
	{
		var Handler = Instantiate(SpriteHandlerPrefab, Container.transform);
		SpriteHandlerManager.UnRegisterHandler(this.netIdentity, Handler);
		Handler.name = value;
		SpriteHandlerManager.RegisterHandler(this.netIdentity, Handler);
		LocalHandlers.Add(Handler);

		return  Handler;
	}

	void OnItemRemoved(int index, string oldItem)
	{
		for (int i = LocalHandlers.Count - 1; i >= 0; i--)
		{
			var handler = LocalHandlers[i];
			if (handler.name == oldItem)
			{
				LocalHandlers.Remove(handler);
				SpriteHandlerManager.UnRegisterHandler(this.netIdentity, handler);
				Destroy(handler.gameObject);
			}
		}
	}

	void OnListCleared()
	{
		for (int i = LocalHandlers.Count - 1; i >= 0; i--)
		{
			var handler = LocalHandlers[i];
			LocalHandlers.Remove(handler);
			SpriteHandlerManager.UnRegisterHandler(this.netIdentity, handler);
			Destroy(handler.gameObject);
		}
	}


	public void TrackItem(GameObject item)
	{
		TrackingObject = item;
		if (TrackingObject != null)
		{
			var Handlers = TrackingObject.GetComponentsInChildren<SpriteHandler>();
			foreach (var Handler in Handlers)
			{
				if (Handler.enabled == false || Handler.gameObject.activeInHierarchy == false) continue;
				if (Handler.CurrentSprite == null) continue;
				var  Newhandler = OnItemAddedString(Handler.name);
				SynchronisedHandlers.Add(Handler.name);
				Newhandler.SetSpriteSO(Handler.PresentSpritesSet);
			}
		}
		else
		{
			SynchronisedHandlers.Clear();
			OnListCleared();
		}

	}

}
