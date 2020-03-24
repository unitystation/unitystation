using System.Collections;
using System.Collections.Generic;
using Mirror;
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
		private static AdminOverlay _adminOverlay;

		public static AdminOverlay Instance
		{
			get
			{
				if (_adminOverlay == null)
				{
					_adminOverlay = FindObjectOfType<AdminOverlay>();
				}

				return _adminOverlay;
			}
		}

		[SerializeField] private GameObject infoPanelPrefab;
		private List<AdminOverlayPanel> infoPanelPool = new List<AdminOverlayPanel>();
		private List<AdminOverlayPanel> panelsInUse = new List<AdminOverlayPanel>();

		private Dictionary<uint, AdminInfo> serverInfos = new Dictionary<uint, AdminInfo>();

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
			serverInfos.Clear();
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
				obj.SetActive(false);
				infoPanelPool.Add(obj.GetComponent<AdminOverlayPanel>());
			}
		}

		AdminOverlayPanel GetPanelFromPool()
		{
			if (infoPanelPool.Count == 0)
			{
				//non left, add some more:
				for (int i = 0; i < 10; i++)
				{
					var obj = Instantiate(infoPanelPrefab, transform);
					obj.SetActive(false);
					infoPanelPool.Add(obj.GetComponent<AdminOverlayPanel>());
				}
			}

			var panel = infoPanelPool[0];
			infoPanelPool.Remove(panel);
			panelsInUse.Add(panel);
			return panel;
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

		public static void ClientAddEntry(AdminInfosEntry entry)
		{
			var obj = NetworkIdentity.spawned[entry.netId];
			var objBehaviour = obj.GetComponent<ObjectBehaviour>();
			if (objBehaviour == null)
			{
				Logger.Log($"ERROR! Admin Info Overlays can only work with objects that have an ObjectBehaviour attached: {obj.name}");
				return;
			}
			var panel = Instance.GetPanelFromPool();
			panel.SetAdminOverlayPanel(entry.infos, Instance, objBehaviour, entry.offset);
		}

		public static void ClientFullUpdate(AdminInfoUpdate update)
		{
			foreach (var e in Instance.panelsInUse)
			{
				e.ReturnToPool();
			}

			foreach (var e in update.entries)
			{
				ClientAddEntry(e);
			}
		}

		public static void ServerAddInfoPanel(uint netId, AdminInfo adminInfo)
		{
			if (netId == NetId.Empty || netId == NetId.Invalid) return;
			if (Instance.serverInfos.ContainsKey(netId)) return;

			Instance.serverInfos.Add(netId, adminInfo);
			AdminInfoUpdateMessage.SendEntryToAllAdmins(new AdminInfosEntry
			{
				infos = adminInfo.StringInfo,
				netId = netId,
				offset = adminInfo.OffsetPosition
			});
		}

		public static void RequestFullUpdate(string adminId, string adminToken)
		{
			var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);

			if (admin != null)
			{
				AdminInfoUpdateMessage.SendFullUpdate(admin, Instance.serverInfos);
			}
			else
			{
				Logger.Log($"Someone tried to request all admin info overlay entries and failed. " +
				           $"Using adminId: {adminId} and token: {adminToken}");
			}
		}
	}
}
