using System;
using HealthV2;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHealthData", menuName = "ScriptableObjects/PlayerHealthData", order = 1)]
public class PlayerHealthData : ScriptableObject
{
    public RaceHealthData Base;
}

[Serializable]
public class ObjectList
{
    public List<GameObject> Elements = new List<GameObject>();
}

[Serializable]
public class RaceHealthData
{
    public ObjectList Head;
    public ObjectList Torso;
    public ObjectList ArmRight;
    public ObjectList ArmLeft;
    public ObjectList LegRight;
    public ObjectList LegLeft;

    public List<CustomisationAllowedSetting> CustomisationSettings = new List<CustomisationAllowedSetting>();

    public BodyTypeSettings bodyTypeSettings = new BodyTypeSettings();

    public List<Color> SkinColours = new List<Color>();

    public BloodType BloodType;

    public ImplantProcedure RootImplantProcedure;

    public List<BodyPart> BodyPartsThatShareTheSkinTone = new List<BodyPart>();
}

[Serializable]
public class CustomisationAllowedSetting
{
    public CustomisationGroup CustomisationGroup;
    public List<PlayerCustomisationData> Blacklist = new List<PlayerCustomisationData>();
}

[Serializable]
public class BodyTypeSettings
{
    public List<BodyTypeName> AvailableBodyTypes = new List<BodyTypeName>();
}

[Serializable]
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