using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SpriteHandlerManager : NetworkBehaviour
{

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
			Logger.LogWarning(" RegisterHandler networkIdentity is null on  > " + spriteHandler.transform.parent.name, Category.SpriteHandler);
		}

		if (Instance.PresentSprites.ContainsKey(networkIdentity) == false)
		{
			Instance.PresentSprites[networkIdentity] = new Dictionary<string, SpriteHandler>();
		}

		if (Instance.PresentSprites[networkIdentity].ContainsKey(spriteHandler.name))
		{
			if (Instance.PresentSprites[networkIdentity][spriteHandler.name] != spriteHandler)
			{
				Logger.LogWarning("SpriteHandler has the same name as another SpriteHandler on the game object > " + spriteHandler.transform.parent.name, Category.SpriteHandler);
			}
		}
		Instance.PresentSprites[networkIdentity][spriteHandler.name] = spriteHandler;
	}


	// Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
