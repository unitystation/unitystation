using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;

public class ItemBreakable : MonoBehaviour
{
	private Integrity integrity;

	public float damageOnHit;

	public int integrityHealth;

	public GameObject brokenItem;

	[SerializeField] private AddressableAudioSource SoundOnBreak = null;

	// Start is called before the first frame update
	void Awake()
	{
		integrity = GetComponent<Integrity>();

		integrity.OnApplyDamage.AddListener(OnDamageReceived);
	}

	public void AddDamage()
	{
		integrity.ApplyDamage(damageOnHit, AttackType.Melee, DamageType.Brute);
		if (integrity.integrity <= integrityHealth)
		{
			ChangeState();
		}
	}
	private void ChangeState()
	{
		SoundManager.PlayNetworkedAtPos(SoundOnBreak, gameObject.AssumedWorldPosServer(), sourceObj: gameObject);
		Spawn.ServerPrefab(brokenItem, gameObject.AssumedWorldPosServer());
		Despawn.ServerSingle(gameObject);
	}

	private void OnDamageReceived(DamageInfo arg0)
	{
		if (integrity.integrity <= integrityHealth)
		{
			ChangeState();
		}
	}
}