
using System;

/// <summary>
/// Utilities used in IF2 for various core interaction components
/// </summary>
public static class InteractionComponentUtils
{
	/// <summary>
	/// Uses the coordinator to validate and attempt the interaction on the server side.
	/// </summary>
	/// <param name="info"></param>
	/// <param name="coordinator"></param>
	/// <param name="onValidationFail"></param>
	/// <typeparam name="T">type of interaction</typeparam>
	/// <returns>If validation succeeds and
	/// coordinator performs server side interaction, returns
	/// STOP_PROCESSING. Otherwise returns CONTINUE_PROCESSING and invokes the onValidationFail callback.</returns>
	public static InteractionControl ServerProcessCoordinatedInteraction<T>(T info, InteractionCoordinator<T> coordinator,
		Action<T> onValidationFail)
		where T : Interaction
	{
		if (coordinator.ServerValidateAndPerform(info) == ValidationResult.SUCCESS)
		{
			return InteractionControl.STOP_PROCESSING;
		}
		else
		{
			onValidationFail.Invoke(info);
			return InteractionControl.CONTINUE_PROCESSING;
		}
	}

	/// <summary>
	/// Uses the coordinator to validate and attempt the interaction on the client side.
	/// </summary>
	/// <param name="info"></param>
	/// <param name="coordinator"></param>
	/// <param name="onValidationSuccess"></param>
	/// <typeparam name="T">type of interaction</typeparam>
	/// <returns>If validation succeeds and coordinator sends the interaciton msg, invokes
	/// onValidationSuccess and returns STOP_PROCESSING. Otherwise returns CONTINUE_PROCESSING.</returns>
	public static InteractionControl CoordinatedInteract<T>(T info, InteractionCoordinator<T> coordinator,
		Action<T> onValidationSuccess)
		where T : Interaction
	{
		var validated = coordinator.ClientValidateAndRequest(info);
		if (validated == ValidationResult.SUCCESS)
		{
			//success, so do client prediction if not server player
			if (!CustomNetworkManager.Instance._isServer)
			{
				onValidationSuccess.Invoke(info);
			}
		}

		return validated == ValidationResult.SUCCESS ? InteractionControl.STOP_PROCESSING : InteractionControl.CONTINUE_PROCESSING;
	}
}
