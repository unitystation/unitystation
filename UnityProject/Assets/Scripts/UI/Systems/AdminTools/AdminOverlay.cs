﻿using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using Messages.Server.AdminTools;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

		[SerializeField] private GameObject infoPanelPrefab = null;
		private List<AdminOverlayPanel> infoPanelPool = new List<AdminOverlayPanel>();
		private List<AdminOverlayPanel> panelsInUse = new List<AdminOverlayPanel>();

		private Dictionary<uint, AdminInfo> serverInfos = new Dictionary<uint, AdminInfo>();

		[SerializeField] private Button overlayToggleButton = null;
		[SerializeField] private Color selectedColor = new Color(67, 112, 156); // Ocean blue
		[SerializeField] private Color unSelectedColor = new Color(59, 78, 96); // Darker ocean blue

		public bool IsOn { get; private set; }

		void Awake()
		{
			Init();
		}

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
			foreach (Transform t in transform)
			{
				var panel = t.GetComponent<AdminOverlayPanel>();
				if (panel != null && t.gameObject.activeInHierarchy)
				{
					panel.ReturnToPool();
				}
			}
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
			if (!Instance.IsOn) return;

			var obj = NetworkIdentity.spawned[entry.netId];
			var panel = Instance.GetPanelFromPool();
			panel.SetAdminOverlayPanel(entry.infos, Instance, obj.transform, entry.offset);
		}

		public static void ClientFullUpdate(AdminInfoUpdate update)
		{
			Instance.ReturnAllPanelsToPool();

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

		public void ToggleOverlayBtn()
		{
			IsOn = !IsOn;

			if (IsOn)
			{
				if (PlayerManager.LocalPlayerScript == null)
				{
					Logger.LogError("Cannot activate Admin Overlay with PlayerManager.LocalPlayerScript being null");
					IsOn = false;
					overlayToggleButton.image.color = unSelectedColor;
				}
				else
				{
					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdGetAdminOverlayFullUpdate(ServerData.UserID, PlayerList.Instance.AdminToken);
					overlayToggleButton.image.color = selectedColor;
				}
			}
			else
			{
				overlayToggleButton.image.color = unSelectedColor;
				ReturnAllPanelsToPool();
			}
		}

		void ReturnAllPanelsToPool()
		{
			for (int i = Instance.panelsInUse.Count - 1; i >= 0; i--)
			{
				Instance.panelsInUse[i].ReturnToPool();
			}
		}
	}
}
