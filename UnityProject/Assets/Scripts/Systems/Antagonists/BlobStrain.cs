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
	}

	[Serializable]
	public struct Damages
	{
		public int damageDone;

		public DamageType damageType;
	}
}