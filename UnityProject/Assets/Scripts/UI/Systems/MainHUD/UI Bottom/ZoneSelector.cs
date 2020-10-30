using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
			SelectAction((BodyPartType) curSelect);
		}

		/// <summary>
		/// Used for targeting specific body parts
		/// </summary>
		public void SelectAction(BodyPartType curSelect, bool clickSound = true)
		{
			if (clickSound)
			{

			SoundManager.Play(SingletonSOSounds.Instance.Click01);
		}
		selImg.sprite = selectorSprites[(int)curSelect];
			UIManager.DamageZone = curSelect;
		}

		/// <summary>
		/// Cycles through head -> eyes -> mouth -> head for hotkey targeting
		/// </summary>
		public void CycleHead()
		{
			switch (UIManager.DamageZone)
			{
				case BodyPartType.Head:
					SelectAction(BodyPartType.Eyes);
					break;
				case BodyPartType.Eyes:
					SelectAction(BodyPartType.Mouth);
					break;
				default:
					SelectAction(BodyPartType.Head);
					break;
			}
		}
	}
