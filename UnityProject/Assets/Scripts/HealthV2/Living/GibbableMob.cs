using AddressableReferences;
using HealthV2.Living;
using Mirror;
using UnityEngine;
using Util.Independent.FluentRichText;

namespace HealthV2
{
	[RequireComponent(typeof(LivingHealthMasterBase))]
	public class GibbableMob : NetworkBehaviour, IGib
	{
		public bool IsEnabled = true;
		[SerializeField] private LivingHealthMasterBase health;
		[SerializeField] private SkinnableMob skinnableMob;
		[SerializeField] private PlayerScript mob;
		[SerializeField] private AddressableAudioSource defaultGibSound;

		public LivingHealthMasterBase Health => health;
		public PlayerScript Mob => mob;

		[Server]
		public void OnGib()
		{
			PlayAudio();
			if (IsEnabled == false || GameManager.Instance.GibbingAllowed == false)
			{
				Chat.AddActionMsgToChat(gameObject,
					$"{mob.visibleName}'s body stretches violently before falling to the ground.".Color(Color.red));
				mob.ObjectPhysics.NewtonianPush(new Vector2().RandomDirection(),500,
					Random.Range(6, 55) / 100f, 8, (BodyPartType) Random.Range(0, 13), gameObject, Random.Range(0, 13));
				health.Death();
				return;
			}
			//Prepare for gibbing.
			mob.Mind.OrNull()?.Ghost();
			Inventory.ServerDropAll(mob.DynamicItemStorage);

			if (skinnableMob != null) skinnableMob.SpawnSpeciesProduce(4);
			health.Death();

			//drop everything now
			health.reagentPoolSystem?.Bleed(health.reagentPoolSystem.GetTotalBlood());
			RemoveAllBodyParts();
			mob.ObjectPhysics.DisappearFromWorld();
		}

		private void PlayAudio()
		{
			// TODO: Gibbing sounds are different for various mobs. We'll need to read it from their species SO when we add them.
			_ = SoundManager.PlayAtPosition(defaultGibSound, gameObject.AssumedWorldPosServer(),
				gameObject);
		}

		private void RemoveAllBodyParts()
		{
			for (int i = health.BodyPartList.Count - 1; i >= 0; i--)
			{
				if (health.BodyPartList[i].BodyPartType == BodyPartType.Chest) continue;
				health.BodyPartList[i].TryRemoveFromBody(true, PreventGibb_Death: true);
			}
		}
	}
}