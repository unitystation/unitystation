using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunInteractTrigger : MeleeIemTrigger
{
	public float stunTime;

	public string sound;

	private StunBaton stunBaton;

	public void Start()
	{
		stunBaton = GetComponent<StunBaton>();
	}

	public override bool MeleeItemInteract(GameObject victim)
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
			SoundManager.PlayNetworkedAtPos(sound, victim.transform.position);
		}

		return UIManager.CurrentIntent != Intent.Help;
	}
}
