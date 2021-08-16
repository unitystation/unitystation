using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Systems.ObjectConnection;


namespace Core.Editor.Tools.Mapping
{
	/// <summary>
	/// An editor window to assist in quickly connecting slave devices to their masters while scene editing.
	/// </summary>
	public class DeviceLinkerWindow : EditorWindow
	{
		private static WindowTab[] tabs;

		private int activeWindowTab = 0;
		private int previousWindowTab = -1;

		private WindowTab Tab => tabs[activeWindowTab];

		[MenuItem("Tools/Mapping/Device Linker", priority = 120)]
		public static void ShowWindow()
		{
			GetWindow<DeviceLinkerWindow>().Show();
		}

		private void OnEnable()
		{
			titleContent = new GUIContent("Device Linker");
			var windowSize = minSize;
			windowSize.x = 250;
			minSize = windowSize;

			tabs = new WindowTab[] {
				//new WindowTab("ACU Devices", MultitoolConnectionType.ACU),
				new WindowTab("APC Devices", MultitoolConnectionType.APC),
			};
		}

		private void OnGUI()
		{
			EditorGUILayout.Space();
			activeWindowTab = GUILayout.Toolbar(activeWindowTab, tabs.Select(info => info.Name).ToArray());
			EditorGUILayout.Space();

			if (activeWindowTab != previousWindowTab)
			{
				DeviceLinker.InitDeviceLists(Tab.Type);
				previousWindowTab = activeWindowTab;
			}

			DrawUiElements();
		}

		private void DrawUiElements()
		{
			EditorGUILayout.LabelField("Link Devices", EditorStyles.boldLabel);

			if (DeviceLinker.Masters.Count < 1 || DeviceLinker.Slaves.Count < 1)
			{
				GUILayout.Label("No masters or no slaves found!");
				return;
			}

			Tab.RelinkConnected = GUILayout.Toggle(Tab.RelinkConnected, "Relink connected devices");

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Link"))
			{
				Tab.RelinkedSlavesCount = LinkSlaves(Tab.RelinkConnected);
			}
			GUILayout.Label($"<b>{DeviceLinker.LinkedCount}</b> / <b>{DeviceLinker.Slaves.Count}</b> connected", EditorUIUtils.LabelStyle, GUILayout.Width(120));
			GUILayout.EndHorizontal();
			if (Tab.RelinkedSlavesCount > -1)
			{
				GUILayout.Label($"Linked <b>{Tab.RelinkedSlavesCount}</b> / <b>{DeviceLinker.Slaves.Count}</b> devices.", EditorUIUtils.LabelStyle);
			}

			EditorGUILayout.Space();
			if (Tab.DistantSlaves.Count > 0)
			{
				DrawReviewDistantsElements();
			}
		}

		private void DrawReviewDistantsElements()
		{
			EditorGUILayout.LabelField("Distant Devices", EditorStyles.boldLabel);
			GUILayout.Label($"<b>{Tab.DistantSlaves.Count}</b> slave {(Tab.DistantSlaves.Count == 1 ? "device was" : "devices were")} "
					+ "too far from a master device for connection.", EditorUIUtils.LabelStyle);
			GUILayout.BeginHorizontal();
			bool btnPrevious = GUILayout.Button("Previous");
			bool btnNext = GUILayout.Button("Next");
			GUILayout.EndHorizontal();

			if (btnPrevious)
			{
				Tab.ReviewDistantSlaveIndex--;
			}
			if (btnNext)
			{
				Tab.ReviewDistantSlaveIndex++;
			}

			if (btnPrevious || btnNext)
			{
				Tab.ReviewDistantSlaveIndex = Mathf.Clamp(Tab.ReviewDistantSlaveIndex, 0, Tab.DistantSlaves.Count - 1);

				Selection.activeGameObject = Tab.DistantSlaves[Tab.ReviewDistantSlaveIndex].gameObject;
				SceneView.FrameLastActiveSceneView();
			}

			if (Tab.ReviewDistantSlaveIndex > -1) 
			{
				var slaveUnderReview = Tab.DistantSlaves[Tab.ReviewDistantSlaveIndex];
				float distance = DeviceLinker.LinkSlave(slaveUnderReview);
				if (distance < DeviceLinker.Masters[0].MaxDistance)
				{
					GUILayout.Label($"<b>{slaveUnderReview.gameObject.name}</b> has been relinked.</b>");
				}

				GUILayout.Label($"<b>{slaveUnderReview.gameObject.name}</b>: maximum distance of <b>{DeviceLinker.Masters[0].MaxDistance}</b> " +
						$"tiles exceeded; distance to nearest master found to be <b>{distance, 0:N}</b> tiles.", EditorUIUtils.LabelStyle);
			}
		}

