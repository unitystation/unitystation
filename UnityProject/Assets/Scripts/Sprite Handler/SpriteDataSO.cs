using System.Collections;
using System.Collections.Generic;
using MLAgents.CommunicatorObjects;
using UnityEngine;


[CreateAssetMenu(fileName = "SpriteData", menuName = "ScriptableObjects/SpriteData")]
public class SpriteDataSO : ScriptableObject
{
	public List<Variant> Variance = new List<Variant>();
	public bool IsPalette = false;
	public uint ID;
	[System.Serializable]
	public class Variant
	{
		public List<Frame> Frames = new List<Frame>();
	}


	[System.Serializable]
	public class Frame
	{
		public Sprite sprite;
		public float secondDelay;
	}
}