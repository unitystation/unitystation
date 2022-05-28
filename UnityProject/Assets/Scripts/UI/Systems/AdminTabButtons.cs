using UnityEngine;
using AdminTools;
using AdminTools.VariableViewer;


namespace UI.AdminTools
{
	/// <summary>
	/// Behavior for the various buttons on the Dev tab
	/// </summary>
	public class AdminTabButtons : MonoBehaviour
	{
		public GUI_AdminTools adminTools;
		public GUI_DevSpawner devSpawner;
		public GUI_DevCloner devCloner;
		public GUI_DevDestroyer devDestroyer;
		public GUI_DevSelectVVTile devSelectTile;
		public GUI_VariableViewer vv;

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
		}
	}
}
