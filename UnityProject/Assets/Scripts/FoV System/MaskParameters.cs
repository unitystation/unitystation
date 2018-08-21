using System;
using UnityEngine;

public struct MaskParameters : IEquatable<MaskParameters>
{
	public readonly float cameraOrthographicSize;
	public readonly Vector2Int screenSize;

	public MaskParameters(Camera iCamera)
	{
		cameraOrthographicSize = iCamera.orthographicSize;
		screenSize = new Vector2Int(Screen.width, Screen.height);
	}
	
	public static bool operator ==(MaskParameters iLeftHand, MaskParameters iRightHand)
	{
		// Equals handles case of null on right side.
		return iLeftHand.Equals(iRightHand);
	}

	public static bool operator !=(MaskParameters iLeftHand, MaskParameters iRightHand)
	{
		return !(iLeftHand == iRightHand);
	}

	public bool Equals(MaskParameters iMask)
	{
		return this.cameraOrthographicSize == iMask.cameraOrthographicSize &&
		       this.screenSize == iMask.screenSize;
	}
}