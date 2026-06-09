using UnityEngine;
using UnityEngine.UI;
using US13.UI.Core;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	public class ControlWalkRun : TooltipMonoBehaviour
	{
		private Image image;
		public Sprite[] runWalkSprites;
		public override string Tooltip => "run/walk toggle";

		public bool running { get; set; } = true;

		private void Start()
		{
			image = GetComponent<Image>();
		}

		// BUTTON ARE NOW IN ControlIntent.cs

	}
}