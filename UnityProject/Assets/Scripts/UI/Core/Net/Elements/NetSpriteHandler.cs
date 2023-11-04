using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core.Net.Elements
{
	[RequireComponent(typeof(SpriteHandler))]
	[Serializable]
	public class NetSpriteHandler : NetUIIntElement
	{
		public override int Value
		{
			get
			{
				return index;
			}
			protected set
			{
				if (index == value) return;

				index = value;
				CurrentSpriteData = SpriteCatalogue.ResistantCatalogue[index];
			}
		}

		private int index;

		private SpriteDataSO CurrentSpriteData
		{
			get
			{
				return SpriteCatalogue.ResistantCatalogue[Value];
			}
			set
			{
				Element.SetSpriteSO(value);
			}
		}

		private SpriteHandler element;
		public SpriteHandler Element => element ??= GetComponent<SpriteHandler>();
	}
}
