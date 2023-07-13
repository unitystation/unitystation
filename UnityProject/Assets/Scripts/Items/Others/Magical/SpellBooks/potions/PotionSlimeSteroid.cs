using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Items;
using UnityEngine;
using Util;

public class PotionSlimeSteroid : MonoBehaviour, ICheckedInteractable<HandApply>
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.SelfHeal);


	public int MaximumNumberOfCore = 5;


	public virtual bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject) ==false) return false;
		if (interaction.TargetObject.GetComponent<LivingHealthMasterBase>().IsDead) return false;
		return interaction.Intent == Intent.Help;
	}

	public virtual void ServerPerformInteraction(HandApply interaction)
	{
		if (CheckTarget(interaction.TargetObject))
		{
			void ProgressComplete()
			{
				ServerApplyPotion(interaction.TargetObject);
				_ = Despawn.ServerSingle(this.gameObject);
			}

			StandardProgressAction.Create(ProgressConfig, ProgressComplete)
				.ServerStartProgress(interaction.Performer.RegisterTile(), 5f, interaction.Performer); //TODO Think about
		}

	}


	public void ServerApplyPotion(GameObject Target)
	{
		if (CheckTarget(Target))
		{
			var core = Target.GetComponent<LivingHealthMasterBase>().brain.GetComponent<SlimeCore>();

			//Might be worth making this into a function on Spawn.ServerPrefab  ForeverID

			var  NewCore = Spawn.ServerPrefab(CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[core.GetComponent<PrefabTracker>().ForeverID]);
			core.CurrentNumberOfCore++;
			core.RelatedPart.ContainedIn.OrganStorage.ServerTryAdd(NewCore.GameObject);



			foreach (var BodyPart in core.RelatedPart.HealthMaster.BodyPartList)
			{
				var bCore = BodyPart.GetComponent<SlimeCore>();
				if (bCore != null)
				{
					bCore.CurrentNumberOfCore = core.CurrentNumberOfCore;
				}
			}
		}
	}



	public bool CheckTarget(GameObject Target)
	{
		if (Validations.HasComponent<LivingHealthMasterBase>(Target) ==false) return false;
		if (Target.GetComponent<LivingHealthMasterBase>().IsDead) return false;
		var core = Target.GetComponent<LivingHealthMasterBase>().brain.GetComponent<SlimeCore>();
		if (core == null) return false;
		if (core.RelatedPart.ContainedIn == null) return false;

		if (core.CurrentNumberOfCore >= MaximumNumberOfCore) return false;
		return true;

	}



}
