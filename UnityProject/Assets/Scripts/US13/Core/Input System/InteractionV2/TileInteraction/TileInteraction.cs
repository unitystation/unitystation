
using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;

namespace US13.Core.Input_System.InteractionV2.TileInteraction
{
	/// <summary>
	/// Tiles are not prefabs, but we still want to be able to associate interaction logic with them.
	/// This abstract base scriptable object allows tiles to define their interaction logic by referencing
	/// subclasses of this class.
	/// </summary>
	public abstract class TileInteraction : ScriptableObject, IPredictedCheckedInteractable<TileApply>
	{
		public virtual bool WillInteract(TileApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public virtual void ServerPerformInteraction(TileApply interaction) { }
		public virtual void ServerPerformInteraction(PositionalHandApply interaction) { }

		public virtual void ClientPredictInteraction(TileApply interaction) { }

		public virtual void ServerRollbackClient(TileApply interaction) { }
	}
}

