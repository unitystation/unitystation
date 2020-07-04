using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;
using TMPro;
using Mirror;

namespace ServerInfo
{
	public class ServerInfoTMPLinks : NetworkBehaviour//, IPointerClickHandler
	{
		public TMP_Text tmp_Text;

		private Camera camera;

		private readonly SyncListString links = new SyncListString();

		private void Awake()
		{
			camera = Camera.main;
		}

		public override void OnStartClient()
		{
			links.Callback += OnInventoryUpdated;
		}

		[Server]
		private void ServerSyncLists(string[] newVar)
		{
			links.Clear();
			foreach (var url in newVar)
			{
				links.Add(url);
			}
		}

		void OnInventoryUpdated(SyncListString.Operation op, int index, string oldItem, string newItem)
		{
			switch (op)
			{
				case SyncListString.Operation.OP_ADD:
					links.Add(newItem);
					break;
				case SyncListString.Operation.OP_CLEAR:
					links.Clear();
					break;
				case SyncListString.Operation.OP_INSERT:
					// index is where it got added in the list
					// item is the new item
					break;
				case SyncListString.Operation.OP_REMOVEAT:
					// index is where it got removed in the list
					// item is the item that was removed
					break;
				case SyncListString.Operation.OP_SET:
					// index is the index of the item that was updated
					// item is the previous item
					break;
			}
		}

		private void Update()
		{
			if (Input.GetMouseButtonDown(0))
			{
				Debug.LogError("hover: " + TMP_TextUtilities.IsIntersectingRectTransform(tmp_Text.rectTransform, Input.mousePosition, camera));
				TestOnPointerClick();
			}
		}

		public void TestOnPointerClick(/*PointerEventData eventData*/)
		{
			Debug.LogError("test");
			//if (!TMP_TextUtilities.IsIntersectingRectTransform(tmp_Text.rectTransform, Input.mousePosition, camera)) return;

			Debug.LogError("test2");

			int linkHashCode = TMP_TextUtilities.FindNearestLine(tmp_Text, Input.mousePosition, camera);

			Debug.LogError("testhash " + linkHashCode);
			if (linkHashCode != -1)
			{ // was a link clicked?
				var linkInfo = tmp_Text.textInfo.linkInfo[linkHashCode];
				var linkID = linkInfo.GetLinkID();

				Debug.LogError("test3 "+ linkID);
				ProcessLink(linkID);
			}
		}

		public void ProcessLink(string linkID)
		{
			var linkIDInt = long.Parse(linkID);

			Debug.LogError("test4 " + linkIDInt);

			if(links.Count < linkIDInt) return;

			Debug.LogError("test5");

			var url = links[(int)linkIDInt];

			Debug.LogError("test6 "+ url);

			if(string.IsNullOrEmpty(url)) return;

			Debug.LogError("test7");

			Application.OpenURL(url);
		}

		public void OnServerInitialized()
		{
			var path = Path.Combine(Application.streamingAssetsPath, "config", "serverDescLinks.json");

			if (!File.Exists(path)) return;

			var linkList = JsonUtility.FromJson<ServerInfoLinks>(File.ReadAllText(path));

			Debug.LogError("ServerTest1");

			ServerSyncLists(linkList.Links);
		}

		private void Start()
		{
			var path = Path.Combine(Application.streamingAssetsPath, "config", "serverDescLinks.json");

			if (!File.Exists(path)) return;

			var linkList = JsonUtility.FromJson<ServerInfoLinks>(File.ReadAllText(path));

			Debug.LogError("ServerTest2");

			ServerSyncLists(linkList.Links);
		}

		[Serializable]
		public class ServerInfoLinks
		{
			public string[] Links;
		}
	}
}