using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ScriptableObjects.Audio
{
	public class FootstepSounds : MonoBehaviour
	{
		public FloorSounds DefaultFloorSound;

		public static FootstepSounds Instance;

		public void Awake()
		{
			Instance = this;
		}

		public static void PlayerFootstepAtPosition(Vector3 worldPos, PlayerSync playerSync)
		{
			var stepType = GetFootSteptype(playerSync);
			playerSync.Step = !playerSync.Step;
			if (playerSync.Step)
			{
				FootstepAtPosition(worldPos, stepType);
			}
		}

		private static StepType GetFootSteptype(PlayerSync playerSync)
		{
			//TODO find a more optimized way, this is getting items every step (like we did before I did the humongous dictionary lol).

			var hardsuit = playerSync.playerScript.Equipment.ItemStorage.GetNamedItemSlot(NamedSlot.outerwear)?.Item;

			if (hardsuit != null && Validations.HasItemTrait(hardsuit.gameObject, CommonTraits.Instance.Hardsuit))
			{
				return StepType.Hardsuit;
			}

			var shoes = playerSync.playerScript.Equipment.ItemStorage.GetNamedItemSlot(NamedSlot.feet)?.Item;

			if (shoes == null)
			{
				//TODO get the type of barefoot based on mob legs
				return StepType.Barefoot;
			}

			if (Validations.HasItemTrait(shoes.gameObject, CommonTraits.Instance.Squeaky))
			{
				return StepType.ClownStep;
			}

			return StepType.Shoes;
		}


		/// <summary>
		/// Play footsteps at given position. It will handle all the logic to determine
		/// the proper sound to use.
		/// </summary>
		/// <param name="worldPos">Where in the world is this sound coming from. Also used to get the type of tile</param>
		/// <param name="stepType">What kind of step does the creature walking have</param>
		/// <param name="override"></param>
		public static void FootstepAtPosition(Vector3 worldPos, StepType stepType)
		{
			var matrix = MatrixManager.AtPoint(worldPos.NormalizeToInt(), false);
			var locPos = matrix.ObjectParent.transform.InverseTransformPoint(worldPos).RoundToInt();
			var tile = matrix.MetaTileMap.GetTile(locPos) as BasicTile;

			if (tile == null)
			{
				return;
			}

			var floorTileSounds = tile.floorTileSounds;
			List<AddressableAudioSource> addressableAudioSource = null;

			switch (stepType)
			{
				case StepType.None:
					return;
				case StepType.Barefoot:
					addressableAudioSource = floorTileSounds != null
						? floorTileSounds.Barefoot
						: Instance.DefaultFloorSound.Barefoot;

					if (addressableAudioSource.Any() == false)
					{
						addressableAudioSource = Instance.DefaultFloorSound.Barefoot;
					}
					break;

				case StepType.Shoes:
					addressableAudioSource = floorTileSounds != null
						? floorTileSounds.Shoes
						: Instance.DefaultFloorSound.Shoes;

					if (addressableAudioSource.Any() == false)
					{
						addressableAudioSource = Instance.DefaultFloorSound.Shoes;
					}
					break;

				case StepType.Claw:
					addressableAudioSource = floorTileSounds != null
						? floorTileSounds.Claw
						: Instance.DefaultFloorSound.Claw;

					if (addressableAudioSource.Any() == false)
					{
						addressableAudioSource = Instance.DefaultFloorSound.Claw;
					}

					break;

				case StepType.ClownStep:
					addressableAudioSource = floorTileSounds != null
						? floorTileSounds.Clown
						: Instance.DefaultFloorSound.Clown;

					if (addressableAudioSource.Any() == false)
					{
						addressableAudioSource = Instance.DefaultFloorSound.Clown;
					}
					break;

				case StepType.Hardsuit:
					addressableAudioSource = floorTileSounds != null
						? floorTileSounds.Hardsuit
						: Instance.DefaultFloorSound.Hardsuit;

					if (addressableAudioSource.Any() == false)
					{
						addressableAudioSource = Instance.DefaultFloorSound.Hardsuit;
					}
					break;

				default:
					Logger.LogError($"Unexpected value for Steptype: {stepType} on tile: {tile}");
					break;
			}

			if (addressableAudioSource == null)
			{
				return;
			}
			SoundManager.PlayNetworkedAtPos(addressableAudioSource.PickRandom(), worldPos,Random.Range(0.7f, 1.2f), polyphonic: true);
		}
	}
}

