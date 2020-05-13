using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReactorTurbine : MonoBehaviour, INodeControl
{
	[SerializeField] private float tickRate = 1;
	private float tickCount;

	private ModuleSupplyingDevice moduleSupplyingDevice;

	public ReactorBoiler Boiler;
    // Start is called before the first frame update
    void Start()
    {
	    moduleSupplyingDevice = this.GetComponent<ModuleSupplyingDevice>();
    }

    private void OnEnable()
    {
	    UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	    moduleSupplyingDevice.TurnOnSupply();
    }

    private void OnDisable()
    {
	    UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	    moduleSupplyingDevice.TurnOffSupply();
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

    // Update is called once per frame
    public void CycleUpdate()
    {
	    Logger.Log("  moduleSupplyingDevice.ProducingWatts " +   moduleSupplyingDevice.ProducingWatts);
	    moduleSupplyingDevice.ProducingWatts = (float) Boiler.OutputEnergy;
    }

    void INodeControl.PowerNetworkUpdate()
    {
	    //Stuff for the senses to read
    }
}
