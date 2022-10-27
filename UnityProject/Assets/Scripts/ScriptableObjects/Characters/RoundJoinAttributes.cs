using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.Characters
{
	[CreateAssetMenu(fileName = "RoundJoinAttributes", menuName = "RoundJoinAttributes")]
	public class RoundJoinAttributes : ScriptableObject
	{
		[NaughtyAttributes.InfoBox("Make sure that there are no two attributes with similar IDs.")]
		public SerializableDictionary<int, CharacterAttribute> AttributesToUse = new SerializableDictionary<int, CharacterAttribute> ();
	}
}