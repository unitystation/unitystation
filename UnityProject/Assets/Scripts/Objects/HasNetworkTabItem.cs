using UnityEngine;

public class HasNetworkTabItem : MonoBehaviour, ICheckedInteractable<HandActivate>, IServerDespawn
{
	/// <summary>
	///     This is the same thing as HasNetworkTab but it works with items in the hand.
	///     This mean it can open up NetTabs when the object is activated in the hand
	/// </summary>
	[Tooltip("Network tab to display.")] public NetTabType NetTabType = NetTabType.None;

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
			return false;

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		NetworkTabManager.Instance.RemoveTab(gameObject, NetTabType);
	}
}