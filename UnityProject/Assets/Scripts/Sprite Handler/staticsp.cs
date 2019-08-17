using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
public static class StaticSpriteHandler 
{
//  	public static SpriteDataForSH SetUpSheetForClothingData(ClothingData ClothingData, clothing Clothing, ItemAttributes itemAttributes)
//	{
//		SpriteHandlerData.SpriteJson spriteJson;
//		var SpriteInfos = new SpriteDataForSH();
//		SpriteInfos.spriteList = new List<List<List<SpriteHandlerData.SpriteInfo>>>();
//		var spriteList = new List<Texture2D>();

//		ClothingData.Base.Equipped

	
//			SpriteInfos.spriteList.Add(new List<List<SpriteHandlerData.SpriteInfo>>());
//			var path = AssetDatabase.GetAssetPath(spriteList[i]).Substring(17);//Substring(17) To remove the "Assets/Resources/"
//			Sprite[] spriteSheetSprites = Resources.LoadAll<Sprite>(path.Remove(path.Length - 4));
//			if (spriteSheetSprites.Length > 1)
//			{
//				var p = AssetDatabase.GetAssetPath(spriteList[i]);
//				int extensionIndex = p.LastIndexOf(".");
//				p = p.Substring(0, extensionIndex) + ".json";
//				string json = System.IO.File.ReadAllText(p);
//				Logger.Log(json.ToString());
//				spriteJson = JsonConvert.DeserializeObject<SpriteHandlerData.SpriteJson>(json);

//				int c = 0;
//				int cone = 0;
//				for (int J = 0; J < spriteJson.Number_Of_Variants; J++)
//				{
//					SpriteInfos.spriteList[i].Add(new List<SpriteHandlerData.SpriteInfo>());
//				}

//				foreach (var SP in spriteSheetSprites)
//				{
//					var info = new SpriteHandlerData.SpriteInfo();
//					info.sprite = SP;
//					if (spriteJson.Delays.Count > 0)
//					{
//						info.waitTime = spriteJson.Delays[c][cone];
//					}
//					SpriteInfos.spriteList[i][c].Add(info);
//					if (c >= (spriteJson.Number_Of_Variants - 1))
//					{
//						c = 0;
//						cone++;
//					}
//					else
//					{
//						c++;
//					}
//				}
//			}
//			else {
//				var info = new SpriteHandlerData.SpriteInfo()
//				{
//					sprite = spriteSheetSprites[0],
//					waitTime = 0
//				};
//				SpriteInfos.spriteList[i].Add(new List<SpriteHandlerData.SpriteInfo>());
//				SpriteInfos.spriteList[i][0].Add(info);
//			}


//		SpriteInfos.SerializeT();
//		return (SpriteInfos);
//	}

//	public static List<List<SpriteHandlerData.SpriteInfo>> CompleteSpriteSetup(TextureAndData textureAndData)
//	{ 
//		//Texture2D
//		var SpriteInfos = new List<List<SpriteHandlerData.SpriteInfo>>();
//		Sprite[] spriteSheetSprites = Resources.LoadAll<Sprite>(path.Remove(path.Length - 4));
//		if (spriteSheetSprites.Length > 1)
//		{
//			var p = AssetDatabase.GetAssetPath(spriteList[i]);
//			int extensionIndex = p.LastIndexOf(".");
//			p = p.Substring(0, extensionIndex) + ".json";
//			string json = System.IO.File.ReadAllText(p);
//			Logger.Log(json.ToString());
//			spriteJson = JsonConvert.DeserializeObject<SpriteHandlerData.SpriteJson>(json);

//			int c = 0;
//			int cone = 0;
//			for (int J = 0; J < spriteJson.Number_Of_Variants; J++)
//			{
//				SpriteInfos.spriteList[i].Add(new List<SpriteHandlerData.SpriteInfo>());
//			}

//			foreach (var SP in spriteSheetSprites)
//			{
//				var info = new SpriteHandlerData.SpriteInfo();
//				info.sprite = SP;
//				if (spriteJson.Delays.Count > 0)
//				{
//					info.waitTime = spriteJson.Delays[c][cone];
//				}
//				SpriteInfos.spriteList[i][c].Add(info);
//				if (c >= (spriteJson.Number_Of_Variants - 1))
//				{
//					c = 0;
//					cone++;
//				}
//				else
//				{
//					c++;
//				}
//			}
//		}
//	}

}
