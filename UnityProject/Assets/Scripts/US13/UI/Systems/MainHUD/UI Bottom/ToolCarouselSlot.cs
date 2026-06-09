using UnityEngine;
using US13.Systems.Inventory;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
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
}
