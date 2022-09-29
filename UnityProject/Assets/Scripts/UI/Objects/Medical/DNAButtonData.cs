using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DNAButtonData : MonoBehaviour
{

	public DNASpeciesElement RelatedDNASpeciesElement;
	public string BodyPartName;


	public void OnPress()
	{
		RelatedDNASpeciesElement.netClientSyncString.SetValue(BodyPartName);
	}
}
