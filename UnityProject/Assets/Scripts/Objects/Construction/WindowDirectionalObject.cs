using System;
using System.Collections.Generic;
using AddressableReferences;
using Messages.Server.SoundMessages;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;


namespace Objects.Construction
{
	/// <summary>
	/// Used for directional windows, based on WindowFullTileObject.
	/// </summary>
	public class WindowDirectionalObject : WindowFullTileObject
	{
		protected override void OnEnable()
		{
			base.OnEnable();
			objectPhysics.OnLocalTileReached.AddListener(OnLocalTileChange);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			objectPhysics.OnLocalTileReached.RemoveListener(OnLocalTileChange);
		}

		private void OnLocalTileChange(Vector3Int oldLocalPos, Vector3Int newLocalPos)
		{
			//We have moved from old spot so redo atmos blocks for new and old positions
			UpdateSubsystemsAt(oldLocalPos);

			if(oldLocalPos == newLocalPos) return;

			UpdateSubsystemsAt(newLocalPos);
		}

		[Server]
		protected override void ChangeAnchorStatus(HandApply interaction, bool newState)
		{
			objectPhysics.ServerSetAnchored(newState, interaction.Performer);
			UpdateSubsystems();
		}

		private void UpdateSubsystems()
		{
			objectPhysics.registerTile.Matrix.TileChangeManager.SubsystemManager.UpdateAt(objectPhysics.OfficialPosition.ToLocalInt(registerObject.Matrix));
		}

		private void UpdateSubsystemsAt(Vector3Int localPos)
		{
			objectPhysics.registerTile.Matrix.TileChangeManager.SubsystemManager.UpdateAt(localPos);
		}
	}
}
