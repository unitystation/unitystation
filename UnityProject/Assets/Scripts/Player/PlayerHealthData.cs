using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using Chemistry;
using HealthV2;

[CreateAssetMenu(fileName = "PlayerHealthData", menuName = "ScriptableObjects/Health/PlayerHealthData", order = 1)]
public class PlayerHealthData : ScriptableObject
{
	public RaceHealthData Base;
}

[System.Serializable]
public class ObjectList
{
	public List<GameObject> Elements = new List<GameObject>();
}

[System.Serializable]
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

	public List<HealthV2.BodyPart> BodyPartsThatShareTheSkinTone = new List<HealthV2.BodyPart>();

	public float NumberOfMinutesBeforeStarving = 30f;

	public float TotalToxinGenerationPerSecond = 0.1f;

	[Tooltip(" How much does medicine get metabolised by body parts That are internal and don't contribute to  overall health ")]
	public float InternalMetabolismPerSecond = 1f;

	[Tooltip(" How much does medicine get metabolised by body parts that contribute to overall health ")]
	public float ExternalMetabolismPerSecond = 2f;


	[Tooltip("What does this live off?, Sets all the body parts that don't have a set nutriment")]
	public Reagent BodyNutriment;

	[Tooltip("What reagent does this expel as waste?, Sets all the body parts that don't have a set NaturalToxinReagent")]
	public Reagent BodyNaturalToxinReagent;

	[Tooltip("The text that indicates that it's a clue of what species did an interaction for the detectives scanner")]
	public string ClueString;

	public GameObject MeatProduce;
	public GameObject SkinProduce;
	public bool CanShowUpInTheCharacterCreatorScreen = true;
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