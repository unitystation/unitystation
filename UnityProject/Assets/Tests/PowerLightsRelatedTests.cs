using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lighting;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tests
{
    public class PowerLightsRelatedTests
    {
	    [Test]
	    [Ignore("For one current scene only")]
	    public void CheckAll_SwitchesFor_LightSourcesInTheList()
	    {

		    int count = 0;
		    List<string> devicesWithoutAPC = new List<string>();
		    var listOfDevices =  GetAllLightSwitchesInTheScene();
		    var report = new StringBuilder();
		    Logger.Log("LightSources without properly defined switch", Category.Tests);
		    foreach (var objectDevice in listOfDevices)
		    {
			    var device = objectDevice;
			    if (device.listOfLights.Count == 0)
			    {
				    continue;
			    }
			    foreach (var light in device.listOfLights)
			    {
				    if (light.relatedLightSwitch != device)
				    {
					    string obStr = light.name;
					    devicesWithoutAPC.Add(obStr);
					    string lightSwitch = light.relatedLightSwitch == null ? "null" : light.relatedLightSwitch.name;
					    Logger.Log($"\"{obStr}\" relatedSwitch is \"{lightSwitch}\", supposed to be \"{device.name}\"", Category.Tests);
					    report.AppendLine(obStr);
					    count++;
				    }
			    }
		    }
		    Assert.That(count, Is.EqualTo(0),$"APCs count in the scene: {listOfDevices.Count}");
	    }


	    /// <summary>
	    /// Checks only scenes selected for build
	    /// Finds all powered devices
	    /// Checks if they are assigned to APC
	    /// </summary>
	    [Test]
	    public void CheckAllScenes_ForAPCPoweredDevices_WhichMissRelatedAPCs()
	    {
		    var buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled);
		    var missingAPCinDeviceReport = new List<(string, string)>();
		    int countMissingAPC = 0;
		    int countSelfPowered = 0;
		    int countAll = 0;

		    foreach (var scene in buildScenes)
		    {
			    var currentScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
			    var currentSceneName = currentScene.name;

			    var listOfDevices =  GetAllPoweredDevicesInTheScene();
			    foreach (var objectDevice in listOfDevices)
			    {
				    countAll++;
				    var device = objectDevice;
				    if (device.IsSelfPowered)
				    {
					    countSelfPowered++;
					    continue;
				    }
				    if (device.RelatedAPC == null && device.SelfPowered == false)
				    {
					    countMissingAPC++;
					    missingAPCinDeviceReport.Add((currentSceneName,objectDevice.name));
				    }
			    }
		    }

		    // Form report about missing components
		    var report = new StringBuilder();
		    foreach (var s in missingAPCinDeviceReport)
		    {
			    var missingComponentMsg = $"{s.Item1}: \"{s.Item2}\" miss APC reference.";
			    report.AppendLine(missingComponentMsg);
		    }

		    Logger.Log($"All devices count: {countAll}", Category.Tests);
		    Logger.Log($"Self powered Devices count: {countSelfPowered}", Category.Tests);
		    Logger.Log($"Devices count which miss APCs: {countMissingAPC}", Category.Tests);
		    Assert.IsEmpty(missingAPCinDeviceReport, report.ToString());
	    }

	    List<APCPoweredDevice> GetAllPoweredDevicesInTheScene()
	    {
		    List<APCPoweredDevice> objectsInScene = new List<APCPoweredDevice>();

		    foreach (APCPoweredDevice go in Resources.FindObjectsOfTypeAll(typeof(APCPoweredDevice)))
		    {
			    if (!EditorUtility.IsPersistent(go.transform.root.gameObject) && !(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave))
				    objectsInScene.Add(go);
		    }

		    return objectsInScene;
	    }

	    [Test]
	    [Ignore("For one current scene only")]
	    public void FindAll_PoweredDevices_WithoutRelatedAPC()
	    {
		    int count = 0;
		    List<string> devicesWithoutAPC = new List<string>();
		    var listOfDevices =  GetAllPoweredDevicesInTheScene();
		    var report = new StringBuilder();
		    Logger.Log("Powered Devices without APC", Category.Tests);
		    foreach (var objectDevice in listOfDevices)
		    {
			    var device = objectDevice as APCPoweredDevice;
			    if(device.IsSelfPowered) continue;
			    if (device.RelatedAPC == null)
			    {
				    count++;
				    var obStr = objectDevice.name;
				    devicesWithoutAPC.Add(obStr);
				    Logger.Log(obStr, Category.Tests);
				    report.AppendLine(obStr);
			    }
		    }
		    Assert.That(count, Is.EqualTo(0),$"APCPoweredDevice count in the scene: {listOfDevices.Count}");
	    }

	    /// <summary>
	    /// Checks scenes selected for the build
	    /// Finds all lights sources
	    /// Checks if they are without a switch by default
	    /// or if they are assigned to switch
	    /// </summary>
	    [Test]
	    public void CheckAllScenes_ForLightSources_WhichMissRelatedSwitches()
	    {
		    var buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled);
		    var missingAPCinDeviceReport = new List<(string, string)>();
		    int countMissingSwitch = 0;
		    int countWithoutSwitches = 0;
		    int countAll = 0;

		    foreach (var scene in buildScenes)
		    {
			    var currentScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
			    var currentSceneName = currentScene.name;

			    var listOfDevices = GetAllLightSourcesInTheScene();
			    foreach (var objectDevice in listOfDevices)
			    {
				    countAll++;
				    var device = objectDevice;
				    if (device.IsWithoutSwitch)
				    {
					    countWithoutSwitches++;
					    continue;
				    }
				    if (device.relatedLightSwitch == null)
				    {
					    countMissingSwitch++;
					    missingAPCinDeviceReport.Add((currentSceneName,objectDevice.name));
				    }
			    }
		    }

		    // Form report about missing components
		    var report = new StringBuilder();
		    foreach (var s in missingAPCinDeviceReport)
		    {
			    var missingComponentMsg = $"{s.Item1}: \"{s.Item2}\" miss switch reference.";
			    report.AppendLine(missingComponentMsg);
		    }

		    Logger.Log($"All Light Sources count: {countAll}", Category.Tests);
		    Logger.Log($"Without switches Light Sources count: {countWithoutSwitches}", Category.Tests);
		    Logger.Log($"With missing switch reference: {countMissingSwitch}", Category.Tests);
		    Assert.IsEmpty(missingAPCinDeviceReport, report.ToString());
	    }

	    private List<LightSource> GetAllLightSourcesInTheScene()
	    {
		    List<LightSource> objectsInScene = new List<LightSource>();

		    foreach (LightSource go in Resources.FindObjectsOfTypeAll(typeof(LightSource)))
		    {
			    if (!EditorUtility.IsPersistent(go.transform.root.gameObject) && !(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave))
				    objectsInScene.Add(go);
		    }

		    return objectsInScene;
	    }

	    [Test]
	    [Ignore("For one current scene only")]
	    public void FindAll_LightSources_WithoutRelatedSwitch()
	    {
		    int count = 0;
		    List<string> devicesWithoutAPC = new List<string>();
		    var listOfDevices =  GetAllLightSourcesInTheScene();
		    var report = new StringBuilder();
		    Logger.Log("LightSource without Switches", Category.Tests);
		    foreach (var objectDevice in listOfDevices)
		    {

			    var device = objectDevice as LightSource;
			    if(device.IsWithoutSwitch) continue;
			    if (device.relatedLightSwitch == null)
			    {
				    count++;
				    var obStr = objectDevice.name;
				    devicesWithoutAPC.Add(obStr);
				    Logger.Log(obStr, Category.Tests);
				    report.AppendLine(obStr);
			    }
		    }
		    Assert.That(count, Is.EqualTo(0),$"LightSource count in the scene: {listOfDevices.Count}");
	    }

	    /// <summary>
	    /// Checks scenes selected for the build
	    /// Finds all lights switches
	    /// Checks if the switch has an empty list
	    /// </summary>
	    [Test]
	    public void CheckAllScenes_ForLightSwitchesLists_WhichMissLightSources()
	    {
		    var buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled);
		    var missingAPCinDeviceReport = new List<(string, string)>();
		    int countSwitchesWithoutLights = 0;
		    int countAll = 0;

		    foreach (var scene in buildScenes)
		    {
			    var currentScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
			    var currentSceneName = currentScene.name;

			    var listOfDevices = GetAllLightSwitchesInTheScene();
			    foreach (var objectDevice in listOfDevices)
			    {
				    countAll++;
				    var device = objectDevice;
				    if (device.listOfLights.Count == 0)
				    {
					    countSwitchesWithoutLights++;
					    missingAPCinDeviceReport.Add((currentSceneName,objectDevice.name));
				    }
			    }
		    }

		    // Form report about missing components
		    var report = new StringBuilder();
		    foreach (var s in missingAPCinDeviceReport)
		    {
			    var missingComponentMsg = $"{s.Item1}: \"{s.Item2}\" miss switch reference.";
			    report.AppendLine(missingComponentMsg);
		    }

		    Logger.Log($"All Light Switches count: {countAll}", Category.Tests);
		    Logger.Log($"Switches with empty lists of lights: {countSwitchesWithoutLights}", Category.Tests);
		    Assert.IsEmpty(missingAPCinDeviceReport, report.ToString());
	    }

	    List<LightSwitchV2> GetAllLightSwitchesInTheScene()
	    {
		    List<LightSwitchV2> objectsInScene = new List<LightSwitchV2>();

		    foreach (LightSwitchV2 go in Resources.FindObjectsOfTypeAll(typeof(LightSwitchV2)))
		    {
			    if (!EditorUtility.IsPersistent(go.transform.root.gameObject) &&
			        !(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave))
				    objectsInScene.Add(go);
		    }

		    return objectsInScene;
	    }

	    [Test]
	    [Ignore("For one current scene only")]
	    public void FindAll_Switches_WithoutLightSources()
	    {
		    int count = 0;
		    List<string> devicesWithoutAPC = new List<string>();
		    var listOfDevices =  GetAllLightSwitchesInTheScene();
		    var report = new StringBuilder();
		    Logger.Log("Light switches without Lights", Category.Tests);
		    foreach (var objectDevice in listOfDevices)
		    {
			    var device = objectDevice;
			    if (device.listOfLights.Count == 0)
			    {
				    count++;
				    var obStr = objectDevice.name;
				    devicesWithoutAPC.Add(obStr);
				    Logger.Log(obStr, Category.Tests);
				    report.AppendLine(obStr);
			    }
		    }
		    Assert.That(count, Is.EqualTo(0),$"Switches count in the scene: {listOfDevices.Count}");
	    }

	    /// <summary>
	    /// Checks scenes selected for the build
	    /// Finds all APCs
	    /// Checks if Device list is empty
	    /// if there are null values in the list
	    /// if device is not assigned to this APC
	    /// </summary>
	    [Test]
	    public void CheckAllScenes_ForAPCs_ConnectedDevicesInTheList()
	    {
		    var buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled);
		    var listAPCsWrongDevices = new List<(string, string,string)>();
		    var listAPCsWithNulls = new List<(string, string)>();
		    var listAPCWithEmptyList = new List<(string, string)>();

		    int countAPCsWithEmptyLists = 0;
		    int countAPCsWithNullDevices = 0;
		    int countAPCsWithBadlyAssignedDevices = 0;
		    int countAll = 0;

		    foreach (var scene in buildScenes)
		    {
			    var currentScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
			    var currentSceneName = currentScene.name;

			    var listOfDevices = GetAllAPCsInTheScene();
			    foreach (var device in listOfDevices)
			    {
				    countAll++;
				    if (device.ConnectedDevices.Count == 0)
				    {
					    listAPCWithEmptyList.Add((currentSceneName,device.name));
					    countAPCsWithEmptyLists++;
					    continue;
				    }
				    foreach (var connectedDevice in device.ConnectedDevices)
				    {
					    if (connectedDevice == null)
					    {
						    listAPCsWithNulls.Add((currentSceneName,device.name));
						    countAPCsWithNullDevices++;
					    }
					    else
					    if (connectedDevice.RelatedAPC != device)
					    {
						    listAPCsWrongDevices.Add((currentSceneName,device.name, connectedDevice.name));
						    countAPCsWithBadlyAssignedDevices++;
					    }
				    }
			    }
		    }


		    var report = new StringBuilder();
		    foreach (var s in listAPCWithEmptyList)
		    {
			    var missingComponentMsg = $"{s.Item1}: \"{s.Item2}\" has an empty list of devices.";
			    report.AppendLine(missingComponentMsg);
		    }
		    foreach (var s in listAPCsWithNulls)
		    {
			    var missingComponentMsg = $"{s.Item1}: \"{s.Item2}\" has a null value in the list.";
			    report.AppendLine(missingComponentMsg);
		    }
		    foreach (var s in listAPCsWrongDevices)
		    {
			    var missingComponentMsg = $"{s.Item1}: \"{s.Item3}\" is not assigned to \"{s.Item2}\"";
			    report.AppendLine(missingComponentMsg);
		    }

		    Logger.Log($"All APCs count: {countAll}", Category.Tests);
		    Logger.Log($"APCs with empty lists count: {countAPCsWithEmptyLists}", Category.Tests);
		    Logger.Log($"APCs with nulls in the list count: {countAPCsWithNullDevices}", Category.Tests);
		    Logger.Log($"APCs with not assigned devices to it count: {countAPCsWithBadlyAssignedDevices}", Category.Tests);

		    Assert.IsEmpty(listAPCWithEmptyList, report.ToString());
		    Assert.IsEmpty(listAPCsWithNulls, report.ToString());
		    Assert.IsEmpty(listAPCsWrongDevices, report.ToString());
	    }

	    List<APC> GetAllAPCsInTheScene()
	    {
		    List<APC> objectsInScene = new List<APC>();

		    foreach (APC go in Resources.FindObjectsOfTypeAll(typeof(APC)))
		    {
			    if (!EditorUtility.IsPersistent(go.transform.root.gameObject) &&
			        !(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave))
				    objectsInScene.Add(go);
		    }

		    return objectsInScene;
	    }

	    [Test]
	    [Ignore("For one current scene only")]
	    public void CheckAll_APCsFor_ConnectedDevicesInTheList()
	    {
		    // Checks if powered device from APC list
		    // is assigned to the APC
		    int count = 0;
		    List<string> devicesAPC = new List<string>();
		    var listOfAPCs = GetAllAPCsInTheScene();
		    var report = new StringBuilder();
		    Logger.Log("Devices without properly defined APC", Category.Tests);
		    foreach (var apc in listOfAPCs)
		    {
			    var device = apc as APC;
			    if (device.ConnectedDevices.Count == 0)
			    {
				    continue;
			    }
			    foreach (var connectedDevice in device.ConnectedDevices)
			    {
				    if (connectedDevice == null)
				    {
					    devicesAPC.Add(device.name);
					    Logger.Log($"ConnectedDevice is null in \"{device.name}\"", Category.Tests);
					    report.AppendLine(device.name);
					    count++;
				    }
				    else
				    if (connectedDevice.RelatedAPC != device)
				    {
					    string obStr = connectedDevice.name;
					    devicesAPC.Add(obStr);
					    string apcStr = connectedDevice.RelatedAPC == null ? "null" : connectedDevice.RelatedAPC.name;
					    Logger.Log($"\"{obStr}\" RelatedAPC is \"{apcStr}\", supposed to be \"{device.name}\"", Category.Tests);
					    report.AppendLine(obStr);
					    count++;
				    }
			    }
		    }

		    Assert.That(count, Is.EqualTo(0), $"APCs in the scene: {listOfAPCs.Count}");
	    }
    }
}
