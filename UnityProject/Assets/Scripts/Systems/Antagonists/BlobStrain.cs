using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blob
{
	[CreateAssetMenu(menuName="ScriptableObjects/BlobStrain")]
	public class BlobStrain : ScriptableObject
	{
		public string strainName;

		[TextArea(15,20)]
		public string strainDesc;

		public List<Damages> playerDamages = new List<Damages>();

		public List<Damages> objectDamages = new List<Damages>();

		public bool customResistances;

		public Resistances resistances;

		public bool customArmor;

		public Armor armor;

		public Color color;

		public StrainTypes strainType;
	}

	[Serializable]
	public struct Damages
	{
		public int damageDone;

		public DamageType damageType;
	}

	public enum StrainTypes
	{
		BlazingOil,
		CryogenicPoison,
		DebrisDevourer,
		ElectromagneticWeb,
		EnergizedJelly,
		ExplosiveLattice,
		NetworkedFibers,
		PressurizedSlime,
		ReactiveSpines,
		RegenerativeMateria,
		ReplicatingFoam,
		ShiftingFragments,
		SynchronousMesh,
		DistributedNeurons
	}
}