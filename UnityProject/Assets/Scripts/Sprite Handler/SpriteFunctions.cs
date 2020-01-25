using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;

public static class SpriteFunctions
{
	public static List<List<SpriteHandler.SpriteInfo>> CompleteSpriteSetup(SpriteSheetAndData textureAndData)
	{
		var SpriteInfos = new List<List<SpriteHandler.SpriteInfo>>();

		if (textureAndData.Sprites.Length > 1)
		{
			SpriteJson spriteJson;
			spriteJson = JsonConvert.DeserializeObject<SpriteJson>(textureAndData.EquippedData.text);
			int variance = 0;
			int frame = 0;
			for (int J = 0; J < spriteJson.Number_Of_Variants; J++)
			{
				SpriteInfos.Add(new List<SpriteHandler.SpriteInfo>());
			}

			foreach (var SP in textureAndData.Sprites)
			{
				var info = new SpriteHandler.SpriteInfo();
				info.sprite = SP;
				if (spriteJson.Delays.Count > 0)
				{
					info.waitTime = spriteJson.Delays[variance][frame];
				}

				SpriteInfos[variance].Add(info);
				if (variance >= (spriteJson.Number_Of_Variants - 1))
				{
					variance = 0;
					frame++;
				}
				else
				{
					variance++;
				}
			}
		}
		else {
			if (textureAndData.Sprites.Length > 0)
			{
				var info = new SpriteHandler.SpriteInfo()
				{
					sprite = textureAndData.Sprites[0],
					waitTime = 0
				};
				SpriteInfos.Add(new List<SpriteHandler.SpriteInfo>());
				SpriteInfos[0].Add(info);
			}

		}
		return (SpriteInfos);
	}


	public static SpriteData SetupSingleSprite(SpriteSheetAndData textureAndData)
	{
		var SpriteInfos = new SpriteData();
		SpriteInfos.List = new List<List<List<SpriteHandler.SpriteInfo>>>();
		SpriteInfos.List.Add(CompleteSpriteSetup(textureAndData));
		return (SpriteInfos);
	}
}

public class SpriteJson
{
	public List<List<float>> Delays;
	public int Number_Of_Variants;
	public int Frames_Of_Animation;
}

