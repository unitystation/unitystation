using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Objects.Medical;
using UnityEngine;

public class DNAConsole : MonoBehaviour
{
	public DNAScanner DNAScanner;

	public char SelectionChar = 'h';
	public BodyPartSelectionType SelectionType;

	public string CustomisationTarget;
	public string CustomisationReplaceWith;

	public enum BodyPartSelectionType
	{
		MustInclude,
		MustNotContain
	}


	public void ModifyCustomisation(BodyPart bodyPart, string InCustomisationTarget, string InCustomisationReplaceWith  )
	{
		if (bodyPart.SetCustomisationData.Contains(InCustomisationTarget))
		{
			Logger.LogError($"{bodyPart.name} has {InCustomisationTarget} in SetCustomisationData");
			var newone = bodyPart.SetCustomisationData.Replace(InCustomisationTarget, InCustomisationReplaceWith);
			Logger.LogError($"Changing from {bodyPart.SetCustomisationData} to {newone} ");
			bodyPart.LobbyCustomisation.OnPlayerBodyDeserialise(bodyPart, newone, bodyPart.HealthMaster);
		}

	}


	[RightClickMethod()]
	[NaughtyAttributes.Button()]
	public void Inject()
	{
		foreach (var BP in DNAScanner.occupant.BodyPartList)
		{
			switch (SelectionType)
			{
				case BodyPartSelectionType.MustNotContain:
					if (BP.name.Contains(SelectionChar) == false)
					{
						ModifyCustomisation(BP, CustomisationTarget, CustomisationReplaceWith);
					}
					break;
				case BodyPartSelectionType.MustInclude:
					if (BP.name.Contains(SelectionChar))
					{
						ModifyCustomisation(BP, CustomisationTarget, CustomisationReplaceWith);
					}
					break;
			}



		}




	}

}
