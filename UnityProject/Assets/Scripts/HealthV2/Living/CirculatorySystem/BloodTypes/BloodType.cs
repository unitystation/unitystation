using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry;

public class BloodType : ScriptableObject
{
	[Tooltip("This is the reagent actually metabolised and circulated through this circulatory system.")]
	public Reagent CirculatedReagent;

	[Tooltip("This is the reagent mix that the blood is composed of.")]
	public ReagentMix CompleteBloodMix;

	[Tooltip("The color of this bloodtype.")]
	public Color Color;
}
