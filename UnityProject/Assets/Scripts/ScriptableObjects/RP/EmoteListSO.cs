using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.RP
{
	[CreateAssetMenu(fileName = "EmoteListSO", menuName = "ScriptableObjects/RP/Emotes/EmoteList")]
	public class EmoteListSO : ScriptableObject
	{
		public List<EmoteSO> Emotes = new List<EmoteSO>();
	}
}