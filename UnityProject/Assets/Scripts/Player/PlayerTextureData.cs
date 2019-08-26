using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerTextureData", menuName = "ScriptableObjects_PlayerTextureData", order = 1)]
public class PlayerTextureData : ScriptableObject
{
	public RaceVariantTextureData Base;
	public RaceVariantTextureData Male;
	public RaceVariantTextureData Female;
	public List<RaceVariantTextureData> Other;
}

[System.Serializable]
public class RaceVariantTextureData
{
	public SpriteSheetAndData Head;
	public SpriteSheetAndData Eyes;
	public SpriteSheetAndData Torso;
	public SpriteSheetAndData ArmRight;
	public SpriteSheetAndData ArmLeft;
	public SpriteSheetAndData HandRight;
	public SpriteSheetAndData HandLeft;
	public SpriteSheetAndData LegRight;
	public SpriteSheetAndData LegLeft;
}