		private int LinkSlaves(bool relinkConnected)
		{
			int count = DeviceLinker.Slaves.Count(slave => {
				if (slave.IsLinked && relinkConnected == false) return false;

				// Masters is sorted by each LinkSlave().
				float distance = DeviceLinker.LinkSlave(slave);
				if (distance > DeviceLinker.Masters[0].MaxDistance)
				{
					if (Tab.DistantSlaves.Contains(slave) == false)
					{
						Tab.DistantSlaves.Add(slave);
					}

					return false;
				}
				
				return true;
			});

			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
			return count;
		}

		/// <summary>Holds information unique to each tab.</summary>
		private class WindowTab
		{
			public string Name;
			public MultitoolConnectionType Type;

			public bool RelinkConnected = false;
			public int RelinkedSlavesCount = -1;
			public int ReviewDistantSlaveIndex = -1;

			public readonly List<IMultitoolSlaveable> DistantSlaves = new List<IMultitoolSlaveable>();

			public WindowTab(string name, MultitoolConnectionType type)
			{
				Name = name;
				Type = type;
			}
		}
	}


	/// <summary>
	/// Assistive class to link slave devices with master devices.
	/// </summary>
	public static class DeviceLinker
	{
		/// <summary>
		/// A list of all the master devices in the active scene as populated by <see cref="InitDeviceLists(MultitoolConnectionType)"/>.
		/// </summary>
		public static List<IMultitoolMasterable> Masters;
		/// <summary>
		/// A list of all the slave devices in the active as populated by <see cref="InitDeviceLists(MultitoolConnectionType)"/>.
		/// </summary>
		public static List<IMultitoolSlaveable> Slaves;

		/// <summary>How many slave devices in the scene consider themselves to be linked.</summary>
		public static int LinkedCount => Slaves.Count(slave => slave.IsLinked);

		/// <summary>
		/// Populates the <see cref="Masters"/> and <see cref="Slaves"/> device lists with the relevant devices from the active scene.
		/// </summary>
		/// <param name="type">The multitool connection otype the device must support</param>
		public static void InitDeviceLists(MultitoolConnectionType type)
		{
			Masters = FindUtils.FindInterfacesOfType<IMultitoolMasterable>().Where(master => master.ConType == type).ToList();
			Slaves = FindUtils.FindInterfacesOfType<IMultitoolSlaveable>().Where(master => master.ConType == type).ToList();
		}

		/// <summary>
		/// Sorts the devices in <see cref="Masters"/> by the distance to the specified position in order of closest to furthest.
		/// </summary>
		public static void SortMastersToPosition(Vector3 worldPosition)
		{
			Masters.Sort((a, b) =>
			{
				var aDistance = (a.gameObject.transform.position - worldPosition).sqrMagnitude;
				var bDistance = (b.gameObject.transform.position - worldPosition).sqrMagnitude;

				return aDistance.CompareTo(bDistance); // Ascending
			});
		}

		/// <summary>
		/// Links the given slave device to the closest master device.
		/// <para>If the distance to the closest master device exceeds the multitool connection's range,
		/// then the slave's master device will be set as null.</para>
		/// <remarks><see cref="InitDeviceLists(MultitoolConnectionType)"/> is assumed to have been run.</remarks>
		/// </summary>
		/// <returns>The distance to the closest master device, connected or not.</returns>
		public static float LinkSlave(IMultitoolSlaveable slave)
		{
			SortMastersToPosition(slave.gameObject.transform.position);

			float distance = Vector3.Distance(slave.gameObject.transform.position, Masters[0].gameObject.transform.position);
			slave.SetMaster(distance > Masters[0].MaxDistance ? null : Masters[0].gameObject.GetComponent<IMultitoolMasterable>());

			EditorUtility.SetDirty(slave as Component);
			return distance;
		}
	}
}
