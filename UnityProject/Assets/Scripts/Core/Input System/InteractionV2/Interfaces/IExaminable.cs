using UnityEngine;

/// <summary>
/// Indicates an Examinable object - players can examine this object and get a textual response.
/// Implement this if your object's examination response is dynamic.
/// </summary>
public interface IExaminable
{
	string Examine(Vector3 worldPos = default(Vector3));
}
