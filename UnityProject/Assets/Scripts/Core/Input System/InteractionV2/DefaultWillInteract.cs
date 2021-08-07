using Systems.Interaction;


// TODO: namespace me to Systems.Interaction (have fun)
public static class DefaultWillInteract
{
	/// <summary>
	/// Chooses and invokes DefaultWillInteract method corresponding to the supplied type.
	/// </summary>
	/// <param name="interaction">interaction to check</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <typeparam name="T">type of interaction</typeparam>
	public static bool Default<T>(T interaction, NetworkSide side) where T : Interaction
	{
		if (typeof(T) == typeof(PositionalHandApply))
		{
			var positionalHandApply = interaction as PositionalHandApply;
			return Validations.CanApply(positionalHandApply.PerformerPlayerScript, positionalHandApply.TargetObject, side, targetVector: positionalHandApply.TargetVector);
		}
		if (typeof(T) == typeof(HandApply))
		{
			var handApply = interaction as HandApply;
			return Validations.CanApply(handApply.PerformerPlayerScript, handApply.TargetObject, side);
		}
		if (typeof(T) == typeof(AimApply))
		{
			return AimApply(interaction as AimApply, side);
		}
		if (typeof(T) == typeof(MouseDrop))
		{
			return Validations.CanInteract(interaction.PerformerPlayerScript, side);
		}
		if (typeof(T) == typeof(HandActivate))
		{
			return Validations.CanInteract(interaction.PerformerPlayerScript, side);
		}
		if (typeof(T) == typeof(InventoryApply))
		{
			return Validations.CanInteract(interaction.PerformerPlayerScript, side);
		}
		if (typeof(T) == typeof(TileApply))
		{
			var tileApply = interaction as TileApply;
			return Validations.CanApply(tileApply.PerformerPlayerScript, tileApply.TargetInteractableTiles.gameObject, side, targetVector: tileApply.TargetVector);
		}
		if (typeof(T) == typeof(ConnectionApply))
		{
			var connectionApply = interaction as ConnectionApply;
			return Validations.CanApply(connectionApply.PerformerPlayerScript, connectionApply.TargetObject, side, targetVector: connectionApply.TargetVector);
		}
		if (typeof(T) == typeof(ContextMenuApply))
		{
			var contextMenuApply = interaction as ContextMenuApply;
			return Validations.CanApply(contextMenuApply.PerformerPlayerScript, contextMenuApply.TargetObject, side);
		}
		if (typeof(T) == typeof(AiActivate))
		{
			return Validations.CanApply(interaction as AiActivate, side);
		}

		Logger.LogError("Unable to recognize interaction type.", Category.Interaction);
		return false;
	}

	public static bool AimApply(AimApply interaction, NetworkSide side)
	{
		if ( !Validations.CanInteract(interaction.PerformerPlayerScript, side) )
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
