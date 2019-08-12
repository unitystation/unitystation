using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

public class SpriteHandlerData : NetworkBehaviour
{
	//TODO
	//Maybe a dictionary so you can easily look up in hands and stuff like that
	//With enum

	public SpriteDataForSH SpriteInfos;
	public List<Sprite> spriteList = new List<Sprite>();
	private SpriteJson spriteJson;
	// Start is called before the first frame update
	void Start()
	{
		SpriteInfos.DeSerializeT();
	}
	public void HELP()
	{
		if (SpriteInfos == null)
		{
			Logger.Log("OH FUCK!!");
		}
		else {
			Logger.Log("itit ok~");
		}
	}

	//public SpriteDataForSH GetSpriteData() { 
	//	return(SpriteInfos)

	//}

	public void SetUpSheet()
	{
		SpriteInfos = new SpriteDataForSH();
		SpriteInfos.spriteList = new List<List<List<SpriteInfo>>>();
		for (int i = 0; i < spriteList.Count; i++)
		{
			SpriteInfos.spriteList.Add(new List<List<SpriteInfo>>());
			var path = AssetDatabase.GetAssetPath(spriteList[i]).Substring(17);//Substring(17) To remove the "Assets/Resources/"
			Sprite[] spriteSheetSprites = Resources.LoadAll<Sprite>(path.Remove(path.Length - 4));
			if (spriteSheetSprites.Length > 1)
			{
				var p = AssetDatabase.GetAssetPath(spriteList[i]);
				int extensionIndex = p.LastIndexOf(".");
				p = p.Substring(0, extensionIndex) + ".json";
				string json = System.IO.File.ReadAllText(p);
				Logger.Log(json.ToString());
				spriteJson = JsonConvert.DeserializeObject<SpriteJson>(json);

				int c = 0;
				int cone = 0;
				for (int J = 0; J < spriteJson.Number_Of_Variants; J++)
				{
					Logger.Log("J > " + J.ToString());
					SpriteInfos.spriteList[i].Add(new List<SpriteInfo>());
				}

				foreach (var SP in spriteSheetSprites)
				{
					Logger.Log("c > " + c.ToString());
					Logger.Log("cone > " + cone.ToString());
					//Logger.Log("spriteJson.Delays[c] > " + spriteJson.Delays[c].Count.ToString());
					var info = new SpriteInfo();
					info.sprite = SP;
					if (spriteJson.Delays.Count > 0)
					{
						info.waitTime = spriteJson.Delays[c][cone];
					}

					Logger.Log("i > " + i.ToString());
					Logger.Log("c > " + c.ToString());

					SpriteInfos.spriteList[i][c].Add(info);
					Logger.Log("added");
					if (c >= (spriteJson.Number_Of_Variants - 1))
					{
						c = 0;
						cone++;
					}
					else
					{
						c++;
					}
				}
			}
			else {
				var info = new SpriteInfo()
				{
					sprite = spriteSheetSprites[0],
					waitTime = 0
				};
				SpriteInfos.spriteList[i].Add(new List<SpriteInfo>());
				SpriteInfos.spriteList[i][0].Add(info);
				Logger.Log("added");
			}

		}
		SpriteInfos.SerializeT();
		var IA = this.GetComponent<ItemAttributes>();
		if (IA != null) {
			IA.spriteHandlerData = this;
		}

		var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
		if (prefabStage != null)
		{
			EditorSceneManager.MarkSceneDirty(prefabStage.scene);
			Logger.Log("YOYO");
		}

	}

	public class SpriteInfo
	{
		public Sprite sprite;
		public float waitTime;
	}

	public class SpriteJson
	{
		public List<List<float>> Delays;
		public int Number_Of_Variants;
		public int Frames_Of_Animation;
	}
}
