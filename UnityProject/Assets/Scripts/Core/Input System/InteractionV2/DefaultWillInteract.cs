using Systems.Interaction;


// TODO: namespace me to Systems.Interaction (have fun)
public static class DefaultWillInteract
{
	/// <summary>
	/// Chooses and invokes DefaultWillInteract method corresponding to the supplied type.
	/// </summary>
	/// <param name="interaction">interaction to check</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <param name="aps"></param>
	/// <typeparam name="T">type of interaction</typeparam>
	public static bool Default<T>(T interaction, NetworkSide side, PlayerTypes aps = PlayerTypes.Normal) where T : Interaction
	{
		if (typeof(T) == typeof(PositionalHandApply))
		{
			var positionalHandApply = interaction as PositionalHandApply;
			return Validations.CanApply(positionalHandApply.PerformerPlayerScript, positionalHandApply.TargetObject,
				side, targetPosition: positionalHandApply.TargetPosition, aps: aps);
		}
		if (typeof(T) == typeof(HandApply))
		{
			var handApply = interaction as HandApply;
			return Validations.CanApply(handApply.PerformerPlayerScript, handApply.TargetObject, side,
				aps: aps);
		}
		if (typeof(T) == typeof(AimApply))
		{
			return AimApply(interaction as AimApply, side, aps);
		}
		if (typeof(T) == typeof(MouseDrop))
		{
			return Validations.CanInteract(interaction.PerformerPlayerScript, side, aps: aps);
		}
		if (typeof(T) == typeof(HandActivate))
		{
			return Validations.CanInteract(interaction.PerformerPlayerScript, side, aps: aps);
		}
		if (typeof(T) == typeof(InventoryApply))
		{
			return Validations.CanInteract(interaction.PerformerPlayerScript, side, aps: aps);
		}
		if (typeof(T) == typeof(TileApply))
		{
			var tileApply = interaction as TileApply;
			return Validations.CanApply(tileApply.PerformerPlayerScript, tileApply.TargetInteractableTiles.gameObject,
				side, targetPosition: tileApply.TargetPosition, aps: aps);
		}
		if (typeof(T) == typeof(ConnectionApply))
		{
			var connectionApply = interaction as ConnectionApply;
			return Validations.CanApply(connectionApply.PerformerPlayerScript, connectionApply.TargetObject,
				side, targetPosition: connectionApply.TargetPosition, aps: aps);
		}
		if (typeof(T) == typeof(ContextMenuApply))
		{
			var contextMenuApply = interaction as ContextMenuApply;
			return Validations.CanApply(contextMenuApply.PerformerPlayerScript, contextMenuApply.TargetObject, side,
				aps: aps);
		}
		if (typeof(T) == typeof(AiActivate))
		{
			return Validations.CanApply(interaction as AiActivate, side);
		}

		Logger.LogError("Unable to recognize interaction type.", Category.Interaction);
		return false;
	}

	private static bool AimApply(AimApply interaction, NetworkSide side,
		PlayerTypes allowedPlayerTypes = PlayerTypes.Normal)
	{
		if (Validations.CanInteract(interaction.PerformerPlayerScript, side, aps: allowedPlayerTypes) == false)
		{
			return false;
		}

		bool? isNotHidden = true;
		if (side == NetworkSide.Client)
		{
			//local player is performing interaction
			isNotHidden = !PlayerManager.LocalPlayerScript.IsHidden;
		}
		else
		{
			//server is validating the interaction
			isNotHidden = !interaction.Performer.Player()?.Script.IsHidden;
		}

		return isNotHidden.GetValueOrDefault( true );
	}

	/// <summary>
	/// Default WillInteract logic for AiActivate interactions
	/// </summary>
	public static bool AiActivate(AiActivate interaction, NetworkSide side, bool lineCast = true)
	{
		return Validations.CanApply(interaction, side, lineCast);
	}
}
