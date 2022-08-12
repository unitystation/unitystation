
using System;
using Core.Transforms;
using UnityEngine;

/// <summary>
/// Handles the clamping / filtering of matrix move position to make PPRT shaders not flicker so much.
/// </summary>
public class MatrixPositionFilter
{
	private Vector3 mPreviousPosition;
	private Vector2 mPreviousFilteredPosition;



	/// <summary>
	/// Updates the transform's position to be filtered / clamped. Invoke this when transform position changes to ensure
	/// it wil.
	/// </summary>
	/// <param name="toClamp">transform whose position should be filtered</param>
	/// <param name="curPosition">unfiltered position of the transform</param>
	/// <param name="flyingDirection">current flying direction</param>
	public void FilterPosition(Transform toClamp, Vector3 curPosition, Orientation flyingDirection)
	{
		Vector2 filteredPos = LightingSystem.GetPixelPerfectPosition(curPosition, mPreviousPosition, mPreviousFilteredPosition);

		//pixel perfect position can induce lateral movement at the beginning of motion, so we must prevent that
		if (flyingDirection == Orientation.Right || flyingDirection == Orientation.Left)
		{
			filteredPos.y = (float) Math.Round(filteredPos.y);
		}
		else
		{
			filteredPos.x = (float) Math.Round(filteredPos.x);
		}

		toClamp.position = filteredPos;


		mPreviousPosition = curPosition;
		mPreviousFilteredPosition = toClamp.position;
	}

	/// <summary>
	/// Sets the saved positions manually to the specified value. Used for start / stop.
	/// TODO: I'm not sure why this logic even exists but it was there in MatrixMove
	/// </summary>
	/// <param name="position"></param>
	public void SetPosition(Vector3 position)
	{
		mPreviousPosition = position;
		mPreviousFilteredPosition = position;
	}
}
