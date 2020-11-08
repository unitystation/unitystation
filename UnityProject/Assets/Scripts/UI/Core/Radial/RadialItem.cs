using UnityEngine;

namespace UI.Core.Radial
{
	public class RadialItem<T> : MonoBehaviour, ICanvasRaycastFilter where T : RadialItem<T>
	{
		protected Radial<T> Radial { get; private set; }

		public int Index { get; set; }

		public virtual void Setup(Radial<T> parent, int index)
		{
			Radial = parent;
			Index = index;
		}

		public bool IsRaycastLocationValid(Vector2 screenPosition, Camera eventCamera) =>
			Radial.IsItemRaycastable(this, screenPosition);
	}
}
