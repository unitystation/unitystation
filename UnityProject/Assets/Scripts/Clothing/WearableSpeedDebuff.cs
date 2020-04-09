using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// when wore, this item will apply a debuff on player speed
/// </summary>
public class WearableSpeedDebuff : MonoBehaviour, IServerInventoryMove
{
    private PlayerScript player;
    const float InitialRunSpeed = 6;
    
    [SerializeField]
    [Tooltip("This will be the speed to substract from the initial run speed")]
    private float runningSpeedDebuff = 1.5f;
    
    [SerializeField]
    [Tooltip("In what slot should this debuff take place.")]
    private NamedSlot slot;

    public void OnInventoryMoveServer(InventoryMove info)
	{   
		//Wearing
		if (info.ToSlot != null & info.ToSlot?.NamedSlot != null)
		{
            player = info.ToRootPlayer?.PlayerScript;

            if (player != null && info.ToSlot.NamedSlot == slot)
            {
                ServerChangeSpeed(InitialRunSpeed - runningSpeedDebuff);
            }
		}
		//taking off
		if (info.FromSlot != null & info.FromSlot?.NamedSlot != null)
		{
            player = info.FromRootPlayer?.PlayerScript;

            if (player != null && info.FromSlot.NamedSlot == slot)
            {

                ServerChangeSpeed(InitialRunSpeed);
            }
		}
	}

    private void ServerChangeSpeed(float speed)
	{
        player.playerMove.InitialRunSpeed = speed;
        player.playerMove.RunSpeed = speed;

        if (player.PlayerSync.SpeedServer < speed)
        {
            return;
        }

        player.PlayerSync.SpeedServer = speed;
	}
}
