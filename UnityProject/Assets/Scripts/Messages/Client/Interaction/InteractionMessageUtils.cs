

using UnityEngine;

/// <summary>
/// Util methods for dealing with interaction messages in Interaction Framework V2
/// </summary>
public static class InteractionMessageUtils
{

	/// <summary>
	/// Send a request to the server to validate + perform the interaction.
	/// </summary>
	/// <param name="info">info on the interaction being performed. Each object involved in the interaction
	/// must have a networkidentity.</param>
	/// <param name="processor">component which will process the interaction on the server-side. The processor's
	/// info and result types must match the info and result type of one of the interaction type constants
	/// defined in InteractionType.
	///
	/// This component
	/// must live on a GameObject with a network identity, and there must only be one instance of this component
	/// on the object. For organization, we suggest that the component which is sending this message
	/// should be the processor, as such this parameter should almost always be passed using the "this" keyword, and
	/// should almost always be either a component on the target object or a component on the used object</param>
	/// <typeparamref name="T">Interaction subtype
	/// for the interaction that the processor can handle (such as MouseDrop for a mouse drop interaction).
	/// Must be a subtype of Interaction.</typeparamref>
	public static void SendRequest<T>(T info, IInteractionProcessor<T> processor)
		where T : Interaction
	{
		if (!info.Performer.Equals(PlayerManager.LocalPlayer))
		{
			Logger.LogError("Client attempting to perform an interaction on behalf of another player." +
			                " This is not allowed. Client can only perform an interaction as themselves. Message" +
			                " will not be sent.", Category.NetMessage);
			return;
		}

		if (!(processor is Component))
		{
			Logger.LogError("processor must be a component, but isn't. The message will not be sent.", Category.NetMessage);
			return;
		}

		//send the message appropriate to the specific interaction type
		var processorObject = (processor as Component).gameObject;
		if (typeof(T) == typeof(PositionalHandApply))
		{
			RequestPositionalHandApplyMessage.Send(info as PositionalHandApply, processorObject);
			return;
		}
		else if (typeof(T) == typeof(HandApply))
		{
			RequestHandApplyMessage.Send(info as HandApply, processorObject);
			return;
		}
		else if (typeof(T) == typeof(AimApply))
		{
			RequestAimApplyMessage.Send(info as AimApply, processorObject);
			return;
		}
		else if (typeof(T) == typeof(MouseDrop))
		{
			RequestMouseDropMessage.Send(info as MouseDrop, processorObject);
			return;
		}
		else if (typeof(T) == typeof(HandActivate))
		{
			RequestHandActivateMessage.Send(info as HandActivate, processorObject);
			return;
		}
		else if (typeof(T) == typeof(InventoryApply))
		{
			RequestInventoryApplyMessage.Send(info as InventoryApply, processorObject);
			return;
		}

		//TODO: Other types

		//we didn't send anything
		Logger.LogErrorFormat("Interaction type was {0} - we couldn't determine what to do for this interaction" +
		                      " type, most likely because it hasn't been implemented yet." +
		                      " Please implement handling for this interaction type in InteractionMessageUtils.SendRequest()", Category.NetMessage, nameof(T));
	}

	/// <summary>
	/// Get all interaction processors in the specified game object which can process an interaction of
	/// the specified type.
	/// </summary>
	/// <param name="gameObject">object to check</param>
	/// <typeparam name="T">type of interaction whose processors should be retrieved</typeparam>
	/// <returns>the processors found. Null if none found.</returns>
	public static IInteractionProcessor<T>[] TryGetProcessors<T>(GameObject gameObject)
		where T : Interaction
	{
		var processorComponents = gameObject.GetComponents<IInteractionProcessor<T>>();
		if (processorComponents == null || processorComponents.Length == 0)
		{
			Logger.LogError("Processor component could not be looked up by the ID sent by the client, " +
			                "this is most likely a programming error. Message will not be processed", Category.NetMessage);
			return null;
		}

		return processorComponents;
	}
}
