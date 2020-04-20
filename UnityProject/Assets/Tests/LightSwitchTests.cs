using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lighting;
using Lucene.Net.Util;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class LightSwitchTests
    {
	    [Test]
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
			   if (device.RelatedAPC == null)
			   {
				   count++;
				   var obStr = objectDevice.name;
				   devicesWithoutAPC.Add(obStr);
				   //Logger.Log(obStr, Category.Tests);
				   report.AppendLine(obStr);
			   }
		   }
		   Assert.That(count, Is.EqualTo(listOfDevices.Length));
	    }

	    [Test]
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
			    if (device.relatedLightSwitch == null)
			    {
				    count++;
				    var obStr = objectDevice.name;
				    devicesWithoutAPC.Add(obStr);
				    //Logger.Log(obStr, Category.Tests);
				    report.AppendLine(obStr);
			    }
		    }
		    Assert.That(count, Is.EqualTo(listOfDevices.Length));
	    }

	    [Test]
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
				    //Logger.Log(obStr, Category.Tests);
				    report.AppendLine(obStr);
			    }
		    }
		    Assert.That(count, Is.EqualTo(listOfDevices.Length));
	    }

	    [Test]
	    public void CheckAll_SwitchesFor_LightSourcesInTheList()
	    {
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
		    Assert.That(count, Is.EqualTo(listOfDevices.Length));
	    }

	    [Test]
	    public void LightMountState_Test()
	    {
		    GameObject variableForPrefab = (GameObject)Resources.Load("Prefabs/Objects/WallProtrusions/LightTubeMount", typeof(GameObject));
		    var lightMountState = variableForPrefab.GetComponent<LightMountStates>();
		    lightMountState.EnsureInit();
		    var lightSource = variableForPrefab.GetComponent<LightSource>();

		    lightMountState.SwitchChangeState(false);
		    lightSource.Trigger(false);

		    Assert.That(LightMountState.Off, Is.EqualTo(lightMountState.State));
	    }

	    /*[Test]
        public void LightSwitchEventTest()
        {
	        GameObject gameObject  = new GameObject();
	        gameObject.AddComponent<LightSource>();
	        gameObject.AddComponent<LightSwitchV2>();
	        var lightSource = gameObject.GetComponent<LightSource>();
	        var lightSwitchV2 = gameObject.GetComponent<LightSwitchV2>();

	        lightSource.SubscribeToSwitch(ref lightSwitchV2.switchTriggerEvent);

	        lightSwitchV2.switchTriggerEvent.Invoke(false);

	        Assert.That(lightSource.SwitchState, Is.False);
        }
        [Test]
        public void LightSwitchEventTestUnsubscribe()
        {
	        GameObject gameObjectSwitch  = new GameObject();
	        gameObjectSwitch.AddComponent<LightSwitchV2>();
	        var lightSwitchV2 = gameObjectSwitch.GetComponent<LightSwitchV2>();

	        GameObject Light2  = new GameObject();
	        Light2.AddComponent<LightSource>();
	        var light2 = Light2.GetComponent<LightSource>();
	        light2.SubscribeToSwitch(ref lightSwitchV2.switchTriggerEvent);

	        GameObject Light3  = new GameObject();
	        Light3.AddComponent<LightSource>();
	        var light3 = Light3.GetComponent<LightSource>();
	        light3.SubscribeToSwitch(ref lightSwitchV2.switchTriggerEvent);

	        GameObject Light4  = new GameObject();
	        Light4.AddComponent<LightSource>();
	        var light4 = Light2.GetComponent<LightSource>();
	        light4.SubscribeToSwitch(ref lightSwitchV2.switchTriggerEvent);

	        lightSwitchV2.switchTriggerEvent.Invoke(true);
	        light3.UnSubscribeFromSwitch(ref lightSwitchV2.switchTriggerEvent);

	        lightSwitchV2.switchTriggerEvent.Invoke(false);

	        Assert.That(light2.SwitchState, Is.False);
	        Assert.That(light3.SwitchState, Is.True);
	        Assert.That(light4.SwitchState, Is.False);
        }

        [Test]
        public void LightMountStatesTest()
        {
	        GameObject lightSourceObject = new GameObject();
	        lightSourceObject.AddComponent<LightSource>();
	        lightSourceObject.AddComponent<LightMountStates>();
	        lightSourceObject.AddComponent<Integrity>();
	        lightSourceObject.AddComponent<Directional>();
	        var lightSource = lightSourceObject.GetComponent<LightSource>();
	        lightSource.EnsureInit();
	        var lightMountState = lightSourceObject.GetComponent<LightMountStates>();
	        lightMountState.EnsureInit();

	        GameObject gameObject  = new GameObject();
	        gameObject.AddComponent<LightSwitchV2>();
	        var lightSwitchV2 = gameObject.GetComponent<LightSwitchV2>();


	        lightSource.SubscribeToSwitch(ref lightSwitchV2.switchTriggerEvent);

	        lightSwitchV2.switchTriggerEvent.Invoke(false);

	        Assert.That(lightSource.SwitchState, Is.False);
	        //Assert.That(lightMountState.State, Is.EqualTo(LightMountState.Off));
        }*/

    }
}
