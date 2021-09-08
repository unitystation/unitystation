using System.Collections.Generic;
using AddressableReferences;
using Messages.Server.SoundMessages;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


namespace ScriptableObjects.Audio
{
	public class FootstepSounds : MonoBehaviour
	{
		[SerializeField]
		[FormerlySerializedAs("DefaultFloorSound")]
		private FloorSounds defaultFloorSound;

		private static FootstepSounds instance;

		public void Awake()
		{
			instance = this;
		}

		public static void PlayerFootstepAtPosition(Vector3 worldPos,
			PlayerSync playerSync)
		{
			if (playerSync.playerScript.registerTile.IsLayingDown == false)
			{
				var stepType = GetFootStepType(playerSync);
				playerSync.Step = !playerSync.Step;

				if (playerSync.Step)
				{
					FootstepAtPosition(worldPos, stepType, playerSync.playerScript.mind.StepSound);
				}
			}
			else
			{
				ShuffleAtPosition(worldPos);
			}

		}

		private static StepType GetFootStepType(PlayerSync playerSync)
		{
			foreach (var itemSlot in playerSync.playerScript.Equipment.ItemStorage.GetNamedItemSlots(NamedSlot.feet))
			{
				if (itemSlot.Item != null)
				{
					return StepType.Shoes;
				}
			}

			//TODO find player's specie and return CLAW if needed

			return StepType.Barefoot;

		}


		private static void ShuffleAtPosition(Vector3 worldPos)
		{
			var audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.7f, 1.2f));
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Crawl1, worldPos, audioSourceParameters, polyphonic: true);
		}

		/// <summary>
		/// Play footsteps at given position. It will handle all the logic to determine
		/// the proper sound to use.
		/// </summary>
		/// <param name="worldPos">Where in the world is this sound coming from. Also used to get the type of tile</param>
		/// <param name="stepType">What kind of step does the creature walking have</param>
		/// <param name="override">if assigned, it will override the default footstep sound.</param>
		private static void FootstepAtPosition(Vector3 worldPos, StepType stepType, FloorSounds @override = null )
		{
			var matrix = MatrixManager.AtPoint(worldPos.RoundToInt(), false);

			if (matrix == null) return;

			var locPos = matrix.ObjectParent.transform.InverseTransformPoint(worldPos).RoundToInt();
			var tile = matrix.MetaTileMap.GetTile(locPos) as BasicTile;

			if (tile == null) return;

			var floorTileSounds = @override ? @override : tile.floorTileSounds;

			List<AddressableAudioSource> addressableAudioSource;

			switch (stepType)
			{
				case StepType.None:
					return;
				case StepType.Barefoot:
					addressableAudioSource = floorTileSounds.OrNull()?.Barefoot;
					if (addressableAudioSource is null || addressableAudioSource.Count == 0)
					{
						addressableAudioSource = instance.defaultFloorSound.Barefoot;
					}
					break;
				case StepType.Shoes:
					addressableAudioSource = floorTileSounds.OrNull()?.Shoes;
					if (addressableAudioSource is null || addressableAudioSource.Count == 0)
					{
						addressableAudioSource = instance.defaultFloorSound.Shoes;
					}
					break;
				case StepType.Claw:
					addressableAudioSource = floorTileSounds.OrNull()?.Claw;
					if (addressableAudioSource is null || addressableAudioSource.Count == 0)
					{
						addressableAudioSource = instance.defaultFloorSound.Claw;
					}
					break;
				default:
					addressableAudioSource = instance.defaultFloorSound.Shoes;
					break;
			}

			var audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.7f, 1.2f));
			SoundManager.PlayNetworkedAtPos(addressableAudioSource.PickRandom(), worldPos, audioSourceParameters, polyphonic: true);
		}
	}
}

