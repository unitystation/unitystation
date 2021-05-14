using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDynamicItemSlotS
{
	GameObject GameObject { get; }
	ItemStorage RelatedStorage { get; }
	List<BodyPartUISlots.StorageCharacteristics> Storage { get; }
}
