using Weapons.Projectiles.Behaviours;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Science;
using Systems.Research;
using ScriptableObjects.Systems.Research;
using Systems.Radiation;
using Shared.Systems.ObjectConnection;
using Mirror;
using Systems.Atmospherics;
using ScriptableObjects.Atmospherics;
using CustomInspectors;
using Random = UnityEngine.Random;

[System.Serializable]
public class ArtifactSprite
{
	public SpriteDataSO idleSprite;
	public SpriteDataSO activeSprite;
}

namespace Objects.Research
{
	public class Artifact : ImnterfaceMultitoolGUI, IServerSpawn, IServerDespawn, ICheckedInteractable<HandApply>, IMultitoolMasterable
	{
		/// <summary>
		/// Set of all artifacts on scenes. Useful to get list of all existing artifacts.
		/// </summary>
		public readonly static HashSet<Artifact> ServerSpawnedArtifacts = new HashSet<Artifact>();

		private static List<string> iDList = new List<string>();

		private Integrity integrity;
		private UniversalObjectPhysics objectPhysics;

		[SerializeField]
		private GameObject SliverPrefab = null;

		[SerializeField]
		private GameObject artifactPlayerTargetEffectSuccess = null;
		[SerializeField]
		private GameObject artifactPlayerTargetEffectFail = null;

		[SerializeField]
		private SpriteHandler spriteHandler = null;

		private RadiationProducer radiationProducer = null;

		private ArtifactSprite currentSprite;

		public float TouchEffectTimeout = 8;
		private float lastActivationTimeTouch;

		public float DamageEffectTimeout = 5f;
		private float lastActivationTimeDamage;

		public bool isDormant = true;
		public ItemTrait DormantTrigger;

		private int samplesTaken = 0;
		private int maxSamples = 5;

		private Coroutine animationCoroutine = null;

		[SyncVar] internal ArtifactData artifactData = new ArtifactData();

		public ArtifactDataSO ArtifactDataSO;

		public AreaEffectBase AreaEffect;
		public InteractEffectBase InteractEffect;
		public DamageEffectBase DamageEffect;

		private bool forceWallAreaEffect => AreaEffect is ForcefieldAreaEffect;

		[SerializeField]
		private DamageEffectBase forceWallDamageEffectSO = null;
		private bool forceWallDamageEffect = false;



		[SyncVar] public string ID = "T376";

		public bool UnderTimeoutTouch
		{
			get
			{
				// check that timeout has passed
				return Time.time - lastActivationTimeTouch < TouchEffectTimeout;
			}
		}

		public bool UnderTimeoutDamage
		{
			get
			{
				// check that timeout has passed
				return Time.time - lastActivationTimeDamage < DamageEffectTimeout;
			}
		}

		#region Lifecycle

		private void Awake()
		{
			integrity = GetComponent<Integrity>();
			radiationProducer = GetComponent<RadiationProducer>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();

			integrity.OnApplyDamage.AddListener(DoDamageEffect);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			//Sets appearance
			artifactData.Type = (ArtifactType)Random.Range(0, 3);
			maxSamples = Random.Range(3, 6);

			//Select random sprite
			ServerSelectRandomSprite();

			//Add it to spawned artifacts registry for artifact detector
			if (!ServerSpawnedArtifacts.Contains(this))
				ServerSpawnedArtifacts.Add(this);

			ArtifactClass Compostion;

			//Add elements to the artifacts compisition
			for (int i = 0; i < Random.Range(1, 5); i++)
			{
				//Choose what the artifact should be made of
				Compostion = (ArtifactClass)Random.Range(0, 3);

				//Adds properties to the artifact depending on what material has been added
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
						artifactData.radiationlevel += Random.Range(1000, 5000);
						break;
				}
			}

			//Randomises ID and adds it to name, this is done for two reasons:
			//To prevent duplication exploits, server knows if youve researched an artifact before and won't give you extra RP or credits for repeated research.
			//So players know which artifact is which if they have the same sprites
			//ID is in form: A000 - Z999
			bool generated = false;

