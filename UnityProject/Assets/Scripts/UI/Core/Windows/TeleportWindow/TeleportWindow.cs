using System;
using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;
using TMPro;
using Systems.Teleport;

namespace UI.Core.Windows
{
	/// <summary>
	/// A generic, reusable window for teleportation.
	/// </summary>
	public class TeleportWindow : MonoBehaviour
	{
		[Header("Assign teleport window references")]
		[SerializeField]
		private GameObject buttonTemplate = null;//Sets what button to use in editor
		[SerializeField]
		private TextMeshProUGUI titleText = null;
		[SerializeField]
		private GameObject coordinateTeleportRegion = null;

		[Header("Options")]
		[SerializeField]
		private bool showCoordTeleportRegion = true;

		public event Action<TeleportInfo> onTeleportRequested;
		public event Action<Vector3> onTeleportToVector;

		public List<GameObject> TeleportButtons { get; private set; } = new List<GameObject>();

		private TeleportButtonSearchBar SearchBar;

		public bool OrbitOnTeleport = false;

		private void Start()
		{
			SearchBar = GetComponentInChildren<TeleportButtonSearchBar>();

			if (!showCoordTeleportRegion)
			{
				coordinateTeleportRegion.SetActive(false);
			}
		}

		public void ButtonClicked(TeleportInfo info)
		{
			onTeleportRequested?.Invoke(info);
			if (PlayerManager.LocalPlayer.TryGetComponent<GhostOrbit>(out var orbit) == false) return;
			orbit.CmdStopOrbiting();
			if (OrbitOnTeleport == false) return;
			orbit.CmdServerOrbit(info.gameObject);
		}

		public void TeleportToVector(Vector3 vector)
		{
			onTeleportToVector?.Invoke(vector);
		}

		public void CloseWindow()
		{
			ResetWindow();
			gameObject.SetActive(false);
		}

		public void SetWindowTitle(string newText)
		{
			titleText.text = newText;
		}

		public void GenerateButtons(IEnumerable<TeleportInfo> teleportInfos)
		{
			ResetWindow();

			foreach (var teleportInfo in teleportInfos)
			{
				GenerateButton(teleportInfo);
			}
		}

		private void GenerateButton(TeleportInfo entry)
		{
			GameObject button = Instantiate(buttonTemplate);
			var teleportButton = button.GetComponent<TeleportButton>();
			teleportButton.SetValues(this, entry);

			button.transform.SetParent(buttonTemplate.transform.parent, false);
			button.SetActive(true);

			TeleportButtons.Add(teleportButton.gameObject);
		}

		private void ResetWindow()
		{
			if (SearchBar != null)
			{
				SearchBar.ResetText();
			}

			foreach (GameObject x in TeleportButtons)//resets buttons everytime it opens
			{
				Destroy(x);
			}
		}
	}
}

namespace Systems.Teleport
{
	public class TeleportInfo
	{
		public readonly string text;
		public readonly Vector3Int position;
		public readonly GameObject gameObject;

		public TeleportInfo(string text, Vector3Int position, GameObject gameObject)
		{
			this.text = text;
			this.position = position;
			this.gameObject = gameObject;
		}
	}
}
