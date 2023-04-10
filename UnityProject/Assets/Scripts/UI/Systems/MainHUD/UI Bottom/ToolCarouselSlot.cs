using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolCarouselSlot : MonoBehaviour
{

	public UI_DynamicItemSlot RelatedUI_DynamicItemSlot;
	public ToolCarousel RelatedToolCarousel;

	public GameObject Highlight;

	public void Pressed()
	{
		RelatedToolCarousel.SetActive(RelatedToolCarousel.FilledSlots.IndexOf(this));
	}

}
