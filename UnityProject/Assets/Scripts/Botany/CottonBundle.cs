using System.Collections;
using System;
using UnityEngine;
using Mirror;

public class CottonBundle : MonoBehaviour, ICheckedInteractable<HandActivate>
{

	[Tooltip("What you get when you use this in your hand.")]
	[SerializeField]
	private GameObject result = null;

    private int seedModifier;

    private float cottonPotency;

    private int finalAmount;

    private GrownFood grownFood;
    
    private void Awake()
    {
    	///Getting GrownFood so I can snag the potency value.
        grownFood = GetComponent<GrownFood>();
        cottonPotency = grownFood.GetPlantData().Potency;
        ///calculating how much cotton/durathread you should get.
		seedModifier = Mathf.RoundToInt(cottonPotency / 25f);
		finalAmount = seedModifier + 1;
    }

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		return true;
	}

    public void ServerPerformInteraction(HandActivate interaction)
	{
		Despawn.ServerSingle(gameObject);
		Spawn.ServerPrefab(result, interaction.Performer.WorldPosServer(), count: finalAmount);
		Chat.AddExamineMsgFromServer(interaction.Performer, "You pull some raw material out of the bundle!");
	}

}