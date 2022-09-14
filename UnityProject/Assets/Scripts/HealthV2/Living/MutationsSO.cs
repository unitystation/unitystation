using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

[CreateAssetMenu(fileName = "_DoNotUse", menuName = "ScriptableObjects/Mutations/_DoNotUse")]
public class MutationSO : ScriptableObject
{
	public int Stability = 0;
	public virtual Mutation GetMutation(BodyPart BodyPart)
	{
		return new Mutation(BodyPart);
	}
}



public class Mutation
{
	public int Stability = 0;

	public BodyPart BodyPart;

	public Mutation(BodyPart _BodyPart)
	{
		BodyPart = _BodyPart;
	}

	public virtual void SetUp()
	{


	}

	public virtual void Remove()
	{
	}
}
