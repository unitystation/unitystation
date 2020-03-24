using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdminTools
{
	/// <summary>
	/// Controls the updating and display of the info panels
	/// of in game items for admin use
	/// </summary>
	public class AdminOverlay : MonoBehaviour
	{
		[SerializeField] private GameObject infoPanelPrefab;
		private List<AdminOverlayPanel> infoPanelPool = new List<AdminOverlayPanel>();
		private List<AdminOverlayPanel> panelsInUse = new List<AdminOverlayPanel>();

		private void OnEnable()
		{
			SceneManager.activeSceneChanged += OnSceneChange;
		}

		private void OnDisable()
		{
			SceneManager.activeSceneChanged -= OnSceneChange;
		}

		void OnSceneChange(Scene oldScene, Scene newScene)
		{
			Init();
		}

		void Init()
		{
			for (int i = infoPanelPool.Count - 1; i > 0; i--)
			{
				if (infoPanelPool[i] != null)
				{
					Destroy(infoPanelPool[i].gameObject);
				}
			}

			for (int i = panelsInUse.Count - 1; i > 0; i--)
			{
				if (panelsInUse[i] != null)
				{
					Destroy(panelsInUse[i].gameObject);
				}
			}

			infoPanelPool.Clear();
			panelsInUse.Clear();

			for (int i = 0; i < 20; i++)
			{
				var obj = Instantiate(infoPanelPrefab, transform);
				infoPanelPool.Add(obj.GetComponent<AdminOverlayPanel>());
			}
		}

		public void ReturnToPool(AdminOverlayPanel panelToReturn)
		{
			if (panelToReturn != null)
			{
				if (panelsInUse.Contains(panelToReturn))
				{
					panelsInUse.Remove(panelToReturn);
				}

				if (!infoPanelPool.Contains(panelToReturn))
				{
					infoPanelPool.Add(panelToReturn);
				}

				panelToReturn.gameObject.SetActive(false);
			}
		}
	}
}
