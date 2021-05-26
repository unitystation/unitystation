﻿using UnityEngine;

namespace ScriptableObjects
{
	public enum LightMountState
	{
		None = 0,
		On,
		Off,
		MissingBulb,
		Broken,
		Emergency,
		BurnedOut
	}
	[CreateAssetMenu(fileName = "SOLightMountState", menuName = "ScriptableObjects/States/SOLightMountState", order = 0)]
	public class SOLightMountState : UnityEngine.ScriptableObject
	{
		[SerializeField]private SpriteDataSO spriteData = null;
		public SpriteDataSO SpriteData => spriteData;

		[Tooltip("Will drop this item.")]
		[SerializeField]private GameObject tube = null;
		public GameObject Tube => tube;

		[Tooltip("Item with this trait will be put in.")]
		[SerializeField]private ItemTrait traitRequired = null;
		public ItemTrait TraitRequired => traitRequired;

		[Tooltip("On what % of integrity mount changes state.")]
		[Range(0.1f, 0.90f)]
		[SerializeField]private float multiplierIntegrity = 0;
		public float MultiplierIntegrity => multiplierIntegrity;

		[Tooltip("Drops this on destroy.")]
		[SerializeField]private GameObject lootDrop = null;
		public GameObject LootDrop => lootDrop;

		[Tooltip("Light color.")]
		[SerializeField]private Color lightColor = new Color();
		public Color LightColor => lightColor;
	}
}