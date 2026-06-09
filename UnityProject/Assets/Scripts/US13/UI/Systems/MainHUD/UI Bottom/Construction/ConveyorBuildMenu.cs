using UnityEngine;
using US13.Core.Addressables;
using US13.Managers;
using US13.Messages.Client;
using US13.Objects.Cargo.ConveyorBelts;
using US13.Systems.Construction;

namespace US13.UI.Systems.MainHUD.UI_Bottom.Construction
{
	public class ConveyorBuildMenu : MonoBehaviour
	{
		private BuildingMaterial materials;
		private BuildList.Entry entry;

		[SerializeField] private BuildList.Entry conveyorBeltPrefab;

		public BuildList.Entry ConveyorBeltPrefab => conveyorBeltPrefab;

		private bool isSandbox = false;

		public void OpenConveyorBuildMenu(BuildList.Entry entry, BuildingMaterial materials)
		{
			this.isSandbox = false;
			this.materials = materials;
			this.entry = entry;
			gameObject.SetActive(true);
		}

		public void OpenConveyorBuildMenu()
		{
			//FIXME : Add extra checks here when the sandbox gamemode is fully rolled out to not let people use this unless they're in that gamemode or are admins.
			this.isSandbox = true;
			gameObject.SetActive(true);
		}

		public void TryBuildBelt(int direction)
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if(isSandbox == false) CloseWindow();
			RequestConveyorBuildMessage.Send(isSandbox ? conveyorBeltPrefab : entry, materials, (ConveyorBelt.ConveyorDirection)direction, isSandbox);
		}

		public void GoToMainMenu()
		{
			UIManager.BuildMenu.ShowBuildMenu(materials);
		}

		public void CloseWindow()
		{
			gameObject.SetActive(false);
		}
	}
}
