using UnityEngine;

namespace Shared.Editor
{
	public abstract class Check<T>
	{
		public bool Active { get; set; }
		public abstract string Label { get; }

		public virtual void DrawGizmo(T source, Vector3Int position)
		{
		}

		public virtual void DrawLabel(T source, Vector3Int position)
		{
		}
	}
}
