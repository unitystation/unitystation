using System.Collections;
using System.Collections.Generic;
using Objects.Medical;
using UnityEngine;

public class DNAConsole : MonoBehaviour
{
	public DNAScanner DNAScanner;

	public char SelectionChar;
	public BodyPartSelectionType SelectionType;

	public enum BodyPartSelectionType
	{
		MustInclude,
		MustNotContain
	}


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
						Logger.LogError($"Found {BP.name}");
					}
					break;
				case BodyPartSelectionType.MustInclude:
					if (BP.name.Contains(SelectionChar))
					{
						Logger.LogError($"Found {BP.name}");
					}
					break;

			}

		}




	}

}
