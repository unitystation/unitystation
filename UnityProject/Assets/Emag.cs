using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Emag : NetworkBehaviour
{
    [SerializeField]
    public static int startCharges = 3;

    [SerializeField]
    public static int rechargeTimeInSeconds = 10;

    private int charges = startCharges;
    // Start is called before the first frame update
    public bool UseCharge() 
    {
        if(this.charges > 0)
        {
            this.charges -= 1;
            return true;
        }
        return false;
    }

    private void RegenerateCharge()
    {
        if(this.charges < startCharges)
        {
            this.charges += 1;
        }
    }

    public int GetCharges()
    {
        return this.charges;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
