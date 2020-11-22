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
	public GameObject Eyes;
	public GameObject Torso;
	public GameObject ArmRight;
	public GameObject ArmLeft;
	public GameObject HandRight;
	public GameObject HandLeft;
	public GameObject LegRight;
	public GameObject LegLeft;
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