using UnityEngine;

//Do not derive from NetworkBehaviour, this is also used on tilemap layers
/// <summary>
/// Allows an object to be attacked by melee. Not used anymore for meleeing tiles (now done in InteractableTiles)
/// TODO: Refactor to use IF2 rather than PNA
/// </summary>
public class Meleeable : MonoBehaviour, IClientInteractable<PositionalHandApply>
{
	//Cache these on start for checking at runtime
	private Layer tileMapLayer;
	private GameObject gameObjectRoot;

	private void Start()
	{
		gameObjectRoot = transform.root.gameObject;

		var layer = gameObject.GetComponent<Layer>();
		if (layer != null)
		{
			//this is on a tilemap:
			tileMapLayer = layer;
		}
	}

	public bool Interact(PositionalHandApply interaction)
	{

		var localRegisterPlayer = PlayerManager.LocalPlayer.GetComponent<RegisterPlayer>();
		var localPlayerhealth = PlayerManager.LocalPlayer.GetComponent<PlayerHealth>();

		// Only melee while conscious, and not while down or stunned.
		if (localPlayerhealth.ConsciousState != ConsciousState.CONSCIOUS ||
		    localRegisterPlayer.IsDown)
			return false;

		//meleeable is only checked on the target of a melee interaction
		if (interaction.UsedObject == gameObject) return false;

		if (interaction.HandObject != null)
		{
			var handItem = interaction.HandObject;

			//special case
			//We don't melee if we are wielding a gun with ammo and clicking ourselves (we will instead shoot ourselves)
			if (interaction.TargetObject == interaction.Performer)
			{
				var gun = handItem.GetComponent<Gun>();
				if (gun != null)
				{
					if (gun.CurrentMagazine?.ClientAmmoRemains > 0)
					{
						//we have ammo and are clicking ourselves - don't melee. Shoot instead.
						return false;
					}
				}
			}

			// If they are not in attack range they should not attack.
			if (!PlayerManager.LocalPlayerScript.IsInReach(interaction.WorldPositionTarget, false))
				return false;

			// Direction of attack towards the attack target.
			Vector2 dir = ((Vector3)interaction.WorldPositionTarget - localRegisterPlayer.WorldPosition)
				.normalized;

			var lps = PlayerManager.LocalPlayerScript;

			if (tileMapLayer == null)
			{
				lps.weaponNetworkActions.CmdRequestMeleeAttackSlot(gameObject,
					UIManager.Hands.CurrentSlot.NamedSlot, dir, UIManager.DamageZone, LayerType.None);
			}
			else
			{
				lps.weaponNetworkActions.CmdRequestMeleeAttackSlot(gameObjectRoot,
					UIManager.Hands.CurrentSlot.NamedSlot, dir, UIManager.DamageZone, tileMapLayer.LayerType);
			}

			return true;
		}
		// If the performer has an empty hand and harm intent request a punch.
		else if (UIManager.CurrentIntent == Intent.Harm)
		{
			var lps = PlayerManager.LocalPlayerScript;
			// Direction of attack towards the attack target.
			Vector2 dir = ((Vector3)interaction.WorldPositionTarget - localRegisterPlayer.WorldPosition)
				.normalized;

			lps.weaponNetworkActions.CmdRequestPunchAttack(gameObject, dir, UIManager.DamageZone);
			return true;
		}

		return false;
	}
}