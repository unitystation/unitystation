using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;

public static class StaticSpriteHandler
{
	public static SpriteDataForSH SetUpSheetForClothingData(ClothingData ClothingData, Clothing Clothing)
	{

		var SpriteInfos = new SpriteDataForSH();
		SpriteInfos.List = new List<List<List<SpriteHandlerData.SpriteInfo>>>();
		int c = 0;

		SpriteInfos.List.Add(CompleteSpriteSetup(ClothingData.Base.Equipped));
		Clothing.VariantStore[ClothingVariantType.Default] = c;
		c++;

		if (ClothingData.Base_Adjusted.Equipped.Texture != null)
		{
			SpriteInfos.List.Add(CompleteSpriteSetup(ClothingData.Base_Adjusted.Equipped));
			Clothing.VariantStore[ClothingVariantType.Tucked] = c;
			c++;
		}

		if (ClothingData.DressVariant.Equipped.Texture != null)
		{
			SpriteInfos.List.Add(CompleteSpriteSetup(ClothingData.DressVariant.Equipped));
			Clothing.VariantStore[ClothingVariantType.Skirt] = c;
			c++;
		}
		if (ClothingData.Variants.Count > 0)
		{
			foreach (var Variant in ClothingData.Variants)
			{
				SpriteInfos.List.Add(CompleteSpriteSetup(Variant.Equipped));
				Clothing.VariantStore[ClothingVariantType.Skirt] = c;
				c++;
			}
		}
		return (SpriteInfos);
	}

	public static List<List<SpriteHandlerData.SpriteInfo>> CompleteSpriteSetup(SpriteSheetAndData textureAndData)
	{
		var SpriteInfos = new List<List<SpriteHandlerData.SpriteInfo>>();

		if (textureAndData.Sprites.Length > 1)
		{
			SpriteHandlerData.SpriteJson spriteJson;
			//Logger.Log(textureAndData.Texture.name);
			spriteJson = JsonConvert.DeserializeObject<SpriteHandlerData.SpriteJson>(textureAndData.EquippedData.text);

			int c = 0;
			int cone = 0;
			for (int J = 0; J < spriteJson.Number_Of_Variants; J++)
			{
				SpriteInfos.Add(new List<SpriteHandlerData.SpriteInfo>());
			}

			foreach (var SP in textureAndData.Sprites)
			{
				var info = new SpriteHandlerData.SpriteInfo();
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
				var info = new SpriteHandlerData.SpriteInfo()
				{
					sprite = textureAndData.Sprites[0],
					waitTime = 0
				};
				SpriteInfos.Add(new List<SpriteHandlerData.SpriteInfo>());
				SpriteInfos[0].Add(info);
			}
			//else {
			//	Logger.LogError("HELP!!!!!");
			//}
		}
		return (SpriteInfos);
	}


	public static SpriteDataForSH SetupSingleSprite(SpriteSheetAndData textureAndData) { 
		var SpriteInfos = new SpriteDataForSH();
		SpriteInfos.List = new List<List<List<SpriteHandlerData.SpriteInfo>>>();
		SpriteInfos.List.Add(CompleteSpriteSetup(textureAndData));
		return (SpriteInfos);
	}



}