			while (iDList.Contains(ID) == true || generated == false)
			{
				int num = Random.Range(0, 26);
				ID = $"{(char)('a' + num)}{Random.Range(0, 1000).ToString("000")}";
				ID = ID.ToUpperInvariant();
				generated = true;
			}

			iDList.Add(ID);

			GetComponent<ObjectAttributes>().ServerSetArticleName("Artifact - " + ID);

			ArtifactClass chosenClass;

			//Randomises the effects of the artifacts, probabilities of certain effects change with composition
			if (AreaEffect == null)
			{
				chosenClass = PickClass();
				AreaEffect = Instantiate(ArtifactDataSO.AreaEffects[(int)chosenClass].AreaArtifactEffectList.PickRandom());
			}
			if (InteractEffect == null)
			{
				chosenClass = PickClass();
				InteractEffect = Instantiate(ArtifactDataSO.InteractEffects[(int)chosenClass].InteractArtifactEffectList.PickRandom());
			}
			if (DamageEffect == null)
			{
				chosenClass = PickClass();
				var DamageEffectSO = ArtifactDataSO.DamageEffect[(int)chosenClass].DamageArtifactEffectList.PickRandom();
				DamageEffect = Instantiate(DamageEffectSO);

				forceWallDamageEffect = DamageEffectSO == forceWallDamageEffectSO;
			}

			if (AreaEffect.OverrideDormancy == true || DamageEffect.OverrideDormancy == true || InteractEffect.OverrideDormancy == true) isDormant = false;

			artifactData.AreaEffectValue = AreaEffect.GuessIndex;
			artifactData.InteractEffectValue = InteractEffect.GuessIndex;
			artifactData.DamageEffectValue = DamageEffect.GuessIndex;

			artifactData.ID = ID;

			ArtifactData temp = artifactData;
			artifactData = new ArtifactData();
			artifactData = temp;

			ToggleDormancy(isDormant); //Doesn't change dormant value but updates sprite

			//Initalises Radiation for artifacts with uranium composition.
			if (artifactData.radiationlevel > 0)
			{
				radiationProducer.NetSetActive(true);
				radiationProducer.SetLevel(artifactData.radiationlevel);
			}

			UpdateManager.Add(UpdateMe, AreaEffect.coolDown);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			integrity.OnApplyDamage.RemoveListener(DoDamageEffect);

