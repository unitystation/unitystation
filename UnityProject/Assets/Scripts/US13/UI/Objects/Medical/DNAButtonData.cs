using UnityEngine;

namespace US13.UI.Objects.Medical
{
	public class DNAButtonData : MonoBehaviour
	{

		public DNASpeciesElement RelatedDNASpeciesElement;
		public string BodyPartName;


		public void OnPress()
		{
			RelatedDNASpeciesElement.netClientSyncString.SetValue(BodyPartName);
			RelatedDNASpeciesElement.CloseSection();
		}
	}
}
