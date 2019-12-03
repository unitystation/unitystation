using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;

public static class StaticSpriteHandler
{
	public static List<List<SpriteDataHandler.SpriteInfo>> CompleteSpriteSetup(SpriteSheetAndData textureAndData)
	{
		var SpriteInfos = new List<List<SpriteDataHandler.SpriteInfo>>();

		if (textureAndData.Sprites.Length > 1)
		{
			SpriteDataHandler.SpriteJson spriteJson;
			spriteJson = JsonConvert.DeserializeObject<SpriteDataHandler.SpriteJson>(textureAndData.EquippedData.text);
			int c = 0;
			int cone = 0;
			for (int J = 0; J < spriteJson.Number_Of_Variants; J++)
			{
				SpriteInfos.Add(new List<SpriteDataHandler.SpriteInfo>());
			}

			foreach (var SP in textureAndData.Sprites)
			{
				var info = new SpriteDataHandler.SpriteInfo();
				info.sprite = SP;
				if (spriteJson.Delays.Count > 0)
				{
					info.waitTime = spriteJson.Delays[c][cone];
				}

				SpriteInfos[c].Add(info);
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
			if (textureAndData.Sprites.Length > 0)
			{
				var info = new SpriteDataHandler.SpriteInfo()
				{
					sprite = textureAndData.Sprites[0],
					waitTime = 0
				};
				SpriteInfos.Add(new List<SpriteDataHandler.SpriteInfo>());
				SpriteInfos[0].Add(info);
			}
			//else {
			//	Logger.LogError("HELP!!!!!"); // Nope, you got us into this mess
			//}
		}
		return (SpriteInfos);
	}


	public static SpriteData SetupSingleSprite(SpriteSheetAndData textureAndData) {
		var SpriteInfos = new SpriteData();
		SpriteInfos.List = new List<List<List<SpriteDataHandler.SpriteInfo>>>();
		SpriteInfos.List.Add(CompleteSpriteSetup(textureAndData));
		return (SpriteInfos);
	}
}

