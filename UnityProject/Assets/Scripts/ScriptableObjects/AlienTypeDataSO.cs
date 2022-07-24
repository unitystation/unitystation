using System.Collections.Generic;
using Systems.Antagonists;
using Systems.Spells;
using UnityEngine;
using UnityEngine.Serialization;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "AlienTypeData", menuName = "ScriptableObjects/Antagonist/AlienTypeData")]
	public class AlienTypeDataSO : ScriptableObject
	{
		public string Name;
		public AlienPlayer.AlienTypes AlienType;
		public float Speed;

		public float AttackSpeed = 7;
		public float AttackDamage = 5;
		public DamageType DamageType = DamageType.Brute;
		public uint ChanceToHit = 50;

		public int MaxPlasma = 100;
		//Per second
		public int PlasmaGainRate = 3;

		public float HealAmount = 5;
		public int HealPlasmaCost = 5;

		public SpriteDataSO Normal;
		public SpriteDataSO Dead;
		public SpriteDataSO Pounce;
		public SpriteDataSO Sleep;
		[FormerlySerializedAs("Unconcious")] public SpriteDataSO Unconscious;
		public SpriteDataSO Running;
		public SpriteDataSO Front;
		public SpriteDataSO Back;

		public int MaxGrowth = 100;

		public AlienPlayer.AlienTypes EvolvedFrom = AlienPlayer.AlienTypes.Larva3;

		public List<ActionData> ActionData = new List<ActionData>();
	}
}