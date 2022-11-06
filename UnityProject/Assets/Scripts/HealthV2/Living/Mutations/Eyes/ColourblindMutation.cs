using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

[CreateAssetMenu(fileName = "Colourblindness", menuName = "ScriptableObjects/Mutations/Colourblindness")]
public class ColourblindMutation : MutationSO
{


	public ColourBlindMode ColourBlindMode;
	public override Mutation GetMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO)
	{
		return new InColourblindMutation(BodyPart,_RelatedMutationSO, ColourBlindMode);
	}

	private class InColourblindMutation : Mutation
	{

		public Eye RelatedEye;
		public ColourBlindMode Mode;

		public InColourblindMutation(BodyPart BodyPart,MutationSO _RelatedMutationSO,  ColourBlindMode inMode) : base(BodyPart,_RelatedMutationSO)
		{
			Mode = inMode;
		}

		public override void SetUp()
		{
			RelatedEye = BodyPart.GetComponent<Eye>();
			RelatedEye.ColourBlindMode = RelatedEye.ColourBlindMode | Mode;
			RelatedEye.UpdateColourblindValues();
		}

		public override void Remove()
		{
			RelatedEye.ColourBlindMode = RelatedEye.ColourBlindMode & ~Mode;
			RelatedEye.UpdateColourblindValues();
		}

	}
}
