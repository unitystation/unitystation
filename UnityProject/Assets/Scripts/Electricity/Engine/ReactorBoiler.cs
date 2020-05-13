using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReactorBoiler : MonoBehaviour
{
	[SerializeField] private float tickRate = 1;
	private float tickCount;

	public decimal OutputEnergy;
	public decimal TotalEnergyInput;
	public decimal Efficiency  = 0.9M;
	//public ReactorTurbine reactorTurbine;
	public List<ReactorGraphiteChamber> Chambers;
	// Start is called before the first frame update

    private void OnEnable()
    {
	    UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
    }

    private void OnDisable()
    {
	    UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
    }

    public void UpdateMe()
    {
	    //Only update at set rate
	    tickCount += Time.deltaTime;
	    if (tickCount < tickRate)
	    {
		    return;
	    }
	    tickCount = 0;

	    CycleUpdate();
    }

    public void CycleUpdate()
    {
	    TotalEnergyInput = 0;
	    foreach (var Chamber in  Chambers)
	    {
		    TotalEnergyInput += Chamber.EnergyReleased;
	    }
	    //if Energy too great explode//Change to temperature water exchange
	    OutputEnergy = TotalEnergyInput * Efficiency;
    }
}
