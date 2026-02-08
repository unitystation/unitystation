using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Clothing;
using US13.Core.Attributes;
using US13.HealthV2.Living;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.CirculatorySystem;
using US13.HealthV2.Living.PolymorphicSystems;
using US13.HealthV2.Living.Surgery.Procedures;
using US13.Items.Traits;
using US13.UI.Systems.Lobby;

namespace US13.Player
{
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

		public List<BodyPart> BodyPartsThatShareTheSkinTone = new List<BodyPart>();

		[Tooltip("The text that indicates that it's a clue of what species did an interaction for the detectives scanner")]
		public string ClueString;

		public GameObject MeatProduce;
		public GameObject SkinProduce;
		public ItemTrait SkinningItemTrait;

		public bool CanBeEmagged = false;

		[FormerlySerializedAs("CanShowUpInTheCharacterCreatorScreen")] public bool CanBePlayerChosen = true;

		public SpriteDataSO PreviewSprite;

		[SerializeReference, SelectImplementation(typeof(HealthSystemBase))] public List<HealthSystemBase> SystemSettings = new List<HealthSystemBase>();

		public List<MutationSO> StartingMutations = new List<MutationSO>();
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
}