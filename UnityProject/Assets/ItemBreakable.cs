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
		GetComponent<ItemAttributesV2>().AddTrait(CommonTraits.Instance.BrokenLightTube);
	}
    // Update is called once per frame
    void Update()
    {
        
    }
}
