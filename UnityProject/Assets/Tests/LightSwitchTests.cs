using System.Collections.Generic;
using System.Text;
using Lighting;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class LightSwitchTests
    {
	    [Test]
	    [Ignore("Need to assign everything")]
	    public void FindAll_PoweredDevices_WithoutRelatedAPC()
	    {
		    int count = 0;
		    List<string> devicesWithoutAPC = new List<string>();
		   var listOfDevices =  Resources.FindObjectsOfTypeAll(typeof(APCPoweredDevice));
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
		   Assert.That(count, Is.EqualTo(0),$"APCPoweredDevice count in the scene: {listOfDevices.Length}");
	    }

	    [Test]
	    [Ignore("Need to assign everything")]
	    public void FindAll_LightSources_WithoutRelatedSwitch()
	    {
		    int count = 0;
		    List<string> devicesWithoutAPC = new List<string>();
		    var listOfDevices =  Resources.FindObjectsOfTypeAll(typeof(LightSource));
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
		    Assert.That(count, Is.EqualTo(0),$"LightSource count in the scene: {listOfDevices.Length}");
	    }

	    [Test]
	    [Ignore("Need to assign everything")]
	    public void FindAll_Switches_WithoutLightSources()
	    {
		    int count = 0;
		    List<string> devicesWithoutAPC = new List<string>();
		    var listOfDevices =  Resources.FindObjectsOfTypeAll(typeof(LightSwitchV2));
		    var report = new StringBuilder();
		    Logger.Log("Light switches without Lights", Category.Tests);
		    foreach (var objectDevice in listOfDevices)
		    {
			    var device = objectDevice as LightSwitchV2;
			    if (device.listOfLights.Count == 0)
			    {
				    count++;
				    var obStr = objectDevice.name;
				    devicesWithoutAPC.Add(obStr);
				    Logger.Log(obStr, Category.Tests);
				    report.AppendLine(obStr);
			    }
		    }
		    Assert.That(count, Is.EqualTo(0),$"Switches count in the scene: {listOfDevices.Length}");
	    }

	    [Test]
	    [Ignore("Need to assign everything")]
	    public void CheckAll_SwitchesFor_LightSourcesInTheList()
	    {
		    // Checks if light source from switch list
		    // is assigned to the switch
		    int count = 0;
		    List<string> devicesWithoutAPC = new List<string>();
		    var listOfDevices =  Resources.FindObjectsOfTypeAll(typeof(LightSwitchV2));
		    var report = new StringBuilder();
		    Logger.Log("LightSources without properly defined switch", Category.Tests);
		    foreach (var objectDevice in listOfDevices)
		    {
			    var device = objectDevice as LightSwitchV2;
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
		    Assert.That(count, Is.EqualTo(0),$"APCs count in the scene: {listOfDevices.Length}");
	    }

	    [Test]
	    [Ignore("Need to assign everything")]
	    public void CheckAll_APCsFor_ConnectedDevicesInTheList()
	    {
		    // Checks if powered device from APC list
		    // is assigned to the APC
		    int count = 0;
		    List<string> devicesAPC = new List<string>();
		    var listOfAPCs =  Resources.FindObjectsOfTypeAll(typeof(APC));
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

		    Assert.That(count, Is.EqualTo(0), $"APCs in the scene: {listOfAPCs.Length}");
	    }
    }
}
