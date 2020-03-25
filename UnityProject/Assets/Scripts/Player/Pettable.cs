using UnityEngine;

/// <summary>
/// Allows an object to be pet by a player. Shameless copy of Huggable.cs
/// </summary>
public class Pettable : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public bool WillInteract( HandApply interaction, NetworkSide side )
	{
		var NPCHealth = interaction.TargetObject.GetComponent<LivingHealthBehaviour>();
		if (!DefaultWillInteract.Default(interaction, side) || NPCHealth.IsDead || interaction.Intent != Intent.Help) return false;
 
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		string NPCName;
		var NPC = interaction.TargetObject.GetComponent<MobAI>();
		
		if (NPC == null) NPCName = interaction.TargetObject.name; else NPCName = NPC.mobName;

		Chat.AddActionMsgToChat(interaction.Performer,
		$"You pet {NPCName}.", $"{interaction.Performer.ExpensiveName()} pets {NPCName}.");
		if(NPC != null) gameObject.GetComponent<MobAI>().OnPetted(interaction.Performer.gameObject);
	}
}
