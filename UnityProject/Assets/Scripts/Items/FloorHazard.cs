using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Items;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public class FloorHazard : NetworkBehaviour {
    public DamageType damageType;
    public AttackType attackType;
    public float damage;
    public bool ignoresShoes = false;
    public bool ignoresWalking = false;
    public bool requiresArming = false;
    public bool armed = false;
    public float chancePercent = 100f;
    private BodyPartType[] legs = {BodyPartType.RightLeg,BodyPartType.LeftLeg};

    public void ArmHazard(){
        armed = true;
    }

    public void DisarmHazard(){
        armed = false;
    }

    public void OnEnterableEnter(BaseEventData eventData)
	{
        if(!requiresArming || armed){
            if(eventData.selectedObject.TryGetComponent(out PlayerHealth playerHealth) && eventData.selectedObject.TryGetComponent(out PlayerScript playerScript)){
                if(playerScript.GetComponent<ItemStorage>().GetNamedItemSlot(NamedSlot.feet).Item == null || ignoresShoes){
                    if(!UIManager.Intent.Running && !ignoresWalking){
                        Chat.AddExamineMsgFromServer(playerScript.gameObject, "You tread gently on the " + GetComponent<ItemAttributesV2>().ArticleName);
                        return;
                    }
                    if(Random.Range(0.0f,100.0f) > chancePercent){
                        Chat.AddExamineMsgFromServer(playerScript.gameObject, "You almost injure your feet on the " + GetComponent<ItemAttributesV2>().ArticleName);
                        return;
                    }
                    playerHealth.ApplyDamageToBodypart(gameObject,damage,attackType,damageType,legs.PickRandom());
                    if(requiresArming && armed) {
                        Chat.AddExamineMsgFromServer(playerScript.gameObject, "You trigger the " + GetComponent<ItemAttributesV2>().ArticleName);
                    }
                    Chat.AddExamineMsgFromServer(playerScript.gameObject, "<color=red>You injure your feet on the " + GetComponent<ItemAttributesV2>().ArticleName + "</color>");
                } else{
                    DisarmHazard();
                    Chat.AddExamineMsgFromServer(playerScript.gameObject, "You trigger the " + GetComponent<ItemAttributesV2>().ArticleName);
                }
            }
        }
	}
}