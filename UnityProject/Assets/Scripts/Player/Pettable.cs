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
		string npcName;
		var npc = interaction.TargetObject.GetComponent<MobAI>();
		
		if (npc == null)
		{
			npcName = interaction.TargetObject.name;
		}
		else
		{
			npcName = npc.mobName;
		} 

		Chat.AddActionMsgToChat(
			interaction.Performer,
			$"You pet {npcName}.",
			$"{interaction.Performer.ExpensiveName()} pets {npcName}.");
		
		if(npc != null) 
		{
			gameObject.GetComponent<MobAI>().OnPetted(interaction.Performer.gameObject);
		}
	}
}
