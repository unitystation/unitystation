using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System;

[CreateAssetMenu(fileName = "PlayerHealthData", menuName = "ScriptableObjects/PlayerHealthData", order = 1)]
public class PlayerHealthData : ScriptableObject
{
	public RaceHealthData Base;
}

[System.Serializable]
public class RaceHealthData
{
	public GameObject Head;
	public GameObject Eyes; //Implant should be in head damage area redirected
	public GameObject Torso;
	public GameObject ArmRight;
	public GameObject ArmLeft;
	public GameObject HandRight; //idk
	public GameObject HandLeft; //idk
	public GameObject LegRight;
	public GameObject LegLeft;

	public List<CustomisationAllowedSetting> CustomisationSettings = new List<CustomisationAllowedSetting>();

	public BodyTypeSettings bodyTypeSettings = new BodyTypeSettings();

}


[System.Serializable]
public class CustomisationAllowedSetting
{
	public CustomisationGroup CustomisationGroup;
	public List<PlayerCustomisationData> Blacklist = new List<PlayerCustomisationData>();
}

[System.Serializable]
public class BodyTypeSettings
{
	public List<BodyTypeName> AvailableBodyTypes = new List<BodyTypeName>();
}

[System.Serializable]
public class BodyTypeName
{
	public BodyType bodyType;
	public string Name;
}
/*public enum BodyPartSpriteName
{
	Null,
	Head,
	Eyes,
	Torso,
	ArmRight, ArmLeft,
	HandRight, HandLeft,
	LegRight, LegLeft,
}*/