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

		public int MaxPlasma = 100;

		//Per second
		public int PlasmaGainRate = 3;

		public SpriteDataSO Normal;
		public SpriteDataSO Dead;
		public SpriteDataSO Pounce;
		public SpriteDataSO Sleep;
		[FormerlySerializedAs("Unconcious")] public SpriteDataSO Unconscious;
		public SpriteDataSO Running;
		public SpriteDataSO Front;
		public SpriteDataSO Back;

		public List<Spell> Spells = new List<Spell>();
	}
}