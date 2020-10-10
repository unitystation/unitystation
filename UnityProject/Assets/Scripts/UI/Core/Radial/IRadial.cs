using UnityEngine;

namespace UI.Core.Radial
{
    public interface IRadial
    {
	    bool IsPositionWithinRadial(Vector2 position, bool fullRadius);
	    float ItemArcAngle { get; }
	    void RotateRadial(float rotation);
    }
}
