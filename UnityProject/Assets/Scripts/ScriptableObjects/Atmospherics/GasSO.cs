﻿using System;
using Systems.Atmospherics;
using Chemistry;
using TileManagement;
using UnityEngine;

namespace ScriptableObjects.Atmospherics
{
	[CreateAssetMenu(fileName = "GasSO", menuName = "ScriptableObjects/Atmos/GasSO")]
	public class GasSO : SOTracker
	{
		public override SpriteDataSO Sprite => PreviewSprite;

		public SpriteDataSO PreviewSprite;

		//This is how many Joules are needed to raise 1 mole of the gas 1 degree Kelvin: J/K/mol
		public float MolarHeatCapacity = 20;

		//This is the mass, in grams, of 1 mole of the gas
		public float MolarMass;

		public string Name;

		///Gas overlay stuff///

		[NonSerialized]
		//Set in OnEnable, we use it as bool is better to check than OverlayTile != null for if we have overlay
		public bool HasOverlay;
		public float MinMolesToSee = 0.4f;
		public OverlayTile OverlayTile;

		//Colour stuff
		public bool CustomColour;
		public Color Colour;

		//Used for fusion reaction
		public int FusionPower;

		//Used to know what the reagent of this gas is
		public Reagent AssociatedReagent;

		[Tooltip("Export price of this gas per mole")]
		public int ExportPrice = 0;

		//Generated when added to dictionary
		//Doesnt need to be public as GasSO is turned into index automatically when needed, see below
		private int Index;

		private void OnEnable()
		{
			HasOverlay = OverlayTile != null;
		}

		public static implicit operator int(GasSO gas)
		{
			return gas.Index;
		}

		public void SetIndex(int newIndex)
		{
			Index = newIndex;
		}
	}
}
