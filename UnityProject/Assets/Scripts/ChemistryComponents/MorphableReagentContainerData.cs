using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

[System.Serializable]
public class ContainerCustomSprite
{
	public string CustomName;
	[TextArea]
	public string CustomDescription = "";
	public Sprite MainSprite;
}

[System.Serializable]
public class DictionaryReagentCustomSprite
	: SerializableDictionary<Chemistry.Reagent, ContainerCustomSprite>
{

}

[CreateAssetMenu(fileName = "morphable container", menuName = "ScriptableObjects/Chemistry/MorphableContainerData")]
public class MorphableReagentContainerData : ScriptableObject
{
	[SerializeField]
	private DictionaryReagentCustomSprite spritesData = new DictionaryReagentCustomSprite();

	public ContainerCustomSprite Get(Chemistry.Reagent reagent)
	{
		if (spritesData.ContainsKey(reagent))
			return spritesData[reagent];

		return null;
	}

	public ContainerCustomSprite Get(int reagentNameHash)
	{
		var pair = spritesData.FirstOrDefault((p) =>
			p.Key.Name.GetStableHashCode() == reagentNameHash);
		return pair.Value;
	}
}
