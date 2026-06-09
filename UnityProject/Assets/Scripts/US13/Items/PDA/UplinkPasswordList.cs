using System.Collections.Generic;
using UnityEngine;
using US13.ScriptableObjects;

namespace US13.Items.PDA
{
	[CreateAssetMenu(fileName = "UplinkPasswordList", menuName = "ScriptableObjects/PDA/UplinkPasswordList")]
	public class UplinkPasswordList : SingletonScriptableObject<UplinkPasswordList>
	{
		[SerializeField] [Tooltip("A list of Item categories.")]
		private List<string> wordList = new List<string>();

		public List<string> WordList => wordList;
	}
}