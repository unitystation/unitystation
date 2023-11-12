using System.Collections.Generic;
using Audio.Containers;
using UnityEngine;

namespace Core.Sound
{
	public enum RoomSize
	{
		Small,
		Medium,
		Large,
		ExtraLarge
	}

	public static class SoundPhysics
	{
		public const int DISTANT_SOUND_DISTANCE = 20;
		public const int MAX_REVERB_RAY_TRAVEL_DISTANCE = 30;
		public const int MEDIUM_ROOM_AREA = 5;
		public const int LARGE_ROOM_AREA = 8;

		public static LayerTypeSelection ObstructionLayerMask => LayerTypeSelection.Walls | LayerTypeSelection.Windows;
		public static LayerTypeSelection ReverbLayerMask => LayerTypeSelection.Walls | LayerTypeSelection.Windows;

		public static readonly Dictionary<RoomSize, float>  RoomSizeToReverbStrength = new()
		{
			{RoomSize.Small, -2000f},
			{RoomSize.Medium, -950f},
			{RoomSize.Large, -650f},
			{RoomSize.ExtraLarge, -0f},
		};

		/// <summary>
		/// Using the player perspective, is the sound obstructed by a wall or other object? Is it too far away?
		/// </summary>
		/// <param name="sound"></param>
		/// <returns></returns>
		public static bool IsObstructedOrDistant(SoundSpawn sound)
		{
			Vector3Int playerPos = PlayerManager.LocalPlayerObject.TileWorldPosition().To3Int();
			Vector3Int sourcePos = sound.transform.position.CutToInt();

			if ((playerPos - sourcePos).magnitude > DISTANT_SOUND_DISTANCE) return true;

			var line = MatrixManager.Linecast(
				playerPos,
				ObstructionLayerMask,
				null,
				sourcePos);

			return line.ItHit;
		}

		/// <summary>
		/// Projects 4 rays, one for each direction, from the sound source. Sums and averages the distance to the nearest wall in each direction, doing an estimation of room area.
		/// Then returns a RoomSize enum based on the average distance.
		/// </summary>
		/// <param name="soundObject"></param>
		/// <param name="debug"></param>
		/// <returns></returns>
		public static RoomSize CalculateRoomSize(GameObject soundObject, bool debug = false)
		{
			float totalDistanceToWalls = 0;
			Vector3 origin = soundObject.transform.position.CutToInt();

			Vector3[] destinations =
			{
				//from origin to the right
				new Vector3(origin.x + MAX_REVERB_RAY_TRAVEL_DISTANCE, origin.y, origin.z).CutToInt(),
				//from origin to the left
				new Vector3(origin.x - MAX_REVERB_RAY_TRAVEL_DISTANCE, origin.y, origin.z).CutToInt(),
				//from origin to the top
				new Vector3(origin.x, origin.y + MAX_REVERB_RAY_TRAVEL_DISTANCE, origin.z).CutToInt(),
				//from origin to the bottom
				new Vector3(origin.x, origin.y - MAX_REVERB_RAY_TRAVEL_DISTANCE, origin.z).CutToInt(),
			};

			foreach (var destination in destinations)
			{
				var line = MatrixManager.Linecast(
					origin,
					ReverbLayerMask,
					null,
					destination,
					debug);

				if (line.ItHit)
				{
					totalDistanceToWalls += line.Distance;
				}
			}

			float averageDistanceToWalls = totalDistanceToWalls / destinations.Length;

			return averageDistanceToWalls switch
			{
				< MEDIUM_ROOM_AREA => RoomSize.Small,
				<= MEDIUM_ROOM_AREA => RoomSize.Medium,
				<= LARGE_ROOM_AREA => RoomSize.Large,
				> LARGE_ROOM_AREA => RoomSize.ExtraLarge,
				_ => RoomSize.Small
			};
		}

		/// <summary>
		/// Evaluates the physical conditions of the sound and routes it to the appropriate mixer group.
		/// </summary>
		/// <param name="sound"></param>
		public static void EvaluateAndRouteSoundToMixer(SoundSpawn sound)
		{
			if (IsObstructedOrDistant(sound))
			{
				sound.AudioSource.outputAudioMixerGroup = AudioManager.Instance.SFXMuffledMixer;
			}

			//TODO: This method used to also route sound to different mixer groups with reverb based on room size.
			//It had to be changed because the Audio management in unity is inflexible and doesn't allow for dynamic routing at the level I wish to do it.
			//I will revise this solution later on
		}
	}
}