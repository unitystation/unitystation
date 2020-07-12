using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SpriteHandlerManager : NetworkBehaviour
{
	public static uint AvailableID = 1;
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

	private static SpriteHandlerManager spriteHandlerManager;

	public Dictionary<NetworkIdentity, Dictionary<string, SpriteHandler>> PresentSprites =
		new Dictionary<NetworkIdentity, Dictionary<string, SpriteHandler>>();

	public static void RegisterHandler(NetworkIdentity networkIdentity,SpriteHandler spriteHandler)
	{

		if (networkIdentity == null)
		{
			Logger.LogError(" RegisterHandler networkIdentity is null on  > " + spriteHandler.transform.parent.name, Category.SpriteHandler);
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
				Logger.LogError("SpriteHandler has the same name as another SpriteHandler on the game object > " + spriteHandler.transform.parent.name, Category.SpriteHandler);
			}
		}
		Instance.PresentSprites[networkIdentity][spriteHandler.name] = spriteHandler;
	}


	// Start is called before the first frame update
    void Start()
    {
	    AvailableID = 1;
	    foreach (var Product in  SpriteCatalogue.Instance.Catalogue)
	    {
		    Product.ID = AvailableID;
		    AvailableID++;
	    }
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
		    Logger.LogError("Was unable to find A NetworkBehaviour for? yeah Youll have to look at this stack trace", Category.SpriteHandler);
		    return null;
	    }
    }
    // Update is called once per frame
    void Update()
    {

    }



}
