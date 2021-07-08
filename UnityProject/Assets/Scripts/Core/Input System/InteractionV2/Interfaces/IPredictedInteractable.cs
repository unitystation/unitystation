/// <summary>
/// Indicates an interactable component which can perform client prediction
/// </summary>
public interface IPredictedInteractable<T> : IInteractable<T>
	where T : Interaction
{
	/// <summary>
	/// Client-side prediction. Called after Willinteract returns true on client side.
	/// Client can perform client side prediction. NOT invoked for server player, since there is no need
	/// for prediction.
	/// </summary>
	void ClientPredictInteraction(T interaction);

	/// <summary>
	/// Called when server-side validation fails. Server can use this hook to use whatever
	/// means necessary to tell client to roll back its prediction and get back in sync with server.
	/// </summary>
	/// <param name="interaction"></param>
	void ServerRollbackClient(T interaction);

	//TODO: Maybe add ClientRollbackPrediction which would be a client-side rollback hook,
	//server could send message back to client when validation fails so client knows to rollback.
	//Currently there is no need for such mechanism as there are already other ways outside of IF2
	//for server to tell client to roll back.
}
