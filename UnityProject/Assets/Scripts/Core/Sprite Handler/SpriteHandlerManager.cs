﻿using System;
using System.Collections;
using System.Collections.Generic;
using Messages.Client.SpriteMessages;
using Messages.Server.SpritesMessages;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpriteHandlerManager : NetworkBehaviour
{
	private static SpriteHandlerManager spriteHandlerManager;
	public static SpriteHandlerManager Instance => spriteHandlerManager;

	public static Dictionary<NetworkIdentity, Dictionary<string, SpriteHandler>> PresentSprites =
		new Dictionary<NetworkIdentity, Dictionary<string, SpriteHandler>>();

	public Dictionary<SpriteHandler, SpriteChange> QueueChanges = new Dictionary<SpriteHandler, SpriteChange>();

	public Dictionary<SpriteHandler, SpriteChange> NewClientChanges = new Dictionary<SpriteHandler, SpriteChange>();

	private void Awake()
	{
		if (spriteHandlerManager == null)
		{
			spriteHandlerManager = this;
		}
		else
		{
			Destroy(this);
		}
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnRoundRestart;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnRoundRestart;
	}

	void OnRoundRestart(Scene oldScene, Scene newScene)
	{
		QueueChanges.Clear();
		NewClientChanges.Clear();
		PresentSprites.Clear();
		SpriteUpdateMessage.UnprocessedData.Clear();
	}

	public static void UnRegisterHandler(NetworkIdentity networkIdentity, SpriteHandler spriteHandler)
	{
		if (spriteHandler == null) return;
		if (networkIdentity == null)
		{
			if (spriteHandler?.transform?.parent != null)
			{
				Logger.LogError(" RegisterHandler networkIdentity is null on  > " + spriteHandler.transform.parent.name,
					Category.Sprites);
				return;
			}
			else
			{
				Logger.LogError(" RegisterHandler networkIdentity is null on  ? ",
					Category.Sprites);
			}

		}

		if (PresentSprites.ContainsKey(networkIdentity))
		{
			if (PresentSprites[networkIdentity].ContainsKey(spriteHandler.name))
			{
				PresentSprites[networkIdentity].Remove(spriteHandler.name);
			}
		}
	}


	public static void RegisterHandler(NetworkIdentity networkIdentity, SpriteHandler spriteHandler)
	{
		if (networkIdentity == null)
		{
			if (spriteHandler?.transform?.parent != null)
			{
				Logger.LogError(" RegisterHandler networkIdentity is null on  > " + spriteHandler.transform.parent.name,
					Category.Sprites);
				return;
			}
			else
			{
				Logger.LogError(" RegisterHandler networkIdentity is null on  ? ",
					Category.Sprites);
			}
		}


		if (PresentSprites.ContainsKey(networkIdentity) == false)
		{
			PresentSprites[networkIdentity] = new Dictionary<string, SpriteHandler>();
		}

		if (PresentSprites[networkIdentity].ContainsKey(spriteHandler.name))
		{
			if (PresentSprites[networkIdentity][spriteHandler.name] != spriteHandler)
			{
				Logger.LogError(
					"SpriteHandler has the same name as another SpriteHandler on the game object > " + spriteHandler.name + " On parent > " +
					spriteHandler.transform.parent.name + " with Net ID of " +  networkIdentity.netId , Category.Sprites);
			}
		}

		PresentSprites[networkIdentity][spriteHandler.name] = spriteHandler;
	}

	public void UpdateNewPlayer(NetworkConnection requestedBy)
	{
		SpriteUpdateMessage.SendToSpecified(requestedBy, NewClientChanges);
	}

	public void ClientRequestForceUpdate(List<SpriteHandler> Specifyed ,NetworkConnection requestedBy)
	{
		var Newtem = new Dictionary<SpriteHandler, SpriteHandlerManager.SpriteChange>();
		foreach (var SH in Specifyed)
		{
			Newtem[SH] = NewClientChanges[SH];
		}
		SpriteUpdateMessage.SendToSpecified(requestedBy, Newtem);
	}


	public override void OnStartClient()
	{
		StartCoroutine(WaitForNetInitialisation());
		base.OnStartClient();
	}

	private IEnumerator WaitForNetInitialisation()
	{
		yield return WaitFor.Seconds(3);
		SpriteRequestCurrentStateMessage.Send(this.GetComponent<NetworkIdentity>().netId);
	}

	void LateUpdate()
	{
		UpdateClients();
		MergeUpdates();
	}

	public void UpdateClients()
	{
		if (QueueChanges.Count > 0)
		{
			//Logger.Log(QueueChanges.Count.ToString());
			//32767 Number of management characters
			//Assuming 50 characters per change
			//655.34‬ changes
			//worst-case scenario 600
			//maybe bring down to 500
			SpriteUpdateMessage.SendToAll(QueueChanges);
		}
	}


	public void MergeUpdates()
	{
		foreach (var Change in QueueChanges)
		{
			if (NewClientChanges.ContainsKey(Change.Key))
			{
				NewClientChanges[Change.Key].MergeInto(Change.Value, this);
			}
			else
			{
				NewClientChanges[Change.Key] = Change.Value;
			}
		}

		QueueChanges.Clear();
	}

	//Ignore startingGameObject when calling externally, as its used internally in this functions recursion
	public static NetworkIdentity GetRecursivelyANetworkBehaviour(GameObject gameObject, GameObject startingGameObject = null)
	{
		if (gameObject == null)
		{
			return null;
		}

		var Net = gameObject.GetComponent<NetworkIdentity>();
		if (Net != null)
		{
			return (Net);
		}

		if (startingGameObject == null)
		{
			startingGameObject = gameObject;
		}

		if (gameObject.transform.parent != null)
		{
			return GetRecursivelyANetworkBehaviour(gameObject.transform.parent.gameObject, startingGameObject);
		}

		Logger.LogError($"Was unable to find A NetworkBehaviour for {startingGameObject.ExpensiveName()} Parent: {startingGameObject.transform.parent.OrNull()?.gameObject.ExpensiveName()}" +
		                $"Parent Parent: {startingGameObject.transform.parent.OrNull()?.parent.OrNull()?.gameObject.ExpensiveName()}",
			Category.Sprites);
		return null;
	}

	public static List<SpriteChange> PooledSpriteChange = new List<SpriteChange>();

	public static SpriteChange GetSpriteChange()
	{
		if (PooledSpriteChange.Count > 0)
		{
			var spriteChange = PooledSpriteChange[0];
			PooledSpriteChange.RemoveAt(0);
			spriteChange.Clean();
			return (spriteChange);
		}
		else
		{
			return (new SpriteChange());
		}
	}

	public class SpriteChange
	{
		//SubCatalogue
		public int PresentSpriteSet = -1;
		public int VariantIndex = -1;

		public int CataloguePage = -1;

		//palette
		public bool PushTexture = false;
		public bool Empty = false;
		public bool PushClear = false;
		public bool ClearPalette = false;
		public Color? SetColour = null;
		public List<Color> Palette = null;
		public bool AnimateOnce = false;

		public void Clean()
		{
			PresentSpriteSet = -1;
			VariantIndex = -1;
			CataloguePage = -1;
			PushTexture = false;
			Empty = false;
			PushClear = false;
			ClearPalette = false;
			SetColour = null;
			Palette = null;
			AnimateOnce = false;
		}


		public void MergeInto(SpriteChange spriteChange, bool pool = false)
		{
			if (spriteChange.PresentSpriteSet != -1)
			{
				if (Empty) Empty = false;
				PresentSpriteSet = spriteChange.PresentSpriteSet;
			}

			if (spriteChange.VariantIndex != -1)
			{
				VariantIndex = spriteChange.VariantIndex;
			}

			if (spriteChange.CataloguePage != -1)
			{
				CataloguePage = spriteChange.CataloguePage;
			}

			if (spriteChange.PushTexture)
			{
				if (PushClear) PushClear = false;
				PushTexture = spriteChange.PushTexture;
			}

			if (spriteChange.Empty)
			{
				if (PresentSpriteSet != -1) PresentSpriteSet = -1;
				Empty = spriteChange.Empty;
			}

			if (spriteChange.ClearPalette)
			{
				if (Palette != null) Palette = null;
				ClearPalette = spriteChange.ClearPalette;
			}

			if (spriteChange.PushClear)
			{
				if (PushTexture) PushTexture = false;
				PushClear = spriteChange.PushClear;
			}

			if (spriteChange.AnimateOnce)
			{
				if (AnimateOnce) AnimateOnce = false;
				AnimateOnce = spriteChange.AnimateOnce;
			}

			if (spriteChange.SetColour != null)
			{
				SetColour = spriteChange.SetColour;
			}

			if (spriteChange.Palette != null)
			{
				if (ClearPalette) ClearPalette = false;
				Palette = spriteChange.Palette;
			}

			if (pool)
			{
				PooledSpriteChange.Add(spriteChange);
			}
		}

		public override string ToString()
		{
			var ST = "";
			if (PresentSpriteSet != -1)
			{
				ST = ST + " PresentSpriteSet > " + PresentSpriteSet;
			}

			if (VariantIndex != -1)
			{
				ST = ST + " VariantIndex > " + VariantIndex;
			}

			if (CataloguePage != -1)
			{
				ST = ST + " CataloguePage > " + CataloguePage;
			}

			if (PushTexture)
			{
				ST = ST + " PushTexture > " + PushTexture;
			}

			if (Empty)
			{
				ST = ST + " Empty > " + Empty;
			}

			if (PushClear)
			{
				ST = ST + " PushClear > " + PushClear;
			}

			if (SetColour != null)
			{
				ST = ST + " SetColour > " + SetColour;
			}

			return ST;
		}
	}
}