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
		[Server]
		protected override void ChangeAnchorStatus(HandApply interaction, bool newState)
		{
			objectPhysics.ServerSetAnchored(newState, interaction.Performer);
		}
	}
}
