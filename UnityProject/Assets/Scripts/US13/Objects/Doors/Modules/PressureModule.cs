using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Lifecycle;
using US13.Managers.MatrixManager;

namespace US13.Objects.Doors.Modules
{
	public class PressureModule : DoorModuleBase, IServerSpawn
	{
		[SerializeField]
		[Tooltip("If the door shows a pressure warning and is used again within this duration, it will open.")]
		private float warningCooldown = 2f;

		[SerializeField][Tooltip("Enables or disables the pressure check on this door.")]
		private bool enablePressureWarning = true;

		private int pressureThresholdCaution = 30; // kPa, both thresholds arbitrarily chosen
		private int pressureThresholdWarning = 120;
		private bool warningActive;

		public void OnSpawnServer(SpawnInfo info)
		{
			master.HackingProcessBase.RegisterPort(PlayPressureWarning, master.GetType());
		}

		private void TryPressureWarning( HashSet<DoorProcessingStates> States)
		{
			//If the door isn't powered, we skip this check. We don't have the power to scan pressure.
			if (!master.HasPower)
			{
				return;
			}

			if (warningActive)
			{
				return;
			}

			if (States.Contains(DoorProcessingStates.SoftwareHacked))
			{
				return;
			}

			if (!IsPressureDangerous())
			{
				return;
			}

			master.HackingProcessBase.ImpulsePort(PlayPressureWarning);
			States.Add(DoorProcessingStates.SoftwarePrevented);
			return;
		}

		public void PlayPressureWarning()
		{
			master.DoorAnimator.PlayPressureWarningAnimation().Forget();
			master.SoundController.ServerPlaySound(DoorSoundController.DoorSoundType.PressureWarn);
			StartCoroutine(ResetWarning());
		}

		public override void ClosedInteraction(HandApply interaction, ref HashSet<DoorProcessingStates> States)
		{
			TryPressureWarning (States);
		}

		public override void BumpingInteraction(GameObject byPlayer, ref HashSet<DoorProcessingStates> States)
		{

			TryPressureWarning( States);
		}

		/// <summary>
		///  Checks each side of the door, returns true if not considered safe and updates pressureLevel.
		///  Used to allow the player to be made aware of the pressure difference for safety.
		/// </summary>
		/// <returns>True if there is a dangerous pressure difference</returns>
		private bool IsPressureDangerous()
		{
			if (!enablePressureWarning)
			{
				// Pressure warning system is disabled, so pretend everything is fine.
				return false;
			}

			// Obtain the adjacent tiles to the door.
			var upMetaNode = MatrixManager.GetMetaDataAt(master.RegisterTile.WorldPositionServer + Vector3Int.up);
			var downMetaNode = MatrixManager.GetMetaDataAt(master.RegisterTile.WorldPositionServer + Vector3Int.down);
			var leftMetaNode = MatrixManager.GetMetaDataAt(master.RegisterTile.WorldPositionServer + Vector3Int.left);
			var rightMetaNode = MatrixManager.GetMetaDataAt(master.RegisterTile.WorldPositionServer + Vector3Int.right);

			// Only find the pressure comparison if both opposing sides are atmos. passable.
			// If both sides are not atmos. passable, then we don't care about the pressure difference.
			var vertPressureDiff = 0.0;
			var horzPressureDiff = 0.0;
			if (!upMetaNode.IsOccupied || !downMetaNode.IsOccupied)
			{
				vertPressureDiff = Math.Abs(upMetaNode.GasMixLocal.Pressure - downMetaNode.GasMixLocal.Pressure);
			}
			if (!leftMetaNode.IsOccupied || !rightMetaNode.IsOccupied)
			{
				horzPressureDiff = Math.Abs(leftMetaNode.GasMixLocal.Pressure - rightMetaNode.GasMixLocal.Pressure);
			}

			// Set pressureLevel according to the pressure difference found.
			if (vertPressureDiff >= pressureThresholdWarning || horzPressureDiff >= pressureThresholdWarning)
			{
				return true;
			}

			return vertPressureDiff >= pressureThresholdCaution || horzPressureDiff >= pressureThresholdCaution;
		}

		private IEnumerator ResetWarning()
		{
			warningActive = true;
			yield return WaitFor.Seconds(warningCooldown);
			warningActive = false;
		}
	}
}
