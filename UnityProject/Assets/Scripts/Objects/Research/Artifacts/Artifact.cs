using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Science;
using Systems.Research;
using ScriptableObjects.Systems.Research;
using Systems.Radiation;

[System.Serializable]
public class ArtifactSprite
{
	public SpriteDataSO idleSprite;
	public SpriteDataSO activeSprite;
}

namespace Objects.Research
{
	public class Artifact : MonoBehaviour, IServerSpawn, IServerDespawn, ICheckedInteractable<HandApply>
	{
		/// <summary>
		/// Set of all artifacts on scenes. Useful to get list of all existing artifacts.
		/// </summary>
		public readonly static HashSet<Artifact> ServerSpawnedArtifacts = new HashSet<Artifact>();

		[SerializeField]
		private SpriteHandler spriteHandler = null;

		private RadiationProducer radiationProducer = null;

		public ArtifactSprite[] RandomSprites;
		private ArtifactSprite currentSprite;

		public float TouchEffectTimeout = 10f;
		private float lastActivationTime;

		private Coroutine animationCoroutine = null;

		public ArtifactData artifactData = new ArtifactData();

		public ArtifactDataSO ArtifactDataSO;

		public bool UnderTimeout
		{
			get
			{
				// check that timeout has passed
				return Time.time - lastActivationTime < TouchEffectTimeout;
			}
		}

		#region Lifecycle

		private void Awake()
		{
			radiationProducer = GetComponent<RadiationProducer>();

			ArtifactClass Compostion;
			for (int i = 0; i < Random.Range(1,5); i++)
			{
				Compostion = (ArtifactClass)Random.Range(0, 3);

				switch (Compostion)
				{
					case ArtifactClass.Uranium:
						artifactData.radiationlevel += Random.Range(100, 500);
						break;
					case ArtifactClass.Bluespace:
						artifactData.bluespacesig += Random.Range(50, 150);
						break;
					case ArtifactClass.Bananium:
						artifactData.bananiumsig += Random.Range(100, 500);
						break;
					default:
						artifactData.radiationlevel += Random.Range(100, 500);
						break;

				}
			}

			artifactData.AreaEffect = ChooseAreaEffect(ArtifactDataSO.AreaEffects);
			artifactData.FeedEffect = ChooseFeedEffect(ArtifactDataSO.FeedEffects);

			if(artifactData.radiationlevel > 0)
			{
				radiationProducer.enabled = true;
				radiationProducer.SetLevel(artifactData.radiationlevel);
			}
		}

		private void OnEnable()
		{
			UpdateManager.Add(UpdateMe, artifactData.AreaEffect.coolDown);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		public void OnSpawnServer(SpawnInfo info)
		{

			// select random sprite
			ServerSelectRandomSprite();

			// add it to spawned artifacts registry for artifact detector
			if (!ServerSpawnedArtifacts.Contains(this))
				ServerSpawnedArtifacts.Add(this);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			// remove it from global artifacts registry
			if (ServerSpawnedArtifacts.Contains(this))
				ServerSpawnedArtifacts.Remove(this);
		}

		private void OnDestroy()
		{
			// remove it from global artifacts registry
			if (ServerSpawnedArtifacts.Contains(this))
				ServerSpawnedArtifacts.Remove(this);
		}

		#endregion

		private void UpdateMe()
		{
			if (!CustomNetworkManager.IsServer)
			{
				return;
			}

			AuraEffect();
		}

		#region Interactions
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			// check if player tried touch artifact
			if (interaction.Intent != Intent.Harm)
			{
				TryActivateByTouch(interaction);
			}
		}
		#endregion

		public void TryActivateByTouch(HandApply interaction)
		{
			if (!UnderTimeout)
			{
				if(interaction.HandObject == null)
				{
					//Contact Effect
				}
				else
				{
					artifactData.FeedEffect.DoEffectTouch(interaction);
				}
				PlayActivationAnimation();

				lastActivationTime = Time.time;
			}
		}

		private void AuraEffect()
		{
			artifactData.AreaEffect.DoEffectAura(this.gameObject);
			PlayActivationAnimation();
		}

		public void ServerSelectRandomSprite()
		{
			currentSprite = RandomSprites.PickRandom();
			spriteHandler?.SetSpriteSO(currentSprite.idleSprite);
		}

		public void PlayActivationAnimation()
		{
			if (animationCoroutine != null)
				StopCoroutine(animationCoroutine);
			animationCoroutine = StartCoroutine(ActivationAnimationRoutine());
		}

		private IEnumerator ActivationAnimationRoutine()
		{
			if (spriteHandler && currentSprite != null)
			{
				// set animation sprite
				spriteHandler.SetSpriteSO(currentSprite.activeSprite);
				// wait for animation to play (just random time)
				yield return WaitFor.Seconds(1f);
				// return back to idle state
				spriteHandler.SetSpriteSO(currentSprite.idleSprite);
			}
		}

		#region EffectRandomisation

		AreaArtifactEffect ChooseAreaEffect(List<ArtifactAreaEffectList> list)
		{
			int total = artifactData.radiationlevel / 2 + artifactData.bluespacesig + artifactData.bananiumsig / 2;
			int choice = Random.Range(0, total + 1);

			if (choice < (artifactData.radiationlevel / 2))
			{
				return list[(int)ArtifactClass.Uranium].AreaArtifactEffectList.PickRandom();
			}
			if (choice >= (artifactData.radiationlevel / 2) && choice < (artifactData.bluespacesig + artifactData.radiationlevel / 2))
			{
				return list[(int)ArtifactClass.Bluespace].AreaArtifactEffectList.PickRandom();
			}
			else
			{
				return list[(int)ArtifactClass.Bananium].AreaArtifactEffectList.PickRandom();
			}
		}

		FeedArtifactEffect ChooseFeedEffect(List<ArtifactFeedEffectList> list)
		{
			int total = artifactData.radiationlevel / 2 + artifactData.bluespacesig + artifactData.bananiumsig / 2;
			int choice = Random.Range(0, total + 1);

			if (choice < (artifactData.radiationlevel / 2))
			{
				return list[(int)ArtifactClass.Uranium].FeedArtifactEffectList.PickRandom();
			}
			if (choice >= (artifactData.radiationlevel / 2) && choice < (artifactData.bluespacesig + artifactData.radiationlevel / 2))
			{
				return list[(int)ArtifactClass.Bluespace].FeedArtifactEffectList.PickRandom();
			}
			else
			{
				return list[(int)ArtifactClass.Bananium].FeedArtifactEffectList.PickRandom();
			}
		}

		#endregion
	}
}
