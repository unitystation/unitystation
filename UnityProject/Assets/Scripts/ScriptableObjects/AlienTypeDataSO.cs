using System.Collections.Generic;
using NaughtyAttributes;
using Systems.Antagonists;
using Systems.Spells;
using UnityEngine;
using UnityEngine.Serialization;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "AlienTypeData", menuName = "ScriptableObjects/Antagonist/AlienTypeData")]
	public class AlienTypeDataSO : ScriptableObject
	{
		[Header("Main")]
		public string Name;

		public AlienPlayer.AlienTypes AlienType;

		public GhostRoleData GhostRoleData;

		[TextArea(10, 20)]
		public string Description;

		[Header("Variables")]
		public float Speed;

		[HorizontalLine()]
		public float AttackSpeed = 7;
		public float AttackDamage = 5;
		public DamageType DamageType = DamageType.Brute;
		public uint ChanceToHit = 50;

		[HorizontalLine()]
		public int MaxPlasma = 100;
		public int InitialPlasma = 100;
		public int PlasmaGainRate = 3; //Per second

		[HorizontalLine()]
		public float HealAmount = 5; //Per second
		public int HealPlasmaCost = 5;

		[HorizontalLine()]
		public int MaxGrowth = 100;

		[HorizontalLine()]
		public int AcidSpitCost = 10;
		public int NeurotoxinSpitCost = 50;

		[HorizontalLine()]
		public AlienPlayer.AlienTypes EvolvedFrom = AlienPlayer.AlienTypes.Larva3;

		[Header("ActionData")]
		public List<ActionData> ActionData = new List<ActionData>();

		[Header("Sprites")]
		public SpriteDataSO Normal;
		public SpriteDataSO Dead;
		public SpriteDataSO Pounce;
		public SpriteDataSO Sleep;
		[FormerlySerializedAs("Unconcious")] public SpriteDataSO Unconscious;
		public SpriteDataSO Running;
		public SpriteDataSO Front;
		public SpriteDataSO Back;
	}
}