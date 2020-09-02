using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Mirror;
using UnityEngine;

public class SpriteHandlerManager : NetworkBehaviour
{
	//public static int AvailableID = 1;

	private static SpriteHandlerManager spriteHandlerManager;
	public static SpriteHandlerManager Instance
	{
		get
		{
			if (!spriteHandlerManager)
			{
				spriteHandlerManager = FindObjectOfType<SpriteHandlerManager>();
			}

			return spriteHandlerManager;
		}
	}



	public Dictionary<NetworkIdentity, Dictionary<string, SpriteHandler>> PresentSprites =
		new Dictionary<NetworkIdentity, Dictionary<string, SpriteHandler>>();

	public Dictionary<SpriteHandler, SpriteChange> QueueChanges = new Dictionary<SpriteHandler, SpriteChange>();

	public Dictionary<SpriteHandler, SpriteChange> NewClientChanges = new Dictionary<SpriteHandler, SpriteChange>();

	public static void RegisterHandler(NetworkIdentity networkIdentity, SpriteHandler spriteHandler)
	{
		if (networkIdentity == null)
		{
			Logger.LogError(" RegisterHandler networkIdentity is null on  > " + spriteHandler.transform.parent.name,
				Category.SpriteHandler);
			return;
		}


		if (Instance.PresentSprites.ContainsKey(networkIdentity) == false)
		{
			Instance.PresentSprites[networkIdentity] = new Dictionary<string, SpriteHandler>();
		}

		if (Instance.PresentSprites[networkIdentity].ContainsKey(spriteHandler.name))
		{
			if (Instance.PresentSprites[networkIdentity][spriteHandler.name] != spriteHandler)
			{
				Logger.LogError(
					"SpriteHandler has the same name as another SpriteHandler on the game object > " +
					spriteHandler.transform.parent.name + " with Net ID of " +  networkIdentity.netId , Category.SpriteHandler);
			}
		}

		Instance.PresentSprites[networkIdentity][spriteHandler.name] = spriteHandler;
	}


	// Start is called before the first frame update
	void Start()
	{
		//AvailableID = 1;
		//foreach (var Product in SpriteCatalogue.Instance.Catalogue)
		//{
			//Product.ID = AvailableID;
			//AvailableID++;
		//}
	}

	public void UpdateNewPlayer(NetworkConnection requestedBy)
	{
		SpriteUpdateMessage.SendToSpecified(requestedBy, NewClientChanges);
	}

	public override void OnStartClient()
	{

		base.OnStartClient();
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

	public static NetworkBehaviour GetRecursivelyANetworkBehaviour(GameObject gameObject)
	{
		if (gameObject == null)
		{
			return null;
		}
		var Net = gameObject.GetComponent<NetworkBehaviour>();
		if (Net != null)
		{
			return (Net);
		}
		else if (gameObject.transform.parent != null)
		{
			return GetRecursivelyANetworkBehaviour(gameObject.transform.parent.gameObject);
		}
		else
		{
			Logger.LogError("Was unable to find A NetworkBehaviour for? yeah Youll have to look at this stack trace",
				Category.SpriteHandler);
			return null;
		}
	}


	// Update is called once per frame
	void Update()
	{
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
		public bool ClearPallet = false;
		public Color? SetColour = null;
		public List<Color> Pallet = null;

		public void Clean()
		{
			PresentSpriteSet = -1;
			VariantIndex = -1;
			CataloguePage = -1;
			PushTexture = false;
			Empty = false;
			PushClear = false;
			ClearPallet = false;
			SetColour = null;
			Pallet = null;
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

			if (spriteChange.ClearPallet)
			{
				if (Pallet != null) Pallet = null;
				ClearPallet = spriteChange.ClearPallet;
			}

			if (spriteChange.PushClear)
			{
				if (PushTexture) PushTexture = false;
				PushClear = spriteChange.PushClear;
			}

			if (spriteChange.SetColour != null)
			{
				SetColour = spriteChange.SetColour;
			}

			if (spriteChange.Pallet != null)
			{
				if (ClearPallet) ClearPallet = false;
				Pallet = spriteChange.Pallet;
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