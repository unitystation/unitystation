using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry;

<<<<<<< HEAD
[CreateAssetMenu(fileName = "BloodType", menuName = "ScriptableObjects/Health/BloodType", order = 0)]
public class BloodType : ScriptableObject
{
	[Tooltip("This is the reagent actually metabolised and circulated through this circulatory system.")]
	public Chemistry.Reagent CirculatedReagent;

	[Tooltip("This is the reagent mix that the blood is composed of.")]
	public Chemistry.ReagentMix CompleteBloodMix;
=======
public class BloodType : ScriptableObject
{
	[Tooltip("This is the reagent actually metabolised and circulated through this circulatory system.")]
	public Reagent CirculatedReagent;

	[Tooltip("This is the reagent mix that the blood is composed of.")]
	public ReagentMix CompleteBloodMix;
>>>>>>> f6fdd9fe97... Initial Commit to Save Progress

	[Tooltip("The color of this bloodtype.")]
	public Color Color;
}
