using Items.Implants.Organs;
using UnityEngine;

namespace HealthV2.Living.Mutations
{
	[CreateAssetMenu(fileName = "BigStomachMutation", menuName = "ScriptableObjects/Mutations/BigStomachMutation")]
	public class BigStomachMutation : MutationSO
	{

		public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
		{
			return new InBigStomachMutation(BodyPart,_RelatedMutationSO);
		}

		public class InBigStomachMutation : Mutation
		{
			public InBigStomachMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO) : base(BodyPart,_RelatedMutationSO)
			{

			}

			public override void SetUp()
			{
				var Stomach = BodyPart.GetComponent<Stomach>();
				Stomach.StomachContents.SetMaxCapacity(99); //TODO better implementation //idk Custom thing, if it's preset custom
			}

			public override void Remove()
			{
				var Stomach = BodyPart.GetComponent<Stomach>();
				Stomach.StomachContents.SetMaxCapacity(2); //TODO better implementation
			}

		}
	}
}
