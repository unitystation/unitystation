using Objects;
using UnityEngine;

/// <summary>
/// Tiles are not prefabs, but we still want to be able to associate interaction logic with them.
/// This abstract base scriptable object allows tiles to define their interaction logic by referencing
/// subclasses of this class.
/// </summary>
[CreateAssetMenu(fileName = "LavaStepInteraction", menuName = "Interaction/TileInteraction/LavaStepInteraction")]
public class LavaStepInteraction : TileStepInteraction
{
	[SerializeField]
	private float fireStacks = 10;

	[SerializeField]
	private float objectFireDamage = 5;

	//Player enter tile interaction//
	public override bool CanPlayerStep(PlayerScript playerScript)
	{
		return true;
	}

	public override void OnPlayerStep(PlayerScript playerScript)
	{
		playerScript.playerHealth.ChangeFireStacks(fireStacks);
	}

	//Object, mob, item enter tile interaction//
	public override bool CanObjectEnter(GameObject eventData)
	{
		return true;
	}

	public override void OnObjectEnter(GameObject eventData)
	{
		if (eventData.TryGetComponent<LivingHealthBehaviour>(out var mobHealth))
		{
			mobHealth.ChangeFireStacks(fireStacks);
			return;
		}

		if (eventData.TryGetComponent<Integrity>(out var integrity))
		{
			integrity.ApplyDamage(objectFireDamage, AttackType.Fire, DamageType.Burn);
		}
	}
}