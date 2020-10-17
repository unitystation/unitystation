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
		private GameObject[] artifacts = default;

		public GameObject[] Artifacts => artifacts;
	}
}
