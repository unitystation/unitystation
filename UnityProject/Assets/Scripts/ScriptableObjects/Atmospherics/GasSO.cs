using Systems.Atmospherics;
using Chemistry;
using TileManagement;
using UnityEngine;

namespace ScriptableObjects.Atmospherics
{
	[CreateAssetMenu(fileName = "GasSO", menuName = "ScriptableObjects/Atmos/GasSO")]
	public class GasSO : ScriptableObject
	{
		//this is how many Joules are needed to raise 1 mole of the gas 1 degree Kelvin: J/K/mol
		public float MolarHeatCapacity = 20;

		//this is the mass, in grams, of 1 mole of the gas
		public float MolarMass;
		public string Name;

		//Generated when added to dictionary
		//Doesnt need to be public as Gas struct is turned into index automatically when needed
		private int Index;

		public bool HasOverlay;
		public float MinMolesToSee = 0.4f;
		public string TileName = "NONE";
		public OverlayType OverlayType = OverlayType.None;
		public int FusionPower;

		public Reagent AssociatedReagent;

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
