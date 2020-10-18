
using System;
using UnityEngine;

/// <summary>
/// Describes the behavior of the camera when recoiling. This only affects the behavior of the camera, not
/// the recoil of the actual shots.
/// </summary>
[Serializable]
public class CameraRecoilConfig
{
	[Tooltip("Distance camera should kick back to from the player")]
	public float Distance;

	[Tooltip("Total time that it should take to reach recoil distance from being centered on the player.")]
	public float RecoilDuration;

	[Tooltip("Total time it should take to recover to being centered on the player" +
	         " after done recoiling back.")]
	public float RecoveryDuration;
}
