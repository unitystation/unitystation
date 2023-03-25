using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUIHandAreasSelectable
{

	public void DeSelect(NamedSlot Hand);

	public void SwapHand();

	public UI_DynamicItemSlot GetHand(NamedSlot Hand);
}
