using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ContainerCustomSprite
{
	public string CustomDescription = "";
	public SpriteSheetAndData SpriteSheet;
}

[System.Serializable]
public class DictionaryReagentCustomSprite
	: SerializableDictionary<Chemistry.Reagent, ContainerCustomSprite>
{

}

[CreateAssetMenu(fileName = "custom sprites", menuName = "ScriptableObjects/Chemistry/ContainerCustomSprites")]
public class ReagentContainerCustomSprites : ScriptableObject
{
	[SerializeField]
	private DictionaryReagentCustomSprite spritesData = new DictionaryReagentCustomSprite();

	public ContainerCustomSprite Get(Chemistry.Reagent reagent)
	{
		if (spritesData.ContainsKey(reagent))
			return spritesData[reagent];

		return null;
	}

	public ContainerCustomSprite Get(string reagentName)
	{
		var pair = spritesData.FirstOrDefault((p) => p.Key.Name == reagentName);
		return pair.Value;
	}
}
