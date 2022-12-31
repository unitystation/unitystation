using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using NaughtyAttributes;

[CreateAssetMenu(fileName = "SpriteData", menuName = "ScriptableObjects/SpriteData")]
public class SpriteDataSO : ScriptableObject
{
	public List<Variant> Variance = new List<Variant>();
	public bool IsPalette = false;

	[NonSerialized] public int SetID = -1;

	public string DisplayName;

	[Serializable]
	public struct Variant
	{
		public List<Frame> Frames;
	}


	[Serializable]
	public class Frame
	{
		public Sprite sprite;
		public float secondDelay;
	}
}
