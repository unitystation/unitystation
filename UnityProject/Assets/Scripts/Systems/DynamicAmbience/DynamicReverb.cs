using Audio.Containers;
using UnityEngine;

namespace Systems.DynamicAmbience
{
	public class DynamicReverb : MonoBehaviour
	{
		[SerializeField] private LayerMask layerMask;
		[SerializeField] private float updateTime = 0.75f;
		[SerializeField] private float clustrophobicDistance = 8f;
		[SerializeField] private bool debug = true;

		private readonly float reverbFullStrength = 0.00f;
		private readonly float reverbMediumStrength = -650.00f;
		private readonly float reverbLowStrength = -950.00f;

		private const string AUDIOMIXER_REVERB_KEY = "SFXReverb";

		private float strength = 0.00f;

		private bool isEnabled = false;

		public void EnableAmbienceForPlayer()
		{
			if (CustomNetworkManager.IsHeadless) return;
			Logger.Log("Enabling Dynamic Reverb system.");
			if (transform.parent.gameObject.NetWorkIdentity()?.isOwned == false || isEnabled) return;
			UpdateManager.Add(UpdateMe, updateTime);
			isEnabled = true;
		}

		public void DisableAmbienceForPlayer()
		{
			if (CustomNetworkManager.IsHeadless) return;
			Logger.Log("Disabling Dynamic Reverb system.");
			if (transform.parent.gameObject.NetWorkIdentity()?.isOwned == false) return;
			AudioManager.Instance.GameplayMixer.audioMixer.ClearFloat(AUDIOMIXER_REVERB_KEY);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			isEnabled = false;
		}

		private void UpdateMe()
		{
			if (ClaustrophobicSpace(out var distance))
			{
				DistanceStrengthToUse(distance);
				AudioManager.Instance.GameplayMixer.audioMixer.SetFloat(AUDIOMIXER_REVERB_KEY, strength);
			}
			else
			{
				AudioManager.Instance.GameplayMixer.audioMixer.ClearFloat(AUDIOMIXER_REVERB_KEY);
			}
		}

		private void DistanceStrengthToUse(float distance)
		{
			if (debug)
			{
				Logger.Log($"Distance: " +
				           $"{distance} -- Full {clustrophobicDistance}/{distance >= clustrophobicDistance} " +
				           $"-- Med {clustrophobicDistance / 1.25f}/{distance < clustrophobicDistance / 1.25f} " +
				           $"-- Low {clustrophobicDistance / 1.50f}/{distance < clustrophobicDistance /1.50f}");
			}

			if (distance >= clustrophobicDistance || distance.Approx(clustrophobicDistance) || distance.Approx(1))
			{
				strength = reverbFullStrength;
				return;
			}
			if (distance < clustrophobicDistance / 1.50f) strength = reverbLowStrength;
			if (distance < clustrophobicDistance / 1.25f) strength = reverbMediumStrength;
		}

		private bool ClaustrophobicSpace(out float distance)
		{
			distance = clustrophobicDistance / 4;
			var pos = transform.parent.gameObject.AssumedWorldPosServer().CutToInt();
			if (MatrixManager.IsSpaceAt(pos, CustomNetworkManager.IsServer)) return true;
			var hitData = CheckSpace(pos);
			var hitValidY = hitData[0].Hit && hitData[1].Hit;
			var hitValidX = hitData[2].Hit && hitData[3].Hit;

			if (hitValidX)
			{
				distance = DistanceCheck(hitData[2].Distance, hitData[3].Distance);
			}
			else if (hitValidY)
			{
				distance = DistanceCheck(hitData[0].Distance, hitData[1].Distance);
			}
			else
			{
				distance = 0;
			}

			return hitValidY || hitValidX;
		}

		private HitData[] CheckSpace(Vector3Int pos)
		{
			Vector3[] directions = new[]
			{
				new Vector3(pos.x, pos.y + clustrophobicDistance, pos.z), //up
				new Vector3(pos.x, pos.y - clustrophobicDistance, pos.z), //down
				new Vector3(pos.x + clustrophobicDistance, pos.y, pos.z), //left
				new Vector3(pos.x - clustrophobicDistance, pos.y, pos.z) //right
			};
			HitData[] hitData = new HitData[4];
			int index = -1;
			foreach (var dir in directions)
			{
				index++;
				HitData data = new HitData();
				var line =  MatrixManager.Linecast(
					pos,
					LayerTypeSelection.Walls, layerMask,
					dir,
					debug);
				data.Distance = line.Distance;
				data.Hit = line.ItHit;
				hitData[index] = data;
			}

			return hitData;
		}

		private float DistanceCheck(float one, float two)
		{
			//TODO: Create a better way to check how far the player is from nearby walls
			return one + two;
		}

		private struct HitData
		{
			public bool Hit;
			public float Distance;
		}
	}
}