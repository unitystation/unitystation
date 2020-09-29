using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoilerTurbineController : MonoBehaviour, ISetMultitoolSlave
{


	public bool State = false;
	public ReactorBoiler ReactorBoiler = null;
	public ReactorTurbine ReactorTurbine = null;

	[RightClickMethod]
    public void ChangeState()
    {
	    State = !State;
    }

    //######################################## Multitool interaction ##################################
    [SerializeField]
    private MultitoolConnectionType conType = MultitoolConnectionType.BoilerTurbine;
    public MultitoolConnectionType ConType  => conType;

    public void SetMaster(ISetMultitoolMaster Imaster)
    {
	    var boiler  = (Imaster as Component)?.gameObject.GetComponent<ReactorBoiler>();
	    if (boiler != null)
	    {
		    ReactorBoiler = boiler;
	    }
	    var Turbine  = (Imaster as Component)?.gameObject.GetComponent<ReactorTurbine>();
	    if (Turbine != null)
	    {
		    ReactorTurbine = Turbine;
	    }
    }

}
