using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using Random = UnityEngine.Random;

public class FootstepSounds : MonoBehaviour
{
	public FloorSounds DefaultFloorSound;

	public static FootstepSounds Instant;

	public void Awake()
	{
		Instant = this;
	}

	public static void PlayerFootstepAtPosition(Vector3 worldPos,
		PlayerSync PlayerSync)
	{
		StepType stepType = GetFootSteptype(PlayerSync);
		PlayerSync.Step = !PlayerSync.Step;
		if (PlayerSync.Step)
		{
			FootstepAtPosition(worldPos, stepType, PlayerSync.playerScript.mind.StepSound);
		}
	}

	public static StepType GetFootSteptype(PlayerSync PlayerSync)
	{
		if (PlayerSync.playerScript.Equipment.ItemStorage.GetNamedItemSlot(NamedSlot.feet)?.Item != null)
		{
			return StepType.Shoes;
		}
		else
		{
			//Add stuff for races
			return StepType.Barefoot;
		}
	}


	/// <summary>
	/// Play footsteps at given position. It will handle all the logic to determine
	/// the proper sound to use.
	/// </summary>
	/// <param name="worldPos">Where in the world is this sound coming from. Also used to get the type of tile</param>
	/// <param name="stepType">What kind of step does the creature walking have</param>
	/// <param name="performer">The creature making the sound</param>
	public static void FootstepAtPosition(Vector3 worldPos, StepType stepType,FloorSounds Override = null )
	{
		MatrixInfo matrix = MatrixManager.AtPoint(worldPos.NormalizeToInt(), false);
		var locPos = matrix.ObjectParent.transform.InverseTransformPoint(worldPos).RoundToInt();
		var tile = matrix.MetaTileMap.GetTile(locPos) as BasicTile;

		if (tile != null)
		{
			FloorSounds FloorTileSounds = tile.floorTileSounds;
			List<AddressableAudioSource> AddressableAudioSource = null;
			if (Override != null && tile.CanSoundOverride == false)
			{
				FloorTileSounds = Override;
			}

			switch (stepType)
			{
				case StepType.None:
					return;
				case StepType.Barefoot:
					AddressableAudioSource = FloorTileSounds?.Barefoot;
					if (FloorTileSounds == null || AddressableAudioSource.Count > 0)
					{
						AddressableAudioSource = Instant.DefaultFloorSound.Barefoot;
					}
					break;
				case StepType.Shoes:
					AddressableAudioSource = FloorTileSounds?.Shoes;
					if (FloorTileSounds == null || AddressableAudioSource.Count > 0)
					{
						AddressableAudioSource = Instant.DefaultFloorSound.Shoes;
					}
					break;
				case StepType.Claw:
					AddressableAudioSource = FloorTileSounds?.Claw;
					if (FloorTileSounds == null || AddressableAudioSource.Count > 0)
					{
						AddressableAudioSource = Instant.DefaultFloorSound.Claw;
					}

					break;
			}

			if (AddressableAudioSource == null)
			{
				return;
			}
			SoundManager.PlayNetworkedAtPos(AddressableAudioSource.PickRandom(), worldPos,pitch : Random.Range(0.7f, 1.2f), polyphonic: true);

		}
	}
}

