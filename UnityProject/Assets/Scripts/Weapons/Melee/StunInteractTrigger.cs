using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunInteractTrigger : MeleeItemTrigger
{
	public float stunTime;

	private StunBaton stunBaton;

	public void Start()
	{
		stunBaton = GetComponent<StunBaton>();
	}

	public override bool MeleeItemInteract(GameObject originator, GameObject victim)
	{
		// If stun baton isn't active just beat the victim
		if (stunBaton && !stunBaton.isActive())
		{
			return true;
		}

		RegisterPlayer registerPlayerVictim = victim.GetComponent<RegisterPlayer>();

		if (victim && (stunBaton == null || stunBaton.isActive()))
		{
			registerPlayerVictim.Stun(stunTime);
			SoundManager.PlayNetworkedAtPos("Sparks0" + UnityEngine.Random.Range(1, 4), victim.transform.position);
		}

		return !originator.GetComponent<PlayerMove>().IsHelpIntent;
	}
}
