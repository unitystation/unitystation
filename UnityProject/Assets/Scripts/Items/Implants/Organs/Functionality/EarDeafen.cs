using Items.Implants.Organs;
using Mirror;
using Player;
using UnityEngine; 

public class EarDeafen : NetworkBehaviour
{
	public ItemTrait DeafenProtection;

	[SerializeField] private float deafenMultiplier = 1;
	[SerializeField] private Ears connectedEars = null;

	public bool TryDeafen(float deafenDuration, bool checkForProtectiveCloth = true)
	{
		if (connectedEars.RelatedPart.ItemAttributes.HasTrait(DeafenProtection))
		{
			return false;
		}

		if (checkForProtectiveCloth)
		{
			if (HasProtectiveCloth())
			{
				return false;
			}
		}

		connectedEars.RelatedPart.TakeDamage(null, deafenDuration * 0.5f, AttackType.Internal, DamageType.Burn);
		PlayerDeafenEffectsMessage.Send(connectedEars.RelatedPart.HealthMaster.gameObject, deafenDuration * deafenMultiplier, connectedEars.gameObject);

		return true;
	}

	public bool HasProtectiveCloth()
	{
		if (connectedEars.RelatedPart.HealthMaster.TryGetComponent<DynamicItemStorage>(out var playerStorage) == false) return false;

		foreach (var slots in playerStorage.ServerContents)
		{
			//TODO Might be better for a script where you ask it if it's blocking Flash but this is good enough for now
			if (slots.Key != NamedSlot.ear && slots.Key != NamedSlot.head) continue;
			foreach (ItemSlot onSlots in slots.Value)
			{
				if (onSlots.IsEmpty) continue;
				if (onSlots.ItemAttributes.HasTrait(DeafenProtection))
				{
					return true;
				}
			}
		}
		return false;
	}
}