			// remove it from global artifacts registry
			if (ServerSpawnedArtifacts.Contains(this))
				ServerSpawnedArtifacts.Remove(this);

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private void OnDestroy()
		{
			// remove it from global artifacts registry
			if (ServerSpawnedArtifacts.Contains(this))
				ServerSpawnedArtifacts.Remove(this);

			if (forceWallAreaEffect) ((ForcefieldAreaEffect)AreaEffect).TerminateObstructions(); //Ensures no forcewalls are left around when artifact is destroyed.
			if (forceWallDamageEffect)
			{
				var forcefieldEffect = ((AreaEffectOnDamage)DamageEffect).GetAreaEffect();
				((ForcefieldAreaEffect)forcefieldEffect).TerminateObstructions();
			}
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		#endregion

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void ClearStatics()
		{
			iDList = new List<string>();
		}

		private void UpdateMe()
		{

			if (!CustomNetworkManager.IsServer)
			{
				return;
			}

			if (isDormant == false)
			{
				CheckAtmosphere();
				DoAuraEffect();
			}
		}

		#region Interactions

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return interaction.Intent != Intent.Harm && DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			switch (artifactData.Type) // Try Take Sample
			{
				case ArtifactType.Organic:
					if(Validations.HasItemTrait(interaction.UsedObject, ArtifactDataSO.OrganicSampleTrait))
					{
						TakeSample(interaction);
						return;
					}
					break;
				case ArtifactType.Mechanical:
					if (Validations.HasUsedActiveWelder(interaction))
					{
						 TakeSample(interaction);
						 return;
					}
					break;
				case ArtifactType.Geological:
					if (Validations.HasItemTrait(interaction.UsedObject, ArtifactDataSO.GeologicalSampleTrait))
					{
						TakeSample(interaction);
						return;
					}
					break;
				default:
					if (Validations.HasItemTrait(interaction.UsedObject, ArtifactDataSO.GeologicalSampleTrait))
					{
						TakeSample(interaction);
						return;
					}
					break;
			}

			if (isDormant && Validations.HasItemTrait(interaction.UsedObject, DormantTrigger))
			{
				ToggleDormancy(false);
				Chat.AddActionMsgToChat(gameObject, $"{gameObject.ExpensiveName()} begins to humm quietly!");
			}

			TryActivateByTouch(interaction);

		}
		private void TakeSample(HandApply interaction)
		{
			if(samplesTaken >= maxSamples)
			{
				Chat.AddWarningMsgFromServer(interaction.Performer.gameObject, "No more samples can be taken from artifact without destablisation!");
				return;
			}

			ToolUtils.ServerUseToolWithActionMessages(interaction, 5f,
				$"You begin extracting a sample from {gameObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} begins extracting a sample from {gameObject.ExpensiveName()}...",
				$"You extract a sample from {gameObject.ExpensiveName()}.",
				$"{interaction.Performer.ExpensiveName()} extracts a sample from {gameObject.ExpensiveName()}.",
				() =>
				{
					GameObject sliver = Spawn.ServerPrefab(SliverPrefab, gameObject.AssumedWorldPosServer()).GameObject;
					if (sliver.TryGetComponent<ArtifactSliver>(out var sliverComponent)) sliverComponent.SetUpValues(artifactData, ID + $":{(char)('a' + samplesTaken)}");
					samplesTaken++;
					DoDamageEffect();
				});
		}

		#endregion

		private void ToggleDormancy(bool _isDormant)
		{
			isDormant = _isDormant;
			if(isDormant)
			{
				spriteHandler.SetColor(spriteHandler.Palette[1]);
			}
			else
			{
				spriteHandler.SetColor(spriteHandler.Palette[0]);
			}
		}

		void DoDamageEffect(DamageInfo damageInfo = null)
		{
			if(isDormant)
			{
				if(DMMath.Prob(50))
				{
					Chat.AddActionMsgToChat(gameObject, "The anomaly begins to gently humm!");
					ToggleDormancy(false);
				}
				else
				{
					Chat.AddActionMsgToChat(gameObject, $"{gameObject.ExpensiveName()} quivers as a crack forms along its edge!");
				}
			}
			if (isDormant == false && !UnderTimeoutDamage)
			{
				PlayActivationAnimation();
				DamageEffect.DoEffect(damageInfo, objectPhysics);
				lastActivationTimeDamage = Time.time;
			}
		}

		public void TryActivateByTouch(HandApply interaction)
		{
			if(isDormant)
			{
				if (DMMath.Prob(10))
				{
					Chat.AddActionMsgToChat(interaction.Performer, "You touch the anomaly, a chill goes down your spine as the anomaly begins to humm quietly...",
						$"{interaction.Performer.ExpensiveName()} touches the anomaly, a chill goes down your spine as the anomaly begins to humm quietly...");
					ToggleDormancy(false);
				}
				else
				{
					Chat.AddActionMsgToChat(interaction.Performer, "You touch the anomaly, it twitches slightly, but remains dormant...",
						$"{interaction.Performer.ExpensiveName()} touches the anomaly, it twitches slightly, but remains dormant...");
				}
			}
			else if(!UnderTimeoutTouch)
			{
				InteractEffect.DoEffectTouch(interaction);
				PlayActivationAnimation();
				lastActivationTimeTouch = Time.time;
			}
		}

		private void DoAuraEffect()
		{
			AreaEffect.DoEffectAura(this.gameObject);
			PlayActivationAnimation();
		}

