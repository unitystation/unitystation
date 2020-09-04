using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyPartMaster : MonoBehaviour
{
	private BodyPartContainerExternal externalLayer;
	private BodyPartContainerStructure structuerLayer;
	private BodyPartContainerInternal internalLayer;

	private void Awake()
	{
		externalLayer = GetComponent<BodyPartContainerExternal>();
		structuerLayer = GetComponent<BodyPartContainerStructure>();
		internalLayer = GetComponent<BodyPartContainerInternal>();
	}
}
