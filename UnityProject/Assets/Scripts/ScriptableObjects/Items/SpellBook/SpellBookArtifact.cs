using System;
using UnityEngine;

namespace ScriptableObjects.Items.SpellBook
{
	/// <summary>
	/// An artifact-type entry for a wizard's Book of Spells.
	/// </summary>
	[CreateAssetMenu(fileName = "SpellBookArtifact", menuName = "ScriptableObjects/Items/SpellBook/Artifact")]
	[Serializable]
	public class SpellBookArtifact : SpellBookEntry
	{
		[SerializeField]
		private new string name = default;
		[SerializeField]
		private GameObject[] artifacts = default;

		public string Name => name;
		public GameObject[] Artifacts => artifacts;
	}
}
