using Doors;
using NUnit.Framework;
using Objects.Engineering;
using Objects.Lighting;
using Objects.Wallmounts;
using Systems.Electricity;

namespace Tests.Scenes
{
	public class PowerAndLightsSceneTests : SceneTest
	{
		public PowerAndLightsSceneTests(SceneTestData data) : base(data)
		{
		}

		/// <summary>
		/// Finds all APCs
		/// Checks if Device list is empty
		/// if there are null values in the list
		/// if device is not assigned to this APC
		/// </summary>
		[Test]
		public void APCPoweredDevicesHaveRelatedAPC()
		{
			foreach (var device in RootObjects.ComponentsInChildren<APCPoweredDevice>().NotNull())
			{
				if (device.IsSelfPowered) continue;

				var position = device.transform.position;
				Report.FailIf(device.RelatedAPC, Is.Null)
					.AppendLine($"{Scene.name}: \"{device.name}\" at {position} has a missing APC reference")
					.MarkDirtyIfFailed()
					.FailIf(device.RelatedAPC.OrNull()?.ConnectedDevices.Contains(device) == false)
					.Append($"{Scene.name}: \"{device.name}\" at {position} has a connected APC reference, ")
					.Append("but the APC doesn't have this device.")
					.AppendLine();
			}

			Report.AssertPassed();
		}

		/// <summary>
		/// Finds all APCs
		/// Checks if Device list is empty
		/// if there are null values in the list
		/// if device is not assigned to this APC
		/// </summary>
		[Test]
		public void APCsConnectedDevicesContainsValidReferences()
		{
			var sceneName = Scene.name;
			foreach (var apc in RootObjects.ComponentsInChildren<APC>().NotNull())
			{
				foreach (var connectedDevice in apc.ConnectedDevices)
				{
					if (connectedDevice == null)
					{
						Report.Fail()
							.AppendLine($"{sceneName}: APC at \"{apc.transform.HierarchyName()}\" has a null value in the list.");
						continue;
					}

					Report.FailIfNot(connectedDevice.RelatedAPC, Is.EqualTo(apc))
						.Append($"{sceneName}: Device at \"{connectedDevice.transform.HierarchyName()}\" ")
						.Append($"is not assigned to connected devices in APC at \"{apc.transform.HierarchyName()}\".")
						.AppendLine();
				}
			}

			Report.AssertPassed();
		}

		[Test]
		public void StatusDisplaysDoNotHaveNullDoors()
		{
			foreach (var display in RootObjects.ComponentsInChildren<StatusDisplay>().NotNull())
			{
				var position = display.transform.position;
				foreach (var doorController in display.doorControllers)
				{
					Report.FailIf(doorController, Is.Null)
						.AppendLine($"{Scene.name}: \"{display.name}\" at {position} has a null {nameof(DoorController)}.");
				}
			}

			Report.AssertPassed();
		}

		[Test]
		public void LightSourcesDoNotHaveMissingSwitch()
		{
			foreach (var lightSource in RootObjects.ComponentsInChildren<LightSource>().NotNull())
			{
				if (lightSource.IsWithoutSwitch) continue;

				var position = lightSource.transform.position;
				Report.FailIf(lightSource.relatedLightSwitch, Is.Null)
					.AppendLine($"{Scene.name}: \"{lightSource.name}\" at {position} has a missing switch reference.");
			}

			Report.AssertPassed();
		}

		[Test]
		public void LightSwitchesHaveLightSources()
		{
			foreach (var lightSwitch in RootObjects.ComponentsInChildren<LightSwitchV2>().NotNull())
			{
				var position = lightSwitch.transform.position;
				Report.FailIf(lightSwitch.listOfLights.Count, Is.EqualTo(0))
					.AppendLine($"{Scene.name}: \"{lightSwitch.name}\" at {position} has no light sources.");
			}

			Report.AssertPassed();
		}
	}
}