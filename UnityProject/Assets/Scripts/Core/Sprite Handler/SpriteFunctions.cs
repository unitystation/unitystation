using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// Used for generating [variant][animation frame] From the provided texture, sprites and related json data
/// </summary>
public static class SpriteFunctions
{
	/// <summary>
	/// Used for generating an Internal data list a single element of
	/// </summary>
	/// <returns>The sprite setup.</returns>
	/// <param name="textureAndData">Texture and data.</param>
	//public static List<List<SpriteHandler.SpriteInfo>> CompleteSpriteSetup(SpriteSheetAndData textureAndData)
	//{
		/*var SpriteInfos = new List<List<SpriteHandler.SpriteInfo>>();

		if (textureAndData.Sprites.Length > 1)
		{
			SpriteJson spriteJson;

			if (textureAndData.EquippedData == null) {
				Logger.LogError("Generating texture and mission data, data is missing for texture " + textureAndData.Texture.name);
				return(new List<List<SpriteHandler.SpriteInfo>>());
			}
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
		return (SpriteInfos);*/
	//}

	/// <summary>
	/// Used for generating [variant][animation frame] from a PlayerCustomisationData
	/// </summary>
	/// <returns>The sprite list</returns>
	/*public static List<List<SpriteHandler.SpriteInfo>> CompleteSpriteSetup(PlayerCustomisationData pcd)
	{
		if (pcd)
		{
			return CompleteSpriteSetup(pcd.Equipped);
		}

		return null;
	}
	*/

	/// <summary>
	/// Used for generating a single element within a The internal data holder
	/// </summary>
	/// <returns>The single sprite.</returns>
	/// <param name="textureAndData">Texture and data.</param>
	/*public static SpriteData SetupSingleSprite(SpriteSheetAndData textureAndData)
	{
		var SpriteInfos = new SpriteData();
		SpriteInfos.List = new List<List<List<SpriteHandler.SpriteInfo>>>();
		SpriteInfos.List.Add(CompleteSpriteSetup(textureAndData));
		return (SpriteInfos);
	}*/
}

public class SpriteJson
{
	public List<List<float>> Delays;
	public int Number_Of_Variants;
	public int Frames_Of_Animation;
}

