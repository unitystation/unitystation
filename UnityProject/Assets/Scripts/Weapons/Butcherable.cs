using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Butcherable : MonoBehaviour, ICheckedInteractable<HandApply>
{
    [SerializeField]
    private ItemTrait knifeTrait;

    [SerializeField]
    private static readonly StandardProgressActionConfig ProgressConfig
    = new StandardProgressActionConfig(StandardProgressActionType.Restrain);

    [SerializeField]
    private float butcherTime = 2.0f;

    [SerializeField]
    private string sound = "BladeSlice";

    public bool WillInteract(HandApply interaction, NetworkSide side)
    {
        if (!DefaultWillInteract.Default(interaction, side)) return false;
        if (!Validations.HasItemTrait(interaction.HandObject, knifeTrait)) return false;

        var healthComponent = interaction.TargetObject.GetComponent<LivingHealthBehaviour>();
        if (!healthComponent || !healthComponent.allowKnifeHarvest || !healthComponent.IsDead) return false;

        return true; 

    }

    public void ServerPerformInteraction(HandApply interaction)
    {

        GameObject victim = interaction.TargetObject;
        GameObject performer = interaction.Performer;

        void ProgressFinishAction()
        {
            LivingHealthBehaviour victimHealth = victim.GetComponent<LivingHealthBehaviour>();
            //playerMove.allowInput = false;
            victimHealth.Harvest();
            SoundManager.PlayNetworkedAtPos("BladeSlice", victim.RegisterTile().WorldPositionServer);
        }

        var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
            .ServerStartProgress(victim.RegisterTile(), butcherTime, performer);
    }
}
