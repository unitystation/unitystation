using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


	public class ControlClothing : TooltipMonoBehaviour
	{
		public GameObject retractableGroup;
		public GameObject hideOnRetract;
		private bool isOpen;

		public GameObject ObjectToHide;

		[SerializeField] private GameObject openButtonImage = default;
		[SerializeField] private GameObject closeButtonImage = default;
		/// <summary>
		/// Whether the expandable clothing menu is open
		/// </summary>
		public bool IsOpen => isOpen;
		public override string Tooltip => "toggle";

		private void Start()
		{
			isOpen = false;
			ToggleEquipMenu(false);
		}

		public void RolloutEquipmentMenu()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			ToggleEquipMenu(isOpen == false);
		}

		private void ToggleEquipMenu(bool isOn)
		{
			isOpen = isOn;
			openButtonImage.SetActive(!isOn);
			closeButtonImage.SetActive(isOn);
			//TODO: This needs to hide the slots better
			if (isOn)
			{
				ObjectToHide.SetActive(true);
			}
			else
			{
				ObjectToHide.SetActive(false);
			}
			if ( hideOnRetract != null )
			{
				hideOnRetract.SetActive( !isOn && UIManager.UseGamePad );
			}
		}
	}
