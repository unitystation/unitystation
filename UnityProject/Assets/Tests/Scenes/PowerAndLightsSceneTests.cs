using Doors;
using NUnit.Framework;
using Objects.Engineering;
using Objects.Lighting;
using Objects.Wallmounts;
using Systems.Electricity;
using Systems.Scenes.Electricity;

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
				if (device.GetComponentInChildren<AutoAPCLinker>() != null) continue;

				var deviceLocation = device.transform.NameAndPosition();
				var relatedAPC = device.RelatedAPC;

				if (relatedAPC == null)
				{
					Report.Fail().AppendLine($"{Scene.name}: {deviceLocation} has a missing APC reference");
					continue;
				}

				var apcLocation = relatedAPC.transform.NameAndPosition("APC");
				Report.FailIf(relatedAPC.ConnectedDevices.Contains(device) == false)
					.AppendLine($"{Scene.name}: {deviceLocation} is connected to ")
					.AppendLine($"{apcLocation} but the APC doesn't have this device.");
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
				var apcTransform = apc.transform;

				foreach (var (connectedDevice, index) in apc.ConnectedDevices.WithIndex())
				{
					var apcLocation = apcTransform.NameAndPosition("APC");

					if (connectedDevice == null)
					{
						Report.Fail()
							.AppendLine($"{sceneName}: {apcLocation} has a null value in the list at index {index}.");
						continue;
					}

					var relatedAPC = connectedDevice.RelatedAPC;

					Report.FailIfNot(relatedAPC, Is.EqualTo(apc))
						.Append($"{sceneName}: {connectedDevice.transform.NameAndPosition("Device")} ")
						.Append($"is not connected to {apcLocation}.")
						.AppendLine();

					var currentAPC = "nothing";

					if (relatedAPC != null)
					{
						currentAPC = $"{relatedAPC.transform.NameAndPosition()}";
						Report.Append("The APC's devices list may unintentionally contain this device. ");
					}

					Report.Append($"The device is currently connected to {currentAPC}.")
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