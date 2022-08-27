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

		private void Awake()
		{
			DisableAllGUI();
		}

		public void BtnAdminPanel()
		{
			DisableAllGUI();
			adminTools.gameObject.SetActive(true);
		}

		public void BtnSpawnItem()
		{
			DisableAllGUI();
			devSpawner.gameObject.SetActive(true);
			devSpawner.Open();
		}

		public void BtnCloneItem()
		{
			DisableAllGUI();
			devCloner.gameObject.SetActive(true);
			devCloner.Open();
		}

		public void BtnDestroyItem()
		{
			DisableAllGUI();
			devDestroyer.gameObject.SetActive(true);
		}

		public void BtnOpenVV()
		{
			DisableAllGUI();
			UIManager.Instance.VariableViewer.Open();
		}

		public void BtnOpenTileChange()
		{
			DisableAllGUI();
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			tileChanger.Open();
		}

		public void BtnOpenTileVV()
		{
			DisableAllGUI();
			devSelectTile.gameObject.SetActive(true);
			devSelectTile.Open();
		}

		private void DisableAllGUI()
		{
			adminTools.gameObject.SetActive(false);
			devSpawner.gameObject.SetActive(false);
			devCloner.gameObject.SetActive(false);
			devDestroyer.gameObject.SetActive(false);
			devSelectTile.gameObject.SetActive(false);
			tileChanger.Close();
		}
	}
}
