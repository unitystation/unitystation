using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

namespace Chemistry.Components
{
	[Serializable]
	public class ContainerCustomSprite
	{
		public string CustomName;
		[TextArea]
		public string CustomDescription = "";
		public SpriteDataSO MainSpriteSO;
	}

	[Serializable]
	public class DictionaryReagentCustomSprite : SerializableDictionary<Reagent, ContainerCustomSprite>
	{

	}

	[CreateAssetMenu(fileName = "morphable container", menuName = "ScriptableObjects/Chemistry/MorphableContainerData")]
	public class MorphableReagentContainerData : ScriptableObject
	{
		[SerializeField]
		private DictionaryReagentCustomSprite spritesData = new DictionaryReagentCustomSprite();

		public ContainerCustomSprite Get(Reagent reagent)
		{
			if (spritesData.ContainsKey(reagent))
			{
				return spritesData[reagent];
			}

			return null;
		}

		public ContainerCustomSprite Get(int reagentNameHash)
		{
			var pair = spritesData.FirstOrDefault((p) =>
					p.Key.Name.GetStableHashCode() == reagentNameHash);
			return pair.Value;
		}
	}
}
