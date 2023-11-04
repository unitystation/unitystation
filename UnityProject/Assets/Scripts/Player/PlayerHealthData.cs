using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using Chemistry;
using Core.Editor.Attributes;
using HealthV2;
using HealthV2.Living.PolymorphicSystems;
using UnityEngine.Serialization;

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
	public bool allowedToChangeling = false;

	public List<CustomisationAllowedSetting> CustomisationSettings = new List<CustomisationAllowedSetting>();

	public BodyTypeSettings bodyTypeSettings = new BodyTypeSettings();

	public List<Color> SkinColours = new List<Color>();

	public ImplantProcedure RootImplantProcedure;

	public List<HealthV2.BodyPart> BodyPartsThatShareTheSkinTone = new List<HealthV2.BodyPart>();

	[Tooltip("The text that indicates that it's a clue of what species did an interaction for the detectives scanner")]
	public string ClueString;

	public GameObject MeatProduce;
	public GameObject SkinProduce;
	[FormerlySerializedAs("CanShowUpInTheCharacterCreatorScreen")] public bool CanBePlayerChosen = true;

	public SpriteDataSO PreviewSprite;

	[SerializeReference, SelectImplementation(typeof(HealthSystemBase))] public List<HealthSystemBase> SystemSettings = new List<HealthSystemBase>();

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