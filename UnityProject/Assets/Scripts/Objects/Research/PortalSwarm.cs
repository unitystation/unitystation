using System.Threading.Tasks;
using Core.Physics;
using Effects;
using Gateway;
using InGameEvents;
using Objects.Research;
using UnityEngine;

public class PortalSwarm : Portal
{
	public override async Task Teleport(GameObject eventData)
	{


		if (eventData.HasComponent<SparkEffect>()) return;
		if(eventData.TryGetComponent<UniversalObjectPhysics>(out var uop) == false) return;

		lastActivationTime = Time.time;

		var Portal = EventSpatialDistortion.ActivePortal.PickRandom();
		Portal.GetComponent<Portal>().lastActivationTime = Time.time;
		TransportUtility.TransportObjectAndPulled(uop, Portal.transform.position, false);
	}

}
