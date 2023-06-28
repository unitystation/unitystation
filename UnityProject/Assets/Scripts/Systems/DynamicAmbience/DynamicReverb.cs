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

		private float reverbFullStrength = 0.00f;
		private float reverbMediumStrength = -450.00f;
		private float reverbLowStrength = -875.00f;

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
			//TODO: With the current values, this feature is not 100% noticeable.
			//need a better way to handle this and a better balance for reverb strength based on how small the area the player is in.
			if (distance >= clustrophobicDistance || distance.Approx(clustrophobicDistance)) strength = reverbFullStrength;
			if (distance < clustrophobicDistance / 2) strength = reverbMediumStrength;
			if (distance < clustrophobicDistance / 4) strength = reverbLowStrength;
		}

		private bool ClaustrophobicSpace(out float distance)
		{
			var pos = transform.parent.gameObject.AssumedWorldPosServer();
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

			distance = 0;
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

			return hitValidY || hitValidX;
		}

		private float DistanceCheck(float one, float two)
		{
			//TODO: Create a better way to check how far the player is from nearby walls
			return one + two;
		}
	}
}