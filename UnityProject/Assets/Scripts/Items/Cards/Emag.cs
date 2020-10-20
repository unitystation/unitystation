using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Emag charges handler
/// </summary>
public class Emag : NetworkBehaviour
{
    [Tooltip("Number of charges emags start with")]
    [SerializeField]
    public static int startCharges = 3;

    [Tooltip("Number of seconds it takes to regenerate 1 charge")]
    [SerializeField]
    public static float rechargeTimeInSeconds = 10f;

	private SpriteHandler spriteHandler;

    private int charges = startCharges;

	/// <summary>
	/// Number of charges left on emag
	/// </summary>
    public int Charges => charges;

    private void Awake()
    {
        spriteHandler = gameObject.transform.GetChild(1).GetComponent<SpriteHandler>(); //overlay is second child
    }

    public void OnDisable()
    {
        if (isServer)
        {
            UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RegenerateCharge);
        }
    }

	/// <summary>
	/// Uses one charge from the emag, returns true if successful
	/// </summary>
    public bool UseCharge() 
    {
        if(Charges > 0)
        {
            //if this is the first charge taken off, add recharge loop
            if(Charges >= startCharges)
            {
                UpdateManager.Add(RegenerateCharge, rechargeTimeInSeconds);
            }

            charges -= 1;
            if(Charges > 0)
            {
                spriteHandler.ChangeSprite(Charges-1);
            }
            else 
            {
                spriteHandler.Empty();
            }
            return true;
        }
        return false;
    }

    private void RegenerateCharge()
    {
        if(Charges < startCharges)
        {
            charges += 1;
            spriteHandler.ChangeSprite(Charges-1);
        }
        if(Charges >= startCharges)
        {
            UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RegenerateCharge);
        }
    }
}
