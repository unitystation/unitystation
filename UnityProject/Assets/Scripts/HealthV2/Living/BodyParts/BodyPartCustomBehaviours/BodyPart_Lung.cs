using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BodyParts
{
    public class BodyPart_Lung : HealthV2.BodyPart
{
    [SerializeField] private float coughChanceWhenInternallyBleeding = 0.5f;
    [SerializeField] private float internalBleedingCooldown = 4f;
    private bool onCooldown = false;

	public override void InternalBleedingLogic()
	{
        if(!onCooldown)
        {
            base.InternalBleedingLogic();
            float bloodCoughChance = Random.Range(0,1.0f);
            if(bloodCoughChance >= coughChanceWhenInternallyBleeding)
            {
                CurrentInternalBleedingDamage -= Random.Range(MinMaxInternalBleedingValues.x, MinMaxInternalBleedingValues.y);
                GameObject script = healthMaster.gameObject.Player().GameObject;
                Chat.AddActionMsgToChat(script, "You cough up blood!", $"{script.Player().Script.visibleName} cough up blood!");
                EffectsFactory.BloodSplat(script.gameObject.RegisterTile().WorldPositionServer, BloodSplatSize.small, BloodSplatType.red);
                onCooldown = true;
                StartCoroutine(CooldownTick());
            }
        }
	}

    private IEnumerator CooldownTick()
    {
        yield return new WaitForSeconds(internalBleedingCooldown);
        onCooldown = false;
    }
}
}

