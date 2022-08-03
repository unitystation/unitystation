using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Science;
using Systems.Research;
using ScriptableObjects.Systems.Research;
using Systems.Radiation;
using Systems.ObjectConnection;
using Mirror;
using Systems.Atmospherics;
using ScriptableObjects.Atmospherics;
using CustomInspectors;

[System.Serializable]
public class ArtifactSprite
{
	public SpriteDataSO idleSprite;
	public SpriteDataSO activeSprite;
}

namespace Objects.Research
{
	public enum ArtifactType //This determines the sprites the artifact can use, what tool is used to take samples from it and what materials it gives when dismantled.
	{
		Geological = 0,
		Mechanical = 1,
		Organic = 2,
	}

	public class Artifact : ImnterfaceMultitoolGUI, IServerSpawn, IServerDespawn, ICheckedInteractable<HandApply>, IMultitoolMasterable
	{
		/// <summary>
		/// Set of all artifacts on scenes. Useful to get list of all existing artifacts.
		/// </summary>
		public readonly static HashSet<Artifact> ServerSpawnedArtifacts = new HashSet<Artifact>();

		private Integrity integrity;
		private UniversalObjectPhysics objectPhysics; 

		[SerializeField]
		private GameObject SliverPrefab = null;

		[SerializeField]
		private SpriteHandler spriteHandler = null;

		private RadiationProducer radiationProducer = null;

		private ArtifactSprite currentSprite;

		public float TouchEffectTimeout = 10f;
		private float lastActivationTime;

		public bool isDormant = true;
		public ItemTrait DormantTrigger;

		int samplesTaken = 0;

		private Coroutine animationCoroutine = null;

		[SyncVar] public ArtifactData artifactData = new ArtifactData();

		public ArtifactDataSO ArtifactDataSO;

		public AreaArtifactEffect AreaEffect;
		public InteractArtifactEffect InteractEffect;
		public ArtifactEffect DamageEffect;


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
			integrity = GetComponent<Integrity>();
			radiationProducer = GetComponent<RadiationProducer>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();

			integrity.OnApplyDamage.AddListener(DoDamageEffect);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			//Sets appearance
			artifactData.Type = (ArtifactType)Random.Range(0, 3);

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
						artifactData.radiationlevel += Random.Range(100, 500);
						break;
				}
			}

			//Randomises ID and adds it to name, this is done for two reasons:
			//To prevent duplication exploits, server knows if youve researched an artifact before and won't give you extra RP or credits for repeated research.
			//So players know which artifact is which if they have the same sprites
			//ID is in form: A000 - Z999
			int num = Random.Range(0, 26);
			ID = $"{(char)('a' + num)}{Random.Range(0, 1000).ToString("000")}"; 
			ID = ID.ToUpper();

			GetComponent<ObjectAttributes>().ServerSetArticleName("Artifact - " + ID);

			//Randomises the effects of the artifacts, probabilities of certain effects change with composition
			ArtifactClass chosenClass = PickClass();
			AreaEffect = ArtifactDataSO.AreaEffects[(int)chosenClass].AreaArtifactEffectList.PickRandom();

			chosenClass = PickClass();
			InteractEffect = ArtifactDataSO.InteractEffects[(int)chosenClass].InteractArtifactEffectList.PickRandom();

			chosenClass = PickClass();
			DamageEffect = ArtifactDataSO.DamageEffect[(int)chosenClass].DamageArtifactEffectList.PickRandom();

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
				radiationProducer.enabled = true;
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
		}

		#endregion

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
				Chat.AddActionMsgToChat(this.gameObject, "Placeholder", $"{gameObject.ExpensiveName()} begins to humm quietly");
			}

			TryActivateByTouch(interaction);
			
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
		private void TakeSample(HandApply interaction)
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 5f,
				$"You begin extracting a sample from {gameObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} begins extracting a sample from {gameObject.ExpensiveName()}...",
				$"You extract a sample from {gameObject.ExpensiveName()}.",
				$"{interaction.Performer.ExpensiveName()} extracts a sample from {gameObject.ExpensiveName()}.",
				() =>
				{
					GameObject sliver = Spawn.ServerPrefab(SliverPrefab, gameObject.AssumedWorldPosServer()).GameObject;
					if(sliver.TryGetComponent<ArtifactSliver>(out var sliverComponent)) sliverComponent.SetUpValues(artifactData,ID + $":{(char)('a' + samplesTaken)}");

					DoDamageEffect();
				});
		}

		void DoDamageEffect(DamageInfo damageInfo = null)
		{
			if(isDormant)
			{
				if(DMMath.Prob(50))
				{
					Chat.AddActionMsgToChat(this.gameObject, "Placeholder", "Wake up damage message");
					ToggleDormancy(false);
				}
				else
				{
					Chat.AddActionMsgToChat(this.gameObject, "Placeholder", "The anomaly quivers and seems to crack a little");
				}
			}
			if (isDormant == false)
			{
				DamageEffect.DoEffect();
			}
		}

		public void TryActivateByTouch(HandApply interaction)
		{
			if(isDormant)
			{
				if (DMMath.Prob(10))
				{
					Chat.AddActionMsgToChat(interaction.Performer, "Message for waking up artifact",
						$"{interaction.Performer.ExpensiveName()} Message for waking up artifact");
					ToggleDormancy(false);
				}
				else
				{
					Chat.AddActionMsgToChat(interaction.Performer, "You touch the anomaly, it twitches slightly, but remains dormant...",
						$"{interaction.Performer.ExpensiveName()} touches the anomaly, it twitches slighty, but remains dormant...");
				}
			}
			else if(!UnderTimeout)
			{
				InteractEffect.DoEffectTouch(interaction);
				PlayActivationAnimation();
				lastActivationTime = Time.time;
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
			moles -= 10;
			if (moles > 0 && DMMath.Prob(Mathf.Clamp(moles, 0, 100)))
			{
				ToggleDormancy(true);
				Chat.AddActionMsgToChat(this.gameObject, "", "The anomaly falls dormant...");
			}
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

			ArtifactClass artifactClass;

			if (choice < (artifactData.radiationlevel / 2))
			{
				artifactClass = ArtifactClass.Uranium;
			}
			if (choice >= (artifactData.radiationlevel / 20) && choice < (artifactData.bluespacesig + artifactData.radiationlevel / 20))
			{
				artifactClass = ArtifactClass.Bluespace;
			}
			else
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
	
	public struct ArtifactData //Artifact Data contains all properties of the artifact that will be transferred to samples and/or guessed by the research console, placed in a struct to make data transfer easier.
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
