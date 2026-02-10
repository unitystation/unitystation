using UnityEngine;
using US13.Core.ObjectConnection;
using US13.Tilemaps.Behaviours.Meta;

namespace US13.Shuttles
{
	public class GuidanceBuoy : ItemMatrixSystemInit, IMultitoolMasterable
	{
		public GuidanceBuoyMoveStep Out;
		public GuidanceBuoyMoveStep In;

		[field: SerializeField] public bool CanRelink { get; set; } = true;
		public MultitoolConnectionType ConType => MultitoolConnectionType.APC;

		int IMultitoolMasterable.MaxDistance => 30;

		bool IMultitoolMasterable.IgnoreMaxDistanceMapper => true;

	}
}


