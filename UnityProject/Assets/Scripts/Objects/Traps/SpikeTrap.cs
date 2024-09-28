
using UnityEngine;
using NaughtyAttributes;
using Core.Physics;
using System.Collections;
using Shared.Systems.ObjectConnection;

namespace Objects.Traps
{

	public class SpikeTrap : FloorHazard, IGenericTrigger, IMultitoolLinkable
	{
		private const int ENABLED_SPRITE_INDEX = 1;
		private const int DISABLED_SPRITE_INDEX = 0;
		private const int ENABLING_SPRITE_INDEX = 2;
		private const int DISABLING_SPRITE_INDEX = 3;

		private const float ENABLE_ANIMATION_LENGTH = 0.16f;
		private const float DISABLE_ANIMATION_LENGTH = 0.2f;

		[SerializeField]
		private SpriteHandler spriteHandler = null;


		private bool spikeEnabled = false;

		[field: SerializeField] public bool CanRelink { get; set; } = true;
		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.GenericTrigger;

		[field: SerializeField] public TriggerType TriggerType { get; protected set; }



		public void OnTrigger()
		{
			if (TriggerType == TriggerType.Toggle) ToggleSpike();
			else if(spikeEnabled == false) StartCoroutine(EnableSpike());
		}

		private void ToggleSpike()
		{
			if (spikeEnabled == true) StartCoroutine(DisableSpike());
			else StartCoroutine(EnableSpike());
		}

		public void OnTriggerEnd()
		{
			if (TriggerType != TriggerType.Active) return;
			StartCoroutine(DisableSpike());
		}

		private IEnumerator DisableSpike()
		{
			spikeEnabled = false;
			spriteHandler.SetSpriteVariant(DISABLING_SPRITE_INDEX);
			yield return new WaitForSeconds(DISABLE_ANIMATION_LENGTH);
			spriteHandler.SetSpriteVariant(DISABLED_SPRITE_INDEX);
		}

		private IEnumerator EnableSpike()
		{
			spikeEnabled = true;
			spriteHandler.SetSpriteVariant(ENABLING_SPRITE_INDEX);
			yield return new WaitForSeconds(ENABLE_ANIMATION_LENGTH);
			spriteHandler.SetSpriteVariant(ENABLED_SPRITE_INDEX);
		}

		public override bool WillAffectPlayer(PlayerScript playerScript)
		{
			return playerScript.PlayerType == PlayerTypes.Normal;
		}

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			if (spikeEnabled == false) return;
			base.OnPlayerStep(playerScript);
		}

		public override void OnObjectEnter(GameObject eventData)
		{
			if (spikeEnabled == false) return;
			base.OnObjectEnter(eventData);
		}
	}
}
