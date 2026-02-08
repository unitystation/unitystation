using System.Threading.Tasks;
using UnityEngine;
using US13.Core.Physics;
using US13.Effects;
using US13.Objects.Gateway;
using US13.Systems.InGameEvents.InGameEventScripts;
using Util;

namespace US13.Objects.Research
{
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
}
