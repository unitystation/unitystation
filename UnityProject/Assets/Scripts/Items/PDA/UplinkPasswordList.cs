using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "UplinkPasswordList", menuName = "ScriptableObjects/PDA/UplinkPasswordList")]
public class UplinkPasswordList : ScriptableObject
{
	[SerializeField] [Tooltip("A list of Item categories.")]
	private List<string> wordList = new List<string>();

	public List<string> WordList => wordList;
}