		private void CheckAtmosphere()
		{
			GasSO gastype = Gas.NitrousOxide;

			Matrix matrix = integrity.RegisterTile.Matrix;
			Vector3Int localPosition = MatrixManager.WorldToLocalInt(objectPhysics.registerTile.WorldPosition, matrix);
			GasMix ambientGasMix = matrix.MetaDataLayer.Get(localPosition).GasMix;

			ambientGasMix.GasData.GetGasMoles(gastype, out var moles);

			moles *= 5;

			if (moles > 0 && DMMath.Prob(Mathf.Clamp(moles, 0, 100)))
			{
				ToggleDormancy(true);
				Chat.AddActionMsgToChat(gameObject, $"{gameObject.ExpensiveName()} falls dormant...");
			}
		}

		[TargetRpc]
		public void SpawnClientEffect(NetworkConnection target, bool successful, Vector3 spawnDestination)
		{
			var effect = Spawn.ClientPrefab(successful == true ? artifactPlayerTargetEffectSuccess : artifactPlayerTargetEffectFail, spawnDestination).GameObject;

			var timeLimitedDecal = effect.GetComponent<TimeLimitedDecal>();

			timeLimitedDecal.SetUpDecal(0.5f);
		}

		#region Sprites

		public void ServerSelectRandomSprite()
		{
			switch(artifactData.Type)
			{
				case ArtifactType.Geological:
					currentSprite = ArtifactDataSO.GeologicalSprites.PickRandom();
					break;
				case ArtifactType.Mechanical:
					currentSprite = ArtifactDataSO.MechanicalSprites.PickRandom();
					break;
				case ArtifactType.Organic:
					currentSprite = ArtifactDataSO.OrganicSprites.PickRandom();
					break;
				default:
					currentSprite = ArtifactDataSO.GeologicalSprites.PickRandom();
					break;
			}
			spriteHandler?.SetSpriteSO(currentSprite.idleSprite);
			spriteHandler?.PushTexture();
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

		#endregion

		#region EffectRandomisation

		ArtifactClass PickClass()
		{
			int total = artifactData.radiationlevel / 20 + artifactData.bluespacesig + artifactData.bananiumsig / 2;
			int choice = Random.Range(0, total + 1);

			ArtifactClass artifactClass = ArtifactClass.Uranium;

			if (choice >= (artifactData.radiationlevel / 20) && choice < (artifactData.bluespacesig + artifactData.radiationlevel / 20))
			{
				artifactClass = ArtifactClass.Bluespace;
			}
			else if(choice >= (artifactData.bluespacesig + artifactData.radiationlevel / 20))
			{
				artifactClass = ArtifactClass.Bananium;
			}

			return artifactClass;
		}

		#endregion

		#region MultitoolInteraction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.Artifact;
		public MultitoolConnectionType ConType => conType;

		public bool MultiMaster => true;
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		#endregion
	}

	public enum ArtifactType //This determines the sprites the artifact can use, what tool is used to take samples from it, what samples it gives, and what materials it gives when dismantled.
	{
		Geological = 0,
		Mechanical = 1,
		Organic = 2,
	}

	internal struct ArtifactData //Artifact Data contains all properties of the artifact that will be transferred to samples and/or guessed by the research console, placed in a struct to make data transfer easier.
	{
		public ArtifactData(int radlvl = 0, int bluelvl = 0, int bnalvl = 0, int mss = 0, ArtifactType type = ArtifactType.Geological, int areaEffectValue = 0, int interactEffectValue = 0, int damageEffectValue = 0, string iD = "")
		{
			radiationlevel = radlvl;
			bluespacesig = bluelvl;
			bananiumsig = bnalvl;
			mass = mss;
			Type = type;
			AreaEffectValue = areaEffectValue;
			InteractEffectValue = interactEffectValue;
			DamageEffectValue = damageEffectValue;
			ID = iD;
		}

		public ArtifactType Type;
		public int radiationlevel;
		public int bluespacesig;
		public int bananiumsig;
		public int mass;
		public int AreaEffectValue;
		public int InteractEffectValue;
		public int DamageEffectValue;
		public string ID;
	}

}
