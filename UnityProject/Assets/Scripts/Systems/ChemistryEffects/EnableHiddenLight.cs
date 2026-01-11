using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chemistry.Effects
{

	[Serializable]
	[CreateAssetMenu(fileName = "EnableHiddenLight", menuName = "ScriptableObjects/Chemistry/Effect/EnableHiddenLight")]
	public class EnableHiddenLight : Chemistry.Effect //TODO When Sprite handle has a simple adding new sprites game objects make this Use this for Lights
	{
		//public GameObject LightSourcePrefab;

		//public int ScaleSize = 7;

		//public Color Colour = Color.white;
		public override void Apply(MonoBehaviour onObject, ReagentMix ReagentMix,Vector3 WorldPosition, float amount)
		{
			if (onObject == null) return;
			var gameObject =  onObject.gameObject;
			var LightSprites = gameObject.GetComponentsInChildren<MeshRenderer>();
			foreach (var LightSprite in LightSprites)
			{
				LightSprite.gameObject.SetActive(true);
			}
		}
	}
}