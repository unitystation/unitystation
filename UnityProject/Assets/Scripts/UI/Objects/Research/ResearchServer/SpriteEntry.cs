using UI.Core.Net.Elements;
using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects.Research
{
	public class SpriteEntry : DynamicEntry
	{
		[SerializeField] private NetSpriteHandler spriteHandler;

		public void Initialise(SpriteDataSO spriteData)
		{
			spriteHandler.MasterSetValue(spriteData.SetID);
		}

	}
}
