using System;
using UnityEngine;

/// <summary>
/// Indicates an Examinable object - players can examine this object and get a textual response.
/// Implement this if your object's examination response is dynamic.
/// </summary>
public interface IExaminable
{
	string Examine(Vector3 worldPos = default(Vector3));
}

[Flags]
public enum ExamineType
{
	None = 0,
	Basic = 1 << 0,
	AlwaysBasic = 1 << 1 | Basic,
	Advanced = 1 << 2,
	AlwaysAdvanced = 1 << 3 | Advanced
}