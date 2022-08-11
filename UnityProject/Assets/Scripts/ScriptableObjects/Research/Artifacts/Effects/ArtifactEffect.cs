using UnityEngine;

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
		//Is it the first in the dropdown? second? etc.
		//More than one effect can have the same index
		[Tooltip("The index of the console option that relates to this")]
		public int GuessIndex = 0;
	}
}
