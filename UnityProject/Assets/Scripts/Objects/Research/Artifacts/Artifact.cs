using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Science;
using Systems.Research;
using ScriptableObjects.Systems.Research;
using Systems.Radiation;
using Mirror;

[System.Serializable]
public class ArtifactSprite
{
	public SpriteDataSO idleSprite;
	public SpriteDataSO activeSprite;
}

namespace Objects.Research
{
	public struct EffectIndex
	{
		public EffectIndex(int Index = 0, ArtifactClass AClass = ArtifactClass.Uranium)
		{
			index = Index;
			aClass = AClass;
		}
		public int index;
		public ArtifactClass aClass;
	}

	public struct ArtifactData
	{
		public ArtifactData(int radlvl = 0, int bluelvl = 0, int bnalvl = 0, int mss = 0, CompositionBase comp = CompositionBase.metal, EffectIndex areaEffect = default, EffectIndex feedEffect = default)
		{
			radiationlevel = radlvl;
			bluespacesig = bluelvl;
			bananiumsig = bnalvl;
			mass = mss;
			compositionBase = comp;
			AreaEffect = areaEffect;
			FeedEffect = feedEffect;
		}

		public CompositionBase compositionBase;
		public int radiationlevel;
		public int bluespacesig;
		public int bananiumsig;
		public int mass;
		public EffectIndex AreaEffect;
		public EffectIndex FeedEffect;
	}

	public class Artifact : NetworkBehaviour, IServerSpawn, IServerDespawn, ICheckedInteractable<HandApply>
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

		public bool isDormant = true;
		public ItemTrait DormantTrigger;

		private Coroutine animationCoroutine = null;

		[SyncVar] public ArtifactData artifactData = new ArtifactData();

		public ArtifactDataSO ArtifactDataSO;

		//Indexes and structs are used as opposed to the classes themselves due to Mirror and Garbage purposes
		public AreaArtifactEffect AreaEffect => ArtifactDataSO.AreaEffects[(int)artifactData.AreaEffect.aClass].AreaArtifactEffectList[artifactData.AreaEffect.index];
		public FeedArtifactEffect FeedEffect => ArtifactDataSO.FeedEffects[(int)artifactData.FeedEffect.aClass].FeedArtifactEffectList[artifactData.FeedEffect.index];


		[SyncVar] public string ID = "T376";

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
		}

		private void OnEnable()
		{
			if (isDormant) return;
			UpdateManager.Add(UpdateMe, AreaEffect.coolDown);
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


			ArtifactClass Compostion;
			for (int i = 0; i < Random.Range(1, 5); i++)
			{
				Compostion = (ArtifactClass)Random.Range(0, 3);

				switch (Compostion)
				{
					case ArtifactClass.Uranium:
						artifactData.radiationlevel += Random.Range(1000, 5000);
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

				int num = Random.Range(0, 26); 
				ID = $"{(char)('a' + num)}{Random.Range(0,1000).ToString("000")}"; //Generates a random ID in the form: Letter-Digit-Digit-Digit
				ID = ID.ToUpper();

				GetComponent<ObjectAttributes>().ServerSetArticleName("Artifact - " + ID);
			}

			artifactData.AreaEffect = ChooseAreaEffect();
			artifactData.FeedEffect = ChooseFeedEffect();

			if (artifactData.radiationlevel > 0)
			{
				radiationProducer.enabled = true;
				radiationProducer.SetLevel(artifactData.radiationlevel);
			}
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

			if(isDormant) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			else AuraEffect();
		}

		#region Interactions
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			// check if player tried touch artifact
			if (interaction.Intent != Intent.Harm && isDormant == false)
			{
				TryActivateByTouch(interaction);
			}
			if (isDormant && interaction.Intent != Intent.Harm && Validations.HasItemTrait(interaction.UsedObject, DormantTrigger))
			{
				isDormant = false;
				UpdateManager.Add(UpdateMe, AreaEffect.coolDown);
				Chat.AddCommMsgByMachineToChat(this.gameObject, $"{gameObject.ExpensiveName()} begins to humm quietly", ChatChannel.Local, Loudness.NORMAL);
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
					FeedEffect.DoEffectTouch(interaction);
				}
				PlayActivationAnimation();

				lastActivationTime = Time.time;
			}
		}

		private void AuraEffect()
		{
			AreaEffect.DoEffectAura(this.gameObject);
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

		EffectIndex ChooseAreaEffect()
		{
			int total = artifactData.radiationlevel / 20 + artifactData.bluespacesig + artifactData.bananiumsig / 2;
			int choice = Random.Range(0, total + 1);

			if (choice < (artifactData.radiationlevel / 2))
			{
				return new EffectIndex(Random.Range(0, ArtifactDataSO.AreaEffects[(int)ArtifactClass.Uranium].AreaArtifactEffectList.Count), ArtifactClass.Uranium);
			}
			if (choice >= (artifactData.radiationlevel / 20) && choice < (artifactData.bluespacesig + artifactData.radiationlevel / 20))
			{
				return new EffectIndex(Random.Range(0, ArtifactDataSO.AreaEffects[(int)ArtifactClass.Bluespace].AreaArtifactEffectList.Count), ArtifactClass.Bluespace);
			}
			else
			{
				return new EffectIndex(Random.Range(0, ArtifactDataSO.AreaEffects[(int)ArtifactClass.Bananium].AreaArtifactEffectList.Count), ArtifactClass.Bananium);
			}
		}

		EffectIndex ChooseFeedEffect()
		{
			int total = artifactData.radiationlevel / 20 + artifactData.bluespacesig + artifactData.bananiumsig / 2;
			int choice = Random.Range(0, total + 1);

			if (choice < (artifactData.radiationlevel / 2))
			{
				return new EffectIndex(Random.Range(0, ArtifactDataSO.FeedEffects[(int)ArtifactClass.Uranium].FeedArtifactEffectList.Count), ArtifactClass.Uranium);
			}
			if (choice >= (artifactData.radiationlevel / 20) && choice < (artifactData.bluespacesig + artifactData.radiationlevel / 20))
			{
				return new EffectIndex(Random.Range(0, ArtifactDataSO.FeedEffects[(int)ArtifactClass.Bluespace].FeedArtifactEffectList.Count), ArtifactClass.Bluespace);
			}
			else
			{
				return new EffectIndex(Random.Range(0, ArtifactDataSO.FeedEffects[(int)ArtifactClass.Bananium].FeedArtifactEffectList.Count), ArtifactClass.Bananium);
			}
		}

		#endregion
	}
}
