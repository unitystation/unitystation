using UnityEngine;

namespace UI.Core.Radial
{
    public interface IRadial
    {
	    bool IsPositionWithinRadial(Vector2 position, bool fullRadius);
	    float ItemArcMeasure { get; }
	    void RotateRadial(float rotation);
    }
}
