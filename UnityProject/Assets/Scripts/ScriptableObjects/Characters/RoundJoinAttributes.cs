using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.Characters
{
	[CreateAssetMenu(fileName = "RoundJoinAttributes", menuName = "RoundJoinAttributes")]
	public class RoundJoinAttributes : ScriptableObject
	{
		public List<CharacterAttribute> AttributesToUse = new List<CharacterAttribute>();
	}
}