using Mirror;
using US13.Core.Input_System.InteractionV2.Interactions;


namespace US13.Objects.Construction
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
