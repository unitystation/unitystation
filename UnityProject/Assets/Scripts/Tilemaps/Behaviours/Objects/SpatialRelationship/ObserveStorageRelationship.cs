
using System;

/// <summary>
/// Relationship where one player is observing some interactable storage, which ends when they go out of
/// range, but also ends if the observed storage is a player's storage and that player gets into a state where they aren't allowed to be observed
/// (uncuffed, conscious, unslipped)
/// </summary>
public class ObserveStorageRelationship : RangeRelationship
{
	private readonly PlayerMove observedPlayerMove;
	private readonly RegisterPlayer observedRegisterPlayer;
	private readonly PlayerHealth observedPlayerHealth;
	public readonly RegisterPlayer ObserverPlayer;
	public readonly InteractableStorage ObservedStorage;

	private ObserveStorageRelationship(InteractableStorage observedStorage, RegisterPlayer observer,
		float maxRange, Action<ObserveStorageRelationship> onObservationEnded) :
		base(observedStorage.GetComponent<RegisterTile>(), observer, maxRange, (rship) => onObservationEnded.Invoke(rship as ObserveStorageRelationship))
	{
		this.ObservedStorage = observedStorage;
		this.ObserverPlayer = observer;
		//check if the observed storage is in a player's inventory, and if so, populate the fields / event hooks
		var rootStorage = observedStorage.ItemStorage.GetRootStorage();
		this.observedPlayerMove = rootStorage.GetComponent<PlayerMove>();
		if (observedPlayerMove != null)
		{
			this.observedRegisterPlayer = rootStorage.GetComponent<RegisterPlayer>();

			this.observedPlayerHealth = rootStorage.GetComponent<PlayerHealth>();

			//add listeners for non-range-based ways in which the relationship can end
			observedPlayerMove.OnCuffChangeServer.AddListener(OnCuffChangeServer);
			observedRegisterPlayer.OnSlipChangeServer.AddListener(OnSlipChangeServer);
			observedPlayerHealth.OnConsciousStateChangeServer.AddListener(OnConsciousStateChangeServer);
		}
	}

	private void OnConsciousStateChangeServer(ConsciousState oldState, ConsciousState newState)
	{
		//stop the relationship if observed player becomes conscious
		if (newState == ConsciousState.CONSCIOUS)
		{
			SpatialRelationship.ServerEnd(this);
		}
	}

	private void OnSlipChangeServer(bool wasSlipped, bool nowSlipped)
	{
		//stop the relationship if observed player becomes unslipped
		if (!nowSlipped)
		{
			SpatialRelationship.ServerEnd(this);
		}
	}

	private void OnCuffChangeServer(bool wasCuffed, bool nowCuffed)
	{
		//stop the relationship if observed player becomes uncuffed
		if (!nowCuffed)
		{
			SpatialRelationship.ServerEnd(this);
		}
	}

	/// <summary>
	/// Defines a relationship in which the onObservationEnded action is invoked when
	/// the storage is no longer allowed to be observed by the observer.
	/// </summary>
	/// <param name="observedStorage">interactable storage being observed</param>
	/// <param name="observer">player who is trying to observe the storage</param>
	/// <param name="maxRange">max range allowed for continued observation</param>
	/// <param name="onObservationEnded">invoked when relationship is ended</param>
	public static ObserveStorageRelationship Observe(InteractableStorage observedStorage, RegisterPlayer observer,
		float maxRange, Action<ObserveStorageRelationship> onObservationEnded)
	{
		return new ObserveStorageRelationship(observedStorage, observer, maxRange, onObservationEnded);
	}

	public override void OnRelationshipEnded()
	{
		base.OnRelationshipEnded();

		if (observedPlayerMove != null)
		{
			//remove our listeners
			observedPlayerMove.OnCuffChangeServer.RemoveListener(OnCuffChangeServer);
			observedRegisterPlayer.OnSlipChangeServer.RemoveListener(OnSlipChangeServer);
			observedPlayerHealth.OnConsciousStateChangeServer.RemoveListener(OnConsciousStateChangeServer);
		}
	}
}
