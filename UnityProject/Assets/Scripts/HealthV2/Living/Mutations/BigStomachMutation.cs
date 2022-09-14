using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

[CreateAssetMenu(fileName = "BigStomachMutation", menuName = "ScriptableObjects/Mutations/BigStomachMutation")]
public class BigStomachMutation : MutationSO
{

	public override Mutation GetMutation(BodyPart BodyPart)
	{
		return new InBigStomachMutation(BodyPart);
	}


	public class InBigStomachMutation : Mutation
	{
		public InBigStomachMutation(BodyPart BodyPart) : base(BodyPart)
		{

		}

		public override void SetUp()
		{
			var Stomach = BodyPart.GetComponent<Stomach>();
			Stomach.StomachContents.SetMaxCapacity(99); //idk Custom thing, if it's preset custom
		}

		public override void Remove()
		{
			var Stomach = BodyPart.GetComponent<Stomach>();
			Stomach.StomachContents.SetMaxCapacity(2);
		}

	}
}
