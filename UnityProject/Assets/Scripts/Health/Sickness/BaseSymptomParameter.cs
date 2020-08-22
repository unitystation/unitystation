using UnityEngine;

namespace Assets.Scripts.Health.Sickness
{
	/// <summary>
	/// Base class for sickness stage extended parameters that depends on symptom type
	/// </summary>
	/// <remarks>This is a ScriptableObject because derived classes will need to be serialized</remarks>
	public class BaseSymptomParameter: ScriptableObject
	{
	}
}
