using System.Collections.Generic;
using Systems.Antagonists;
using Systems.Spells;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "AlienTypeData", menuName = "ScriptableObjects/Antagonist/AlienTypeData")]
	public class AlienTypeDataSO : ScriptableObject
	{
		public AlienPlayer.AlienTypes AlienType;
		public float Speed;

		public SpriteDataSO Normal;
		public SpriteDataSO Dead;
		public SpriteDataSO Pounce;
		public SpriteDataSO Sleep;
		public SpriteDataSO Unconcious;
		public SpriteDataSO Running;
		public SpriteDataSO Front;
		public SpriteDataSO Back;

		public List<Spell> Spells = new List<Spell>();
	}
}