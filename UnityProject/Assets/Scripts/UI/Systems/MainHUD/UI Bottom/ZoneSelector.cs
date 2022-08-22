using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ZoneSelector : TooltipMonoBehaviour
	{
		public Sprite[] selectorSprites;
		public Image selImg;
		public override string Tooltip => "damage zone";

		private void Start()
		{
			// Select the chest initially
			SelectAction(BodyPartType.Chest, false);
		}

		/// <summary>
		/// Overload of SelectAction function to allow it to be used in unity editor (eg with OnClick)
		/// </summary>
		public void SelectAction(int curSelect)
		{
			SelectAction((BodyPartType)curSelect);
		}

		/// <summary>
		/// Used for targeting specific body parts
		/// </summary>
		public void SelectAction(BodyPartType curSelect, bool clickSound = true)
		{
			if (clickSound)
			{
				_ = SoundManager.Play(CommonSounds.Instance.Click01);
			}
			selImg.sprite = selectorSprites[(int)curSelect];
			UIManager.DamageZone = curSelect;
		}

		public void CycleZones(params BodyPartType[] zones)
		{
			if (zones.Length == 0)
				return;

			if (zones.Length == 1)
			{
				SelectAction(zones[0]);
				return;
			}

			for (int i = 0; i < zones.Length - 1; i++)
			{
				if (zones[i] == UIManager.DamageZone)
				{
					SelectAction(zones[i + 1]);
					return;
				}
			}

			SelectAction(zones[0]);
		}
	}
}
