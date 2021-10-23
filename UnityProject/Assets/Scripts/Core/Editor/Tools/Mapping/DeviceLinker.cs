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

			PopulateTabs();

			EditorSceneManager.sceneOpened += (oldScene, newScene) =>
			{
				PopulateTabs();
				DeviceLinker.InitDeviceLists(Tab.Type, forceRefresh: true);
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

		private void PopulateTabs()
		{
			tabs = new WindowTab[] {
				new WindowTab("APC Devices", MultitoolConnectionType.APC),
				new WindowTab("ACU Devices", MultitoolConnectionType.Acu),
				new WindowTab("Firelocks", MultitoolConnectionType.FireAlarm),
				new WindowTab("Lights", MultitoolConnectionType.LightSwitch),
			};
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

			if (Tab.IgnoredSlaves.Count > 0)
			{
				GUILayout.Label($"<b>{Tab.IgnoredSlaves.Count}</b> " +
					$"{(Tab.IgnoredSlaves.Count == 1 ? "was ignored. It" : "were ignored. They")} may not require a link.",
					EditorUIUtils.LabelStyle);
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
				Tab.ReviewDistantSlaveNewDistance = -1;
				Tab.ReviewDistantSlaveIndex = Mathf.Clamp(Tab.ReviewDistantSlaveIndex, 0, Tab.DistantSlaves.Count - 1);

				Selection.activeGameObject = Tab.DistantSlaves[Tab.ReviewDistantSlaveIndex].gameObject;
				SceneView.FrameLastActiveSceneView();
			}

			if (Tab.ReviewDistantSlaveIndex > -1) 
			{
				var slaveUnderReview = Tab.DistantSlaves[Tab.ReviewDistantSlaveIndex];

				if (EditorUIUtils.BigAssButton("Retry"))
				{
					Tab.ReviewDistantSlaveNewDistance = DeviceLinker.TryLinkSlaveToClosestMaster(slaveUnderReview);
					EditorUtility.SetDirty((Component)slaveUnderReview);
				}

				if (Tab.ReviewDistantSlaveNewDistance > DeviceLinker.Masters[0].MaxDistance)
				{
					GUILayout.Label($"<b>{slaveUnderReview.gameObject.name}</b>: maximum distance of <b>{DeviceLinker.Masters[0].MaxDistance}</b> " +
							$"tiles exceeded; distance to nearest master found to be <b>{Tab.ReviewDistantSlaveNewDistance, 0:N}</b> tiles.",
							EditorUIUtils.LabelStyle);
				}
				else if (slaveUnderReview.Master != null)
				{
					GUILayout.Label($"<b>{slaveUnderReview.gameObject.name}</b> has been relinked.", EditorUIUtils.LabelStyle);
				}
				else
				{
					GUILayout.Label($"<b>{slaveUnderReview.gameObject.name}</b>'s link status is uncertain.", EditorUIUtils.LabelStyle);
				}
			}
		}

		private int LinkSlaves(bool relinkConnected)
		{
			int count = DeviceLinker.Slaves.Count(slave => {
				if (slave.RequireLink == false)
				{
					if (Tab.IgnoredSlaves.Contains(slave) == false)
					{
						Tab.IgnoredSlaves.Add(slave);
					}

					return false;
				}


				if (slave.Master != null && relinkConnected == false) return false;

				float distance = DeviceLinker.TryLinkSlaveToClosestMaster(slave);
				if (distance > DeviceLinker.Masters[0].MaxDistance)
				{
					if (Tab.DistantSlaves.Contains(slave) == false)
					{
						Tab.DistantSlaves.Add(slave);
					}

					return false;
				}

				EditorUtility.SetDirty((Component)slave);
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
			public float ReviewDistantSlaveNewDistance = -1;

			public readonly List<IMultitoolSlaveable> IgnoredSlaves = new List<IMultitoolSlaveable>();
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
		private static Dictionary<MultitoolConnectionType, List<IMultitoolMasterable>> masters= new Dictionary<MultitoolConnectionType, List<IMultitoolMasterable>>();
		private static Dictionary<MultitoolConnectionType, List<IMultitoolSlaveable>> slaves = new Dictionary<MultitoolConnectionType, List<IMultitoolSlaveable>>();
		private static MultitoolConnectionType currentConType;

		/// <summary>
		/// A list of all the master devices in the active scene as populated by <see cref="InitDeviceLists(MultitoolConnectionType)"/>.
		/// </summary>
		public static List<IMultitoolMasterable> Masters => masters[currentConType];
		/// <summary>
		/// A list of all the slave devices in the active scene as populated by <see cref="InitDeviceLists(MultitoolConnectionType)"/>.
		/// </summary>
		public static List<IMultitoolSlaveable> Slaves => slaves[currentConType];

		/// <summary>How many slave devices in the scene consider themselves to be linked.</summary>
		public static int LinkedCount => Slaves.Count(slave => slave.Master != null);

		/// <summary>
		/// Populates the <see cref="Masters"/> and <see cref="Slaves"/> device lists with the relevant devices from the active scene.
		/// </summary>
		/// <param name="type">The multitool connection otype the device must support</param>
		public static void InitDeviceLists(MultitoolConnectionType type, bool forceRefresh = false)
		{
			currentConType = type;

			if (forceRefresh == false
					&& masters.TryGetValue(type, out var master) && master != null
					&& slaves.TryGetValue(type, out var slave) && slave != null)
			{
				return;
			}

			masters[type] = FindUtils.FindInterfacesOfType<IMultitoolMasterable>().Where(master => master.ConType == type).ToList();
			slaves[type] = FindUtils.FindInterfacesOfType<IMultitoolSlaveable>().Where(master => master.ConType == type).ToList();
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
		/// then the slave's master device will be set as <c>null</c>.</para>
		/// <remarks><see cref="InitDeviceLists(MultitoolConnectionType)"/> is assumed to have been run.</remarks>
		/// </summary>
		/// <returns>The distance to the closest master device, connected or not. <c>-1</c> if no masters found.</returns>
		public static float TryLinkSlaveToClosestMaster(IMultitoolSlaveable slave)
		{
			if (Masters.Count < 1) return -1;

			SortMastersToPosition(slave.gameObject.transform.position);

			float distance = Vector3.Distance(slave.gameObject.transform.position, Masters[0].gameObject.transform.position);
			slave.SetMasterEditor(distance > Masters[0].MaxDistance ? null : Masters[0]);

			return distance;
		}

		/// <summary>
		/// Links the given slave device to the next master device in the given direction.
		/// <para>If the distance to the closest master device exceeds the multitool connection's range,
		/// then no action will take place.</para>
		/// <remarks><see cref="InitDeviceLists(MultitoolConnectionType)"/> is assumed to have been run.</remarks>
		/// </summary>
		/// <param name="direction">Expecting <c>1</c> for the next closest device, <c>-1</c> for closer.</param>
		/// <returns>The distance to the next master device. <c>-1</c> if no masters found.</returns>
		public static float TryLinkToNextMaster(IMultitoolSlaveable slave, int direction)
		{
			if (Masters.Count < 1) return -1;

			SortMastersToPosition(slave.gameObject.transform.position);

			var index = slave.Master == null ? 0 : Masters.IndexOf(slave.Master);
			index = Mathf.Clamp(index + direction, 0, Masters.Count - 1);
			var master = Masters[index];
			var distance = Vector3.Distance(slave.gameObject.transform.position, master.gameObject.transform.position);

			if (distance <= master.MaxDistance)
			{
				slave.SetMasterEditor(master);
			}

			return distance;
		}
	}
}
