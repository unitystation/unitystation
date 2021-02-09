using UnityEngine;

namespace UI.Core.Radial
{
	public interface IRadial
	{
		bool IsActive { get; }
		int InnerRadius { get; }
		int OuterRadius { get; }
		bool IsPositionWithinRadial(Vector2 position, bool fullRadius);
		float ItemArcMeasure { get; }
		void RotateRadial(float rotation);
	}
}
