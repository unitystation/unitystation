using UnityEngine;

public abstract class Check<S>
{
	public bool Active { get; set; }
	public abstract string Label { get; }

	public virtual void DrawGizmo(S source, Vector3Int position)
	{
	}

	public virtual void DrawLabel(S source, Vector3Int position)
	{
	}
}