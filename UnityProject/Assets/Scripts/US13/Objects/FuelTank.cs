using Chemistry;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Health.Objects;
using US13.Items.Tool;
using US13.Objects.Chemistry;
using US13.Systems.Explosions;
using US13.Tilemaps.Behaviours.Objects;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Objects
{
	public class FuelTank : MonoBehaviour
	{
		private UniversalObjectPhysics objectBehaviour;
		private RegisterObject registerObject;
		private ReagentContainer reagentContainerScript;
		private Integrity integrity;
		private ReagentContainerObjectInteractionScript reagentContainerObjectInteractionScript;
		private bool BlewUp = false;

		[SerializeField]
		private Reagent fuel = default;

		private void Awake()
		{
			BlewUp = false;
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			registerObject = GetComponent<RegisterObject>();
			integrity = GetComponent<Integrity>();
			reagentContainerScript = this.GetCachedComponent<ReagentContainer>(includeDisabled: false);
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

			if (!Validations.HasUsedActiveWelder(interaction)) return;

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
			if (BlewUp) return;

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
				Explosion.StartExplosion(objectBehaviour.registerTile.WorldPositionServer, strength, stunNearbyPlayers: true);
			}
			else
			{
				Explosion.StartExplosion(registerObject.WorldPositionServer, strength, stunNearbyPlayers: true);
			}

			reagentContainerObjectInteractionScript.OnHandApply.RemoveListener(TryServerPerformInteraction);
			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
		}
	}
}
