using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHasUISlot
{
	ItemStorage ItemStorage { get; set; }
	NamedSlot SlotName { get; set; }
}
