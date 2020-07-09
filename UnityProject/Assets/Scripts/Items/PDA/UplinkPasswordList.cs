using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Items.PDA
{
	[CreateAssetMenu(fileName = "UplinkPasswordList", menuName = "ScriptableObjects/PDA/UplinkPasswordList")]
	public class UplinkPasswordList : SingletonScriptableObject<UplinkPasswordList>
	{
		[SerializeField] [Tooltip("A list of Item categories.")]
		private List<string> wordList = new List<string>();

		public List<string> WordList => wordList;
	}
}