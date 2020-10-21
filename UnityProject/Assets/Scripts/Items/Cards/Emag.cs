using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Emag charges handler
/// </summary>
public class Emag : NetworkBehaviour
{
	private SpriteHandler spriteHandler;

    [Tooltip("Number of charges emags start with")]
    [SerializeField]
    public int startCharges = 3;

    [Tooltip("Number of seconds it takes to regenerate 1 charge")]
    [SerializeField]
    public float rechargeTimeInSeconds = 10f;

    private int charges;
	/// <summary>
	/// Number of charges left on emag
	/// </summary>
    public int Charges => charges;

    private string OutOfChargesSFX = "Sparks04";

    private void Awake()
    {
        charges = startCharges;
        spriteHandler = gameObject.transform.Find("Charges").GetComponent<SpriteHandler>();
    }

    public void OnDisable()
    {
        if (isServer)
        {
            UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RegenerateCharge);
        }
    }

    ///<summary>
    ///Used to scale charges if starting charges > 3 so it will show proper pips
    ///</summary>
    private int ScaleChargesToSpriteIndex()
    {
        int output = Mathf.CeilToInt(((float)Charges/(float)startCharges)*3f) - 1;
        return output;
    }

	/// <summary>
	/// Uses one charge from the emag, returns true if successful
	/// </summary>
    public bool UseCharge(HandApply interaction) 
    {
        Logger.Log(spriteHandler.name);
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
                spriteHandler.ChangeSprite(ScaleChargesToSpriteIndex());
            }
            else 
            {
                SoundManager.PlayNetworkedForPlayer(recipient:interaction.Performer, sndName:OutOfChargesSFX, sourceObj:gameObject);
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
            spriteHandler.ChangeSprite(ScaleChargesToSpriteIndex());
        }
        if(Charges >= startCharges)
        {
            UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RegenerateCharge);
        }
    }
}
