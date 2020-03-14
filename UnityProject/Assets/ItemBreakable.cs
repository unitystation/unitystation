using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBreakable : MonoBehaviour
{
	private Integrity integrity;

	public SpriteRenderer spriteRenderer;

	public float damageOnHit;

	public int integrityHealth;

	public Sprite sprite;

	// Start is called before the first frame update
	void Awake()
    {
		integrity = GetComponent<Integrity>();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
	}

	public void AddDamage()
	{
		integrity.ApplyDamage(damageOnHit, AttackType.Melee, DamageType.Brute);
		if(integrity.integrity <= integrityHealth)
		{
			ChangeState();
		}
	}
	private void ChangeState()
	{
		spriteRenderer.sprite = sprite;
		SoundManager.PlayNetworkedAtPos("GlassBreak0#", gameObject.AssumedWorldPosServer());
		GetComponent<ItemAttributesV2>().AddTrait(CommonTraits.Instance.BrokenLightTube);
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		SoundManager.PlayNetworkedAtPos("GlassBreak0#", gameObject.AssumedWorldPosServer());
		Spawn.ServerPrefab("GlassShard", gameObject.AssumedWorldPosServer(), transform.parent, count: 1,
			scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
	}

	// Update is called once per frame
	void Update()
    {
        
    }
}
