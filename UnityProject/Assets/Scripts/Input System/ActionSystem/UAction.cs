
/// <summary>
/// Base class for all Unitystation actions.
///
/// A UAction represents an action that can be performed to cause something to happen. Each UAction
/// has an associated interaction (which may be any of the Interaction subtypes). Each action may have
/// associated client and server-side validation logic, the actual logic of what the action does, and
/// client prediction / rollback.
/// </summary>
public abstract class UAction
{
	private Interaction interaction;

	protected UAction(Interaction interaction)
	{
		this.interaction = interaction;
	}

	/// <summary>
	/// Is this action currently possible? Default implementation is to use the default
	/// WIllInteract logic for the interaction.
	/// </summary>
	/// <param name="side">side of the network the validation is running on</param>
	/// <returns></returns>
	public virtual bool CanDo(NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	/// <summary>
	/// Invoked when this action is going to be requested of the server. Client
	/// can predict what happens before waiting for server. ServerRollbackClient should usually be
	/// implemented if this is implemented.
	/// </summary>
	public virtual void ClientPredictDo() { }

	/// <summary>
	/// Invoked when server-side CanDo returns false. Server should
	/// inform client of the correct state, because client's predicted state is wrong. Only needs to be implemented
	/// if ClientPredictDo is also implemented.
	/// </summary>
	public virtual void ServerRollbackClient() { }

	/// <summary>
	/// Perform the server-side logic of this action.
	/// </summary>
	public virtual void ServerDo() { }

	/// <summary>
	/// Performs all of the server-side logic for this action. Checks if action can really be done,
	/// and does it. If not, calls client rollback.
	/// </summary>
	public void ServerCheckAndDo()
	{
		if (!CanDo(NetworkSide.Server))
		{
			ServerRollbackClient();
		}
		else
		{
			ServerDo();
		}
	}
}
