using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Systems.Lobby
{
	[CreateAssetMenu(fileName = "ClothingStyleIcons", menuName = "ScriptableObjects/UI/ClothingStyleIcons")]
	public class ClothingSyleIcons : ScriptableObject
	{
		public List<BagStyleData> Icons = new List<BagStyleData>();

		[Serializable]
		public class BagStyleData
		{
			public ClothingStyle Style;
			public Sprite Icon;
		}
	}
}