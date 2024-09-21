using UnityEngine;
using AdminTools;
using UI.Systems.AdminTools.DevTools;
using AdminTools.VariableViewer;


namespace UI.AdminTools
{
	/// <summary>
	/// Behavior for the various buttons on the Dev tab
	/// </summary>
	public class AdminTabButtons : MonoBehaviour
	{
		[SerializeField]
		private GUI_AdminTools adminTools = null;
		[SerializeField]
		private GUI_DevSpawner devSpawner = null;
		[SerializeField]
		private GUI_DevCloner devCloner = null;
		[SerializeField]
		private GUI_DevDestroyer devDestroyer = null;
		[SerializeField]
		private GUI_DevSelectVVTile devSelectTile = null;
		[SerializeField]
		private GUI_VariableViewer vv = null;
		[SerializeField]
		private GUI_DevTileChanger tileChanger = null;

		public void BtnAdminPanel()
		{
			adminTools.gameObject.SetActive(true);
		}

		public void BtnSpawnItem()
		{
			devSpawner.gameObject.SetActive(true);
			devSpawner.Open();
		}

		public void BtnCloneItem()
		{
			devCloner.gameObject.SetActive(true);
			devCloner.Open();
		}

		public void BtnDestroyItem()
		{
			devDestroyer.gameObject.SetActive(true);
		}

		public void BtnOpenVV()
		{
			UIManager.Instance.VariableViewer.Open();
		}

		public void BtnOpenTileChange()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			tileChanger.Open();
		}

		public void BtnOpenTileVV()
		{
			devSelectTile.gameObject.SetActive(true);
			devSelectTile.Open();
		}

		public void BtnOpenLinker()
		{
			InGameDeviceLinker.Instance.gameObject.SetActive(true);
		}

		public void BtnOpenRotator()
		{
			DeviceRotator.Instance.gameObject.SetActive(true);
		}

		public void BtnOpenCameraControls()
		{
			DevCameraControls.Instance.gameObject.SetActive(true);
		}

		public void BtnOpenDeviceMover()
		{
			DeviceMover.Instance.gameObject.SetActive(true);
		}

		public void BtnOpenCopyAndPaste()
		{
			CopyAndPaste.Instance.gameObject.SetActive(true);
		}

		public void BtnOpenDeviceRenamer()
		{
			DeviceAttributeEditor.Instance.gameObject.SetActive(true);
		}
	}
}
