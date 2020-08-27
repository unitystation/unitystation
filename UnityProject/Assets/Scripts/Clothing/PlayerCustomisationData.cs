using UnityEngine;

/// <summary>
/// This class holds all information for a customisation option which is used in the character creator
/// </summary>
[CreateAssetMenu(fileName = "PlayerCustomisationData", menuName = "ScriptableObjects/PlayerCustomisationData", order = 1)]
public class PlayerCustomisationData : ScriptableObject
{
	public SpriteDataSO SpriteEquipped;
	public string Name;
	public CustomisationType Type;
	public Gender gender = Gender.Neuter;
}

/// <summary>
/// The types of customisation data
/// </summary>
public enum CustomisationType
{
	Null = 0,
	FacialHair = 1,
	HairStyle = 2,
	Underwear = 3,
	Undershirt = 4,
	Socks = 5,
	BodySprites = 6,
	//Others as needed,
}