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
					spriteHandler.transform.parent.name, Category.SpriteHandler);
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

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.LATE_UPDATE, LateUpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.LATE_UPDATE, LateUpdateMe);
	}

	void LateUpdateMe()
	{
		UpdateClients();
		MergeUpdates();
	}

	public void UpdateClients()
	{
		Logger.Log(QueueChanges.Count.ToString());

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
		public Color? SetColour = null;

		public void Clean()
		{
			PresentSpriteSet = 0;
			VariantIndex = -1;
			CataloguePage = -1;
			PushTexture = false;
			Empty = false;
			PushClear = false;
			SetColour = null;
		}


		public void MergeInto(SpriteChange spriteChange, bool pool = false)
		{
			if (spriteChange.PresentSpriteSet != -1)
			{
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
				PushTexture = spriteChange.PushTexture;
			}

			if (spriteChange.Empty)
			{
				Empty = spriteChange.Empty;
			}

			if (spriteChange.PushClear)
			{
				PushClear = spriteChange.PushClear;
			}

			if (spriteChange.SetColour != null)
			{
				SetColour = spriteChange.SetColour;
			}

			if (pool)
			{
				PooledSpriteChange.Add(spriteChange);
			}
		}
	}
}