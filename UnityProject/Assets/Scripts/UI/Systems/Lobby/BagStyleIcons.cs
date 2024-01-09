using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Systems.Lobby
{
	[CreateAssetMenu(fileName = "BagStyleIcons", menuName = "ScriptableObjects/UI/BagStyleIcons")]
	public class BagStyleIcons : ScriptableObject
	{
		public static BagStyleIcons Instance { get; private set; }
		public List<BagStyleData> Icons = new List<BagStyleData>();

		private void Awake()
		{
			Instance = this;
		}

		[Serializable]
		public class BagStyleData
		{
			public BagStyle Style;
			public Sprite Icon;
		}
	}
}