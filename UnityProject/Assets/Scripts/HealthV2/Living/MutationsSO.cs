using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

[CreateAssetMenu(fileName = "_DoNotUse", menuName = "ScriptableObjects/Mutations/_DoNotUse")]
public class MutationSO : ScriptableObject
{
	[SerializeField]
	private string displayName = "";

	public string DisplayName
	{
		get
		{
			if (string.IsNullOrEmpty(displayName) == false)
			{
				return displayName;
			}
			else
			{
				return name;
			}
		}
	}

	[Tooltip(" Effects the type of dinosaur that spawned when An egg is generated, Hire equals more aggressive and dangerous Dinosaurs")]
	[Range(0, 100)] public int ResearchDifficult;

	[Tooltip("The stability says if this is a negative or positive mutation in terms of balancing, E.G x-ray will give - stability, while a negative mutation for example blindness will give positive stability, " +
	         "this balances out the game preventing you from having to many overpowered mutations, because you need to have a few mutations that are disadvantages")]
	public int Stability = 0;


	[Tooltip(" Description of the Mutation ")]
	public string Description;

	[Tooltip("for the Slider mini game puzzle old implementation, makes it so the slide puzzle is not necessarily solvable without using Locks")]
	public bool CanRequireLocks = false;




	public virtual Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
	{
		return new Mutation(BodyPart,_RelatedMutationSO);
	}
}



public class Mutation
{
	public MutationSO RelatedMutationSO;

	public int Stability = 0;

	public BodyPart BodyPart;

	public Mutation(BodyPart _BodyPart,MutationSO _RelatedMutationSO)
	{
		BodyPart = _BodyPart;
		RelatedMutationSO = _RelatedMutationSO;
	}

	public virtual void SetUp()
	{


	}

	public virtual void Remove()
	{
	}
}
