using Audio.Containers;
using UnityEngine;

namespace Systems.DynamicAmbience
{
	public class DynamicReverb : MonoBehaviour
	{
		[SerializeField] private LayerMask layerMask;
		[SerializeField] private float updateTime = 0.75f;
		[SerializeField] private float clustrophobicDistance = 2.95f;
		[SerializeField] private bool debug = true;

		private readonly float reverbFullStrength = 0.00f;
		private readonly float reverbMediumStrength = -925.00f;
		private readonly float reverbLowStrength = -650.00f;

		private const string AUDIOMIXER_REVERB_KEY = "SFXReverb";

		private float strength = 0.00f;

		public void EnableAmbienceForPlayer()
		{
			if (CustomNetworkManager.IsHeadless) return;
			if (transform.parent.gameObject.NetWorkIdentity()?.isOwned == false) return;
			UpdateManager.Add(UpdateMe, updateTime);
		}

		public void DisableAmbienceForPlayer()
		{
			if (CustomNetworkManager.IsHeadless) return;
			if (transform.parent.gameObject.NetWorkIdentity()?.isOwned == false) return;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
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

			if (distance >= clustrophobicDistance || distance.Approx(clustrophobicDistance))
			{
				strength = reverbFullStrength;
				return;
			}
			if (distance < clustrophobicDistance / 1.50f || distance.Approx(1)) strength = reverbLowStrength;
			if (distance < clustrophobicDistance / 1.25f) strength = reverbMediumStrength;
		}

		private bool ClaustrophobicSpace(out float distance)
		{
			distance = clustrophobicDistance / 4;
			var pos = transform.parent.gameObject.AssumedWorldPosServer().CutToInt();
			if (MatrixManager.IsSpaceAt(pos, CustomNetworkManager.IsServer)) return true;
			var lineUp = MatrixManager.Linecast(
				pos,
				LayerTypeSelection.Walls, layerMask,
				new Vector3(pos.x, pos.y + clustrophobicDistance, pos.z),
				debug);
			var lineDown = MatrixManager.Linecast(
				pos,
				LayerTypeSelection.Walls, layerMask,
				new Vector3(pos.x, pos.y - clustrophobicDistance, pos.z),
				debug);
			var lineLeft = MatrixManager.Linecast(
				pos,
				LayerTypeSelection.Walls, layerMask,
				new Vector3(pos.x + clustrophobicDistance, pos.y, pos.z),
				debug);
			var lineRight = MatrixManager.Linecast(
				pos,
				LayerTypeSelection.Walls, layerMask,
				new Vector3(pos.x - clustrophobicDistance, pos.y, pos.z),
				debug);

			var hitValidY = lineUp.ItHit && lineDown.ItHit;
			var hitValidX = lineLeft.ItHit && lineRight.ItHit;

			if (debug)
			{
				Logger.Log($"Wall Line check distance = Y+ {lineUp.Distance} Y- {lineDown.Distance} " +
				           $"X+ {lineLeft.Distance} X- {lineRight}");
			}
			if (hitValidX)
			{
				distance = DistanceCheck(lineLeft.Distance, lineRight.Distance);
			}
			else if (hitValidY)
			{
				distance = DistanceCheck(lineUp.Distance, lineDown.Distance);
			}
			else
			{
				distance = 0;
			}

			return hitValidY || hitValidX;
		}

		private float DistanceCheck(float one, float two)
		{
			//TODO: Create a better way to check how far the player is from nearby walls
			return one + two;
		}
	}
}