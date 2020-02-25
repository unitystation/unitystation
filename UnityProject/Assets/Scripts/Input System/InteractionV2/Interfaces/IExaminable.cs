using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Indicates an Examinable Component -- Deets are coming later.
/// </summary>
public interface IExaminable
{
	string Examine(Vector3 worldPos = default(Vector3));
}
