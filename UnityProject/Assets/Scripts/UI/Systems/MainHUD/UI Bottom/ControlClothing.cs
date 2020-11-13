using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


	public class ControlClothing : TooltipMonoBehaviour
	{
		public GameObject retractableGroup;
		private Image[] equipImgs = new Image[0];
		private UI_ItemSlot[] itemSlots;
		public GameObject hideOnRetract;
		private bool isOpen;

		[SerializeField] private GameObject openButtonImage;
		[SerializeField] private GameObject closeButtonImage;
		/// <summary>
		/// Whether the expandable clothing menu is open
		/// </summary>
		public bool IsOpen => isOpen;
		public override string Tooltip => "toggle";

		private void Start()
		{
			isOpen = false;
			if ( retractableGroup )
			{
				equipImgs = retractableGroup.GetComponentsInChildren<Image>();
				itemSlots = retractableGroup.GetComponentsInChildren<UI_ItemSlot>();
			}
			ToggleEquipMenu(false);
		}

		public void RolloutEquipmentMenu()
		{
			SoundManager.Play("Click01");

			if (isOpen)
			{
				ToggleEquipMenu(false);
			}
			else
			{
				ToggleEquipMenu(true);
			}
		}

		private void ToggleEquipMenu(bool isOn)
		{
			isOpen = isOn;
			openButtonImage.SetActive(!isOn);
			closeButtonImage.SetActive(isOn);
			//TODO: This needs to hide the slots better
			if (isOn)
			{
				//Adjusting the alpha to hide the slots as the enabled state is handled
				//by other components. Raycast target is also adjusted based on on or off
				for (int i = 0; i < equipImgs.Length; i++)
				{
					Color tempCol = equipImgs[i].color;
					tempCol.a = 1f;
					equipImgs[i].color = tempCol;
					equipImgs[i].raycastTarget = true;
				}
			}
			else
			{
				for (int i = 0; i < equipImgs.Length; i++)
				{
					Color tempCol = equipImgs[i].color;
					tempCol.a = 0f;
					equipImgs[i].color = tempCol;
					equipImgs[i].raycastTarget = false;
				}
			}
			foreach (var itemSlot in itemSlots)
			{
				itemSlot.SetHidden(!isOpen);
			}
			if ( hideOnRetract != null )
			{
				hideOnRetract.SetActive( !isOn && UIManager.UseGamePad );
			}
		}
	}