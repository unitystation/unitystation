using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

public class ReactorTurbine : MonoBehaviour, INodeControl, ISetMultitoolSlave, ISetMultitoolMaster, IServerDespawn, ICheckedInteractable<HandApply>
{
	private float tickCount;

	public ModuleSupplyingDevice moduleSupplyingDevice;

	public ReactorBoiler Boiler;
    // Start is called before the first frame update
    void Start()
    {
	    moduleSupplyingDevice = this.GetComponent<ModuleSupplyingDevice>();
    }

    private void OnEnable()
    {
	    if (CustomNetworkManager.Instance._isServer == false ) return;

	    UpdateManager.Add(CycleUpdate, 1);
	    //moduleSupplyingDevice = this.GetComponent<ModuleSupplyingDevice>();
	    moduleSupplyingDevice?.TurnOnSupply();
    }

    private void OnDisable()
    {
	    if (CustomNetworkManager.Instance._isServer == false ) return;

	    UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
	    moduleSupplyingDevice?.TurnOffSupply();
    }

    // Update is called once per frame
    public void CycleUpdate()
    {
	    if (Boiler != null)
	    {
		    //Logger.Log("  moduleSupplyingDevice.ProducingWatts " +   moduleSupplyingDevice.ProducingWatts);
		    moduleSupplyingDevice.ProducingWatts = (float) Boiler.OutputEnergy;
	    }
	    else
	    {
		    moduleSupplyingDevice.ProducingWatts = 0;
	    }

    }

    void INodeControl.PowerNetworkUpdate()
    {
	    //Stuff for the senses to read
    }

    public bool WillInteract( HandApply interaction, NetworkSide side )
    {

	    if (!DefaultWillInteract.Default(interaction, side)) return false;
	    if (!Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder)) return false;

	    return true;
    }

    public void ServerPerformInteraction(HandApply interaction)
    {
	    if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder))
	    {
		    ToolUtils.ServerUseToolWithActionMessages(interaction, 10,
			    "You start to deconstruct the ReactorTurbine..",
			    $"{interaction.Performer.ExpensiveName()} starts to deconstruct the ReactorTurbine...",
			    "You deconstruct the ReactorTurbine",
			    $"{interaction.Performer.ExpensiveName()} deconstruct the ReactorTurbine.",
			    () =>
			    {
				    Despawn.ServerSingle(gameObject);
			    });
	    }
    }


    /// <summary>
    /// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
    /// </summary>
    void IServerDespawn.OnDespawnServer(DespawnInfo info)
    {
	    Spawn.ServerPrefab(CommonPrefabs.Instance.Plasteel, this.GetComponent<RegisterObject>().WorldPositionServer, count: 25 );
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
		    Boiler = boiler;
	    }
    }

    private bool multiMaster = false;
    public bool MultiMaster => multiMaster;
    public void AddSlave(object SlaveObjectThis) { }
}
