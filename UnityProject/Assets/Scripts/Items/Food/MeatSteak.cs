using PlayGroup;
using UI;
using UnityEngine.Networking;

public class MeatSteak : BaseFood
{
    HealthBehaviour healthScript;
    PlayerHealth playerDetails;
    private PlayerNetworkActions playerNetworkActions;

    public override void EatFood()
    {

        var currentSlot = UIManager.Hands.CurrentSlot;

        currentSlot.Clear();

        NetworkServer.Destroy(this.gameObject);
        PlayerManager.LocalPlayerScript.playerNetworkActions.GetComponent<PlayerHealth>().StopBleeding();
        PlayerManager.LocalPlayerScript.playerNetworkActions.GetComponent<PlayerHealth>().AddHealth(50);
        PlayerManager.LocalPlayerScript.soundNetworkActions.CmdPlaySoundAtPlayerPos("EatFood");


    }
}
