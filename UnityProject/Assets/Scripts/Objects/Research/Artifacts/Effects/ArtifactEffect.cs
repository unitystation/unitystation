using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScriptableObjects;
using ScriptableObjects.Systems.Research;

/// <summary>
/// Base class for artifact effect
/// </summary>
namespace Systems.Research
{
	public enum ArtifactClass
	{
		Uranium = 0,
		Bluespace = 1,
		Bananium = 2,
	}

	public class ArtifactEffect : ScriptableObject
	{
		[Tooltip("The likelyhood and strength of this effect is dependent on the composition of this material in the artifact")]
		public ArtifactClass ArtifactClass;

		[SerializeField]
		protected ArtifactDataSO ArtifactDataSO = null;
	}
}
