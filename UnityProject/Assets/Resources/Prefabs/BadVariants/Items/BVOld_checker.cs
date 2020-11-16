using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Systems.Electricity;
using Doors;
using Lighting;
using Objects;
using Objects.Shuttles;
using Objects.Wallmounts;
using UnityEditor;
using UnityEngine;


//Unsuported map custom edits, will be lost once the object is replaced:
//Ancient temple and palace has a custom chair with a different sprites
//There's a custom bottle with universal enzyme
//Headsets don't have encryption keys and the mapper has to include them manually
//DarkJungle invisible walls

[ExecuteInEditMode]
public class BVOld_checker : MonoBehaviour
{
	public bool HITALL;

	public bool onlyPrintDebugLogs = true;

	public GameObject replacementePrefab;
#if Unity_Editor
	void Start()
	{

		var name = gameObject.name;
		while (name.Contains("BVOld_"))
	    {
		    name = name.Replace("BVOld_", "");
	    }

	    gameObject.name = "BVOld_" + name;
	}


	private void PrepareStuff()
	{
		GeneralSwitch[] generalSwitches = FindObjectsOfType<GeneralSwitch>(); //im not sure if these old switches that handle disposals are connected to a bvold in any map
		foreach (var generalSwitch in generalSwitches)
		{
			foreach (var controlledObject in generalSwitch.generalSwitchControllers)
			{
				var unexpectedBvold = controlledObject.GetComponent<BVOld_checker>();
				if (unexpectedBvold != null)
				{
					Debug.Log($"{gameObject.name} GeneralSwitch actually is connected to a bvold, needs attention");
					return;
				}
			}
		}

		DoorSwitch[] doorSwitchList = FindObjectsOfType<DoorSwitch>();
		foreach (var doorSwitch in doorSwitchList)
		{
			foreach (var door in doorSwitch.DoorControllers)
			{
				if(door != null)
					door.connectedDoorSwitch = doorSwitch;
			}
		}



		BVOld_checker[] components = FindObjectsOfType<BVOld_checker>();
		foreach (var bvoChecker in components)
		{
			var doWeHaveTwo = bvoChecker.GetComponents<BVOld_checker>();
			if (doWeHaveTwo.Length > 1)
			{
				Debug.Log($"Two BVOld_checker detected in {bvoChecker.gameObject.name}");
				continue;
			}
			bvoChecker.RunReplacer(onlyPrintDebugLogs);
		}

		Debug.Log("Fiished bvold replacement loop");
	}


	private void Update()
	{
		if (HITALL)
		{
			Debug.Log("Running bvold replacement");
			HITALL = false;
			PrepareStuff();
		}
	}

