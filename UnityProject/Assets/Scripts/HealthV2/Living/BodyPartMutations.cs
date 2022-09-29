using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class BodyPartMutations : BodyPartFunctionality
{
	public static Dictionary<MutationSO, NumberAndRoundID> MutationVariants = new Dictionary<MutationSO, NumberAndRoundID>();

	public List<MutationSO> CapableMutations = new List<MutationSO>();
	public List<Mutation> ActiveMutations = new List<Mutation>();

	public int Stability = 0;

	public int SecondsForSpeciesMutation = 60;

	public void MutateCustomisation(string InCustomisationTarget, string CustomisationReplaceWith)
	{
		if (bodyPart.SetCustomisationData.Contains(InCustomisationTarget))
		{
			//Logger.LogError($"{bodyPart.name} has {InCustomisationTarget} in SetCustomisationData");
			var newone = bodyPart.SetCustomisationData.Replace(InCustomisationTarget, CustomisationReplaceWith);
			//Logger.LogError($"Changing from {bodyPart.SetCustomisationData} to {newone} ");
			bodyPart.LobbyCustomisation.OnPlayerBodyDeserialise(bodyPart, newone, bodyPart.HealthMaster);
		}
	}




	[NaughtyAttributes.Button()]
	public void AddFirstMutation()
	{
		var  Mutation = CapableMutations[0];
		AddMutation(Mutation);
	}

	public void AddMutation(MutationSO Mutation)
	{
		if (CapableMutations.Contains(Mutation) == false) return; //TODO Maybe add negative mutation instead

		if (MutationVariants.ContainsKey(Mutation) == false)
		{
			MutationVariants[Mutation] = new NumberAndRoundID()
			{
				RoundID = GameManager.Instance.RoundID,
				Stability = Mathf.RoundToInt((Mutation.Stability * Random.Range(0.5f, 1.5f)))

			};
		}
		else
		{
			if (MutationVariants[Mutation].RoundID != GameManager.Instance.RoundID)
			{
				MutationVariants[Mutation].Stability = Mathf.RoundToInt((Mutation.Stability * Random.Range(0.5f, 1.5f)));
				MutationVariants[Mutation].RoundID = GameManager.Instance.RoundID;
			}
		}


		var ActiveMutation = Mutation.GetMutation(bodyPart,Mutation);
		ActiveMutation.Stability = MutationVariants[Mutation].Stability;


		ActiveMutations.Add(ActiveMutation);
		ActiveMutation.SetUp();
		CalculateStability();

		bodyPart.HealthMaster.OrNull()?.BodyPartsChangeMutation();
	}

	public void RemoveMutation()
	{
		Mutation Mutation = ActiveMutations[0];
		ActiveMutations.Remove(Mutation);
		Mutation.Remove();
		CalculateStability();
		bodyPart.HealthMaster.OrNull()?.BodyPartsChangeMutation();
	}

	public List<MutationAndBodyPart> GetAvailableNegativeMutations(List<MutationAndBodyPart> AvailableMutations)
	{
		foreach (var Mutation in CapableMutations)
		{
			if (Mutation.Stability > 0)
			{
				bool AlreadyActive = false;
				foreach (var ActiveMutation in ActiveMutations)
				{
					if (ActiveMutation.RelatedMutationSO == Mutation)
					{
						AlreadyActive = true;
						break;
					}
				}

				if (AlreadyActive == false)
				{
					AvailableMutations.Add(new MutationAndBodyPart(){BodyPartMutations = this, MutationSO = Mutation});
				}
			}
		}

		return AvailableMutations;
	}

	public struct MutationAndBodyPart
	{
		public MutationSO MutationSO;
		public BodyPartMutations BodyPartMutations;

	}



	public void CalculateStability()
	{
		int InStability = 0;
		foreach (var ActiveMutation in ActiveMutations)
		{
			InStability += ActiveMutation.Stability;
		}

		Stability = InStability;
	}

	private IEnumerator ProcessChangeToSpecies(PlayerHealthData NewSpecies, GameObject BodyPart)
	{
		yield return WaitFor.Seconds((SecondsForSpeciesMutation / 2f)  * (1 + UnityEngine.Random.Range(-0.25f, 0.25f)));

		Chat.AddExamineMsgFromServer(RelatedPart.OrNull()?.HealthMaster.gameObject, $" Your {RelatedPart.gameObject.ExpensiveName()} Feels strange");

		yield return WaitFor.Seconds((SecondsForSpeciesMutation / 2f) * (1 + UnityEngine.Random.Range(-0.25f, 0.25f)));

		var SpawnedBodypart = Spawn.ServerPrefab(BodyPart).GameObject.GetComponent<BodyPart>();

		Chat.AddExamineMsgFromServer(RelatedPart.OrNull()?.HealthMaster.gameObject, $" Your {RelatedPart.gameObject.ExpensiveName()} Morphs into a {SpawnedBodypart.gameObject.ExpensiveName()}");

		foreach (var itemSlot in SpawnedBodypart.OrganStorage.GetItemSlots())
		{
			Inventory.ServerDespawn(itemSlot);
		}

		foreach (var itemSlot in RelatedPart.OrganStorage.GetItemSlots())
		{
			if (itemSlot.Item != null)
			{
				var toSlot = SpawnedBodypart.OrganStorage;
				Inventory.ServerTransfer(itemSlot, toSlot.GetBestSlotFor(itemSlot.Item));
			}
		}


		var ContainedIn = RelatedPart.HealthMaster;
		var Region = RelatedPart.BodyPartType;

		var Parent = RelatedPart.ContainedIn;


		RelatedPart.TryRemoveFromBody( CausesBleed: false, Destroy: true, PreventGibb_Death: true );
//dropping UI slots??
//Relink fat / stomach??


		if (Parent != null)
		{
			Inventory.ServerAdd(SpawnedBodypart.gameObject, Parent.OrganStorage.GetBestSlotFor(SpawnedBodypart.gameObject));
		}
		else
		{
			Inventory.ServerAdd(SpawnedBodypart.gameObject, ContainedIn.BodyPartStorage.GetBestSlotFor(SpawnedBodypart.gameObject));
		}


		var ONMutation = SpawnedBodypart.gameObject.GetComponent<BodyPartMutations>();

		if (ONMutation != null)
		{
			foreach (var Mutations in ActiveMutations)
			{
				ONMutation.AddMutation(Mutations.RelatedMutationSO);
			}
			ONMutation.MutateCustomisation(ONMutation.RelatedPart.SetCustomisationData, RelatedPart.SetCustomisationData);
		}
	}

	public PlayerHealthData PlayerHealthData;
	public GameObject TOMutateBodyPart;

	[NaughtyAttributes.Button()]
	public void MutateBodyPart()
	{
		ChangeToSpecies(PlayerHealthData, TOMutateBodyPart);

	}


	public void ChangeToSpecies(PlayerHealthData PlayerHealthData, GameObject BodyPart)
	{
		StartCoroutine(ProcessChangeToSpecies(PlayerHealthData , BodyPart));
	}

	public class NumberAndRoundID
	{
		public int Stability;
		public int RoundID;
	}
	/*
	so, body wide system

		Plus and minus points from positive negative,

	If it is negative gives you random negative trays until it's positive ,


	Gameplay loop will be that with multi- slider balance thing,


	Body part functionality???

	Let's say, let's say there was a stomach  script,

	How would you affect the size of stomach from

		Body part functionality??

	Without making a custom one for it

		Body part functionality, -> list possible mutations idk what type?
	-> Instantiates new, give some variables is SO,  does this stuff of it needs to be permanent
*/
}
