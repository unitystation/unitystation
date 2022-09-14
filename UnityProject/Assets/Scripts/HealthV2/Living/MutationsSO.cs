using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

[CreateAssetMenu(fileName = "_DoNotUse", menuName = "ScriptableObjects/Mutations/_DoNotUse")]
public class MutationSO : ScriptableObject
{
	public int Stability = 0;
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
