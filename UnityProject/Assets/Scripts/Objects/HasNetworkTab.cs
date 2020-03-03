using UnityEngine;

/// <summary>
/// Allows an object to have an associated network tab that pops up when clicked.
/// If there are additional interactions that can be done on this object
/// please ensure this component is placed below them, otherwise the tab open/close will
/// be the interaction that always takes precedence.
/// </summary>
public class HasNetworkTab : MonoBehaviour, IInteractable<HandApply>, IServerDespawn
{
	[Tooltip("Network tab to display.")]
	public NetTabType NetTabType = NetTabType.None;

	public void ServerPerformInteraction(HandApply interaction)
	{
		TabUpdateMessage.Send( interaction.Performer, gameObject, NetTabType, TabAction.Open );
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		NetworkTabManager.Instance.RemoveTab(gameObject, NetTabType);
	}
}