	private void RunReplacer(bool onlyDebugLogs)
	{

		//Before replacing this object we have to make sure we know all the changes that it contains inside the map
		var propertyModifications = PrefabUtility.GetPropertyModifications(gameObject).ToList();
		StringBuilder bld = new StringBuilder();
		foreach (var modification in propertyModifications)
		{
			var stringList = new List<string>()
				//if the modification is one of these, we can ignore it as it is expected
				//note: some stuff is really random, like permabrig APC having 'selfdestruct' marked as modified, but still being equal to the original apc prefab
				//in some cases, the variable change references to something that doesnt exist, for example, shutters trying to toggle "IsOpened" on, IsOpened no longer exists
			{
				"m_Name", "LocalPosition", "LocalRotation", "LocalEulerAngles", "sceneId", "SceneId", "hitMe", "RootOrder",
				"InitialDirection", "initialDirection", "LocalScale", "RelatedAPC", "SelfPowered", "relatedSwitch", "ChangeDirectionWithMatrix",
				"relatedLightSwitch", "Offset", "Size", "AssetId", "Sprite", "isWithoutSwitch", "snapToGridOnStart", "listOfLights",
				"ConnectedDevices", "connectedDevices", "layer", "Anchor", "Pivot", "AdvancedControlToScript", "ignorePassableChecks",
				"usedByComposite", "UsedByComposite", "restriction", "Layer", "DoorControllers", "doorControllers", "connectedDoorSwitch",
				"Vendor", "access", "restricted", "HullColor", "IsClosed", "isClosed", "HITALL", "IsActive", "initialDescription", "initialName",
				"draggerMustBeAdjacent", "DisableSyncing", "initialContents", "_Enabled", "m_Materials", "m_Mesh", "shadow", "prefabChildrenOrientation",
				"StateUpdateOnClient", "syncInterval", "StateUpdateOnClient", "m_AutoTiling", "EjectObjects", "EjectDirection", "m_IsTrigger", "willHighlight",
				"PowerMachinery", "isDirty", "IsDirty", "Resistances", "SelfPowerLights", "preventStartUpCache", "generalSwitchControllers", "ConnectedDepartmentBatteries", "connectedDepartmentBatteries",
				"SelfDestruct", "FireLockList", "fireAlarm", "channel", "onEditorDirectionChange", "direction", "ShuttleMatrixMove", "InitialState", "Armor.Melee", "Armor.Bomb", "IsOpened",
				"OneDirectionRestricted", "EncryptionKey", "IsAutomatic", "IncludeAccessDenied", "initialReagentMix", "m_UsedByEffector", "DoesntRequirePower", "isOn", "PowerCut",
				"MinDistance", "MaxDistance", "butcherResults", "State", "TriggeringObjects", "IsFixedMatrix", "isWindowedDoor", "Current", "EncryptionKey", "radius", "m_Flip",
				"Down", "Left", "Up", "Right", "IsLockable", "m_Color", "onlyPrintDebugLogs"


			};
			var specialModification = false;
			foreach (var stringCheck in stringList)
			{
				if (modification.propertyPath.Contains(stringCheck))
				{
					specialModification = false;
					break;
				}
				specialModification = true;
			}
			if (specialModification)
			{
				bld.Append($"//{modification.propertyPath}//");
			}
		}

		if (bld.Length > 0)
		{
			Debug.Log($"{gameObject.name} has an unsupported custom change: {bld}");
			return;
		}



		if (replacementePrefab == null)
		{
			Debug.Log($"{gameObject.name} has nothing to replace");
			return;
		}



		if (onlyDebugLogs)
			return;



		var newObject = PrefabUtility.InstantiatePrefab(replacementePrefab) as GameObject;


		var shuttleConsole = gameObject.GetComponent<ShuttleConsole>();
		var newshuttleConsole = newObject.GetComponent<ShuttleConsole>();
		if (shuttleConsole && newshuttleConsole)
		{
			newshuttleConsole.ShuttleMatrixMove = shuttleConsole.ShuttleMatrixMove;
		}

		var fireLock = gameObject.GetComponent<FireLock>();
		var newfireLock = newObject.GetComponent<FireLock>();
		if (fireLock && newfireLock)
		{
			newfireLock.fireAlarm = fireLock.fireAlarm;
			if (fireLock.fireAlarm)
			{
				fireLock.fireAlarm.FireLockList.Remove(fireLock);
				fireLock.fireAlarm.FireLockList.Add(newfireLock);
			}
		}

		var fireAlarm = gameObject.GetComponent<FireAlarm>();
		var newfireAlarm = newObject.GetComponent<FireAlarm>();
		if (fireAlarm && newfireAlarm)
		{
			foreach (var linkedFireLock in fireAlarm.FireLockList)
			{
				if (linkedFireLock != null)
				{
					newfireAlarm.FireLockList.Add(linkedFireLock);
					linkedFireLock.fireAlarm = newfireAlarm;
				}
			}

		}

		var mouseDraggable = gameObject.GetComponent<MouseDraggable>();
		var newmouseDraggable = newObject.GetComponent<MouseDraggable>();
		if (mouseDraggable && newmouseDraggable)
		{
			newmouseDraggable.shadow = mouseDraggable.shadow;
		}

		var customNetTransform = gameObject.GetComponent<CustomNetTransform>();
		var newcustomNetTransform = newObject.GetComponent<CustomNetTransform>();
		if (customNetTransform && newcustomNetTransform)
		{
			newcustomNetTransform.snapToGridOnStart = customNetTransform.snapToGridOnStart;
		}

		var apcPoweredDevice = gameObject.GetComponent<APCPoweredDevice>();
		var newLightPoweredDevice = newObject.GetComponent<APCPoweredDevice>();

		if (apcPoweredDevice && newLightPoweredDevice)
		{
			newLightPoweredDevice.AdvancedControlToScript = apcPoweredDevice.AdvancedControlToScript;
			if (apcPoweredDevice.IsSelfPowered)
			{
				newLightPoweredDevice.isSelfPowered = true;
			}
			else
			{
				var connectedAPC = apcPoweredDevice.RelatedAPC;
				connectedAPC.ConnectedDevices.Remove(apcPoweredDevice);
				connectedAPC.ConnectedDevices.Add(newLightPoweredDevice);
			}

			newLightPoweredDevice.RelatedAPC = apcPoweredDevice.RelatedAPC;


			var lightSource = gameObject.GetComponent<LightSource>();
			var newLightSource = newObject.GetComponent<LightSource>();
			if (lightSource && newLightSource)
			{
				if (lightSource.isWithoutSwitch)
					newLightSource.isWithoutSwitch = true;
				else
				{
					var relatedSwitch = lightSource.relatedLightSwitch;
					if (relatedSwitch)
					{
						relatedSwitch.listOfLights.Remove(lightSource);
						relatedSwitch.listOfLights.Add(newLightSource);
						newLightSource.relatedLightSwitch = lightSource.relatedLightSwitch;
					}
				}
			}
		}

		var apc = gameObject.GetComponent<Objects.Engineering.APC>();
		var newAPC = newObject.GetComponent<Objects.Engineering.APC>();
		if (apc && newAPC)
		{
			foreach (var device in apc.ConnectedDevices)
			{
				if (device != null)
				{
					newAPC.ConnectedDevices.Add(device);
					device.RelatedAPC = newAPC;
				}
			}
			foreach (var battery in apc.ConnectedDepartmentBatteries)
			{
				if(battery != null)
					newAPC.ConnectedDepartmentBatteries.Add(battery);
			}
		}

		var lightSwitch = gameObject.GetComponent<LightSwitchV2>();
		var newlightSwitch = newObject.GetComponent<LightSwitchV2>();
		if (lightSwitch && newlightSwitch)
		{
			foreach (var light in lightSwitch.listOfLights)
			{
				if(light == null)
					continue;
				newlightSwitch.listOfLights.Add(light);
				light.relatedLightSwitch = newlightSwitch;
			}
		}


		var directional = gameObject.GetComponent<Directional>();
		var newDirectional = newObject.GetComponent<Directional>();
		if (directional && newDirectional)
		{
			newDirectional.InitialDirection = directional.InitialDirection;
			newDirectional.ChangeDirectionWithMatrix = directional.ChangeDirectionWithMatrix;

			var directionalRotatesParent = gameObject.GetComponent<DirectionalRotatesParent>();
			var newdirectionalRotatesParent = newObject.GetComponent<DirectionalRotatesParent>();
			if (directionalRotatesParent && newdirectionalRotatesParent)
			{
				newdirectionalRotatesParent.prefabChildrenOrientation = directionalRotatesParent.prefabChildrenOrientation;
			}
		}

		var closetControl = gameObject.GetComponent<ClosetControl>();
		var newclosetControl = newObject.GetComponent<ClosetControl>();
		if (closetControl && newclosetControl)
		{
			newclosetControl.initialContents = closetControl.initialContents;
			newclosetControl.IsLockable = closetControl.IsLockable;
		}

		var registerDoor = gameObject.GetComponent<RegisterDoor>();
		var newregisterDoor = newObject.GetComponent<RegisterDoor>();
		if (registerDoor && newregisterDoor)
		{
			newregisterDoor.IsClosed = registerDoor.IsClosed;
		}

		var doorController = gameObject.GetComponent<DoorController>();
		var newDoorController = newObject.GetComponent<DoorController>();
		if (doorController && newDoorController)
		{
			newDoorController.connectedDoorSwitch = doorController.connectedDoorSwitch;
			newDoorController.ignorePassableChecks = doorController.ignorePassableChecks;
			if (newDoorController.connectedDoorSwitch)
			{
				newDoorController.connectedDoorSwitch.DoorControllers.Remove(doorController);
				newDoorController.connectedDoorSwitch.DoorControllers.Add(newDoorController);
			}
		}

		var doorSwitch = gameObject.GetComponent<DoorSwitch>();
		var newdoorSwitch = newObject.GetComponent<DoorSwitch>();
		if (doorSwitch && newdoorSwitch)
		{
			newdoorSwitch.restricted = doorSwitch.restricted;
			newdoorSwitch.access = doorSwitch.access;
			foreach (var door in doorSwitch.DoorControllers)
			{
				newdoorSwitch.DoorControllers.Add(door);
				door.connectedDoorSwitch = newdoorSwitch;
			}
		}

		var accessRestriction = gameObject.GetComponent<AccessRestrictions>();
		var newaccessRestriction = newObject.GetComponent<AccessRestrictions>();
		if (accessRestriction && newaccessRestriction)
		{
			newaccessRestriction.restriction = accessRestriction.restriction;
		}

		var objectAttributes = gameObject.GetComponent<ObjectAttributes>();
		var newObjectAttributes = newObject.GetComponent<ObjectAttributes>();
		if(objectAttributes && newObjectAttributes)
		{
			newObjectAttributes.initialName = objectAttributes.InitialName;
			newObjectAttributes.initialDescription = objectAttributes.InitialDescription;
		}

		var generalSwitch = gameObject.GetComponent<GeneralSwitch>();
		var newgeneralSwitch = newObject.GetComponent<GeneralSwitch>();
		if (generalSwitch && newgeneralSwitch)
		{
			foreach (var controlledObject in generalSwitch.generalSwitchControllers)
			{
				newgeneralSwitch.generalSwitchControllers.Add(controlledObject);
			}
		}

		var vendor = gameObject.GetComponent<Vendor>();
		var newvendor = newObject.GetComponent<Vendor>();
		if (vendor && newvendor)
		{
			newvendor.EjectDirection = vendor.EjectDirection;
			newvendor.EjectObjects = vendor.EjectObjects;
			newvendor.DoesntRequirePower = vendor.DoesntRequirePower;
		}

		var integrity = gameObject.GetComponent<Integrity>();
		var newIntegrity = newObject.GetComponent<Integrity>();
		if (integrity && newIntegrity)
		{
			newIntegrity.Armor.Melee = integrity.Armor.Melee;
			newIntegrity.Armor.Bomb = integrity.Armor.Bomb;
			newIntegrity.Resistances.LavaProof = integrity.Resistances.LavaProof;
			newIntegrity.Resistances.FireProof = integrity.Resistances.FireProof;
			newIntegrity.Resistances.Flammable = integrity.Resistances.Flammable;
			newIntegrity.Resistances.UnAcidable = integrity.Resistances.UnAcidable;
			newIntegrity.Resistances.AcidProof = integrity.Resistances.AcidProof;
			newIntegrity.Resistances.Indestructable = integrity.Resistances.Indestructable;
			newIntegrity.Resistances.FreezeProof = integrity.Resistances.FreezeProof;
		}


		newObject.transform.SetParent(gameObject.transform.parent);
		newObject.transform.position = gameObject.transform.position;

		DestroyImmediate(gameObject);
	}
#endif
}
