using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalComplexNodeControl : ElectricalModuleInheritance
{
	public List<ElectricalNodeControl> SimpleNods = new List<ElectricalNodeControl>();

	public virtual void BroadcastSetUpMessage(ElectricalNodeControl Node)
	{
		RequiresUpdateOn = new HashSet<ElectricalUpdateTypeCategory>
		{

		};
		ModuleType = ElectricalModuleTypeCategory.SupplyingDevice;
		ControllingNode = Node;
		Node.AddModule(this);
	}
}
