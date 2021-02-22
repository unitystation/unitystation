using System.Collections;
using System.Collections.Generic;
using Systems.Mob;
using Systems.MobAIs;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
using Items;
using Random = UnityEngine.Random;

public class MouseTrap : NetworkBehaviour, IInteractable<HandActivate>
{

    [SerializeField]
    private FloorHazard floorHazard = null;
    public bool armed;
    public float chanceFailPercent = 1f;
    public void ServerPerformInteraction(HandActivate interaction){
        if(interaction.Performer.TryGetComponent(out PlayerHealth playerHealth)){
            interaction.Performer.TryGetComponent(out PlayerScript playerScript);
            if(Random.Range(0f,100f) < chanceFailPercent){
                playerHealth.ApplyDamageToBodypart(interaction.Performer,5,AttackType.Melee,DamageType.Brute,BodyPartType.LeftHand); //TODO find a non-hacky way to damage the active hand, instead of just the left
                Chat.AddExamineMsgFromServer(playerScript.gameObject, "<color=red>Your hand slips and the " + GetComponent<ItemAttributesV2>().ArticleName + " snaps closed on your fingers!</color>");
                armed = false;
            } else {
                if(armed){
                    armed = false;
                    Chat.AddExamineMsgFromServer(playerScript.gameObject, "You gently disarm the " + GetComponent<ItemAttributesV2>().ArticleName);
                } else {
                    armed = true;
                    Chat.AddExamineMsgFromServer(playerScript.gameObject, "You arm the " + GetComponent<ItemAttributesV2>().ArticleName);
                }
            }
            if(floorHazard != null){
                if(armed){
                    floorHazard.ArmHazard();
                } else {
                    floorHazard.DisarmHazard();
                }
            }
        }
    }

    public void OnEnterableEnter(BaseEventData eventData)
	{
        if(armed == true){
            if(eventData.selectedObject.TryGetComponent(out MouseAI _) && eventData.selectedObject.TryGetComponent(out SimpleAnimal mouse)){
                mouse.ApplyDamage(gameObject,mouse.maxHealth,AttackType.Melee,DamageType.Brute);
            } else if (eventData.selectedObject.TryGetComponent(out PlayerScript _)){
                floorHazard.OnEnterableEnter(eventData);
            } else return;
            armed = false;
        }
    }

    public void TriggerInventoryTrap(BaseEventData eventData){

    }

}
