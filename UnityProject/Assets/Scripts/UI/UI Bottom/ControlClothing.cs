using UnityEngine;
using UnityEngine.UI;


	public class ControlClothing : MonoBehaviour
	{
		public GameObject retractableGroup;
		private Image[] equipImgs = new Image[0];
		public GameObject hideOnRetract;
		private bool isOpen;

		private void Start()
		{
			isOpen = false;
			if ( retractableGroup )
			{
				equipImgs = retractableGroup.GetComponentsInChildren<Image>();
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
			if ( hideOnRetract != null )
			{
				hideOnRetract.SetActive( !isOn && UIManager.UseGamePad );
			}
		}
	}
