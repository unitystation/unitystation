using UnityEngine;

/// <summary>
/// Allows an object to have an associated network tab that pops up when clicked.
/// If there are additional interactions that can be done on this object
/// please ensure this component is placed below them, otherwise the tab open/close will
/// be the interaction that always takes precedence.
/// </summary>
public class HasNetworkTab : MonoBehaviour, ICheckedInteractable<HandApply>, IServerDespawn
{
	[Tooltip("Network tab to display.")]
	public NetTabType NetTabType = NetTabType.None;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
			return false;

		//interaction only works if hand is empty
		if (interaction.HandObject != null)
		{ return false; }

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		TabUpdateMessage.Send( interaction.Performer, gameObject, NetTabType, TabAction.Open );
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		NetworkTabManager.Instance.RemoveTab(gameObject, NetTabType);
	}
}
