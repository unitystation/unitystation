using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry.Components;
using UnityEngine.Events;

public class FuelTank : MonoBehaviour
{
	private ObjectBehaviour objectBehaviour;
	private RegisterObject registerObject;
	private ReagentContainer reagentContainerScript;
	private Integrity integrity;
	private ReagentContainerObjectInteractionScript reagentContainerObjectInteractionScript;
	private bool BlewUp = false;

	[SerializeField]
	private Chemistry.Reagent fuel;

	private void Awake()
	{
		BlewUp = false;
		objectBehaviour = GetComponent<ObjectBehaviour>();
		registerObject = GetComponent<RegisterObject>();
		integrity = GetComponent<Integrity>();
		reagentContainerScript = GetComponent<ReagentContainer>();
		reagentContainerObjectInteractionScript = GetComponent<ReagentContainerObjectInteractionScript>();
		integrity.OnWillDestroyServer.AddListener(WhenDestroyed);
	}

	private void Start()
	{
		reagentContainerObjectInteractionScript.OnHandApply.AddListener(TryServerPerformInteraction);
	}

	public void TryServerPerformInteraction(HandApply interaction)
	{
		if (!Validations.IsTarget(gameObject, interaction)) return;

		if(!Validations.HasUsedActiveWelder(interaction)) return;

		var welder = interaction.UsedObject.GetComponent<Welder>();

		if (welder == null) return;

		if (!welder.IsOn)
		{
			return;
		}

		var strength = reagentContainerScript[fuel];

		if (strength + 1 < integrity.integrity)
		{
			Chat.AddExamineMsg(interaction.Performer, "You realise you forgot to turn off the welder, luckily the fuel tank seems stable.");
			return;
		}

		Chat.AddExamineMsg(interaction.Performer, "<color=red>You have a sudden realisation that you forgot to do something, but it is too late...</color>");

		Explode(strength);
	}

	private void WhenDestroyed(DestructionInfo info)
	{
		if(BlewUp) return;

		Explode(reagentContainerScript[fuel]);
	}


	private void Explode(float strength)
	{
		if (strength < 400f)
		{
			strength = 400f;
		}

		BlewUp = true;

		if (registerObject == null)
		{
			Explosions.Explosion.StartExplosion(objectBehaviour.registerTile.LocalPosition, strength,
				objectBehaviour.registerTile.Matrix);
		}
		else
		{
			Explosions.Explosion.StartExplosion(registerObject.LocalPosition, strength,
				registerObject.Matrix);
		}

		reagentContainerObjectInteractionScript.OnHandApply.RemoveListener(TryServerPerformInteraction);
		integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
	}
}
