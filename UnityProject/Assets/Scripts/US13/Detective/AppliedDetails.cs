using System.Collections.Generic;
using UnityEngine;

namespace US13.Detective
{
	public class AppliedDetails
	{
		public HashSet<EntityId> Interacted = new HashSet<EntityId>();

		public List<Detail> Details = new List<Detail>();

		public System.Random RNG = new System.Random();

		public void AddDetail(Detail Detail)
		{
			if (Interacted.Contains(Detail.CausedByEntityId)) return;
			if (Details.Count == 15)
			{
				Details.RemoveAt(RNG.Next(0, Details.Count-1));
			}

			if (Details.Count > 0)
			{
				Details.Insert(RNG.Next(0, Details.Count-1), Detail);
			}
			else
			{
				Details.Add(Detail);
			}

			Interacted.Add(Detail.CausedByEntityId);
		}

		public void Clean()
		{
			Interacted.Clear();
			Details.Clear();
		}

	}




	public class Detail
	{
		public EntityId CausedByEntityId;
		public string Description;
		public DetailType DetailType;

	}

	public enum DetailType
	{
		Fibre,
		Fingerprints,
		SpeciesIdentify,

		Footprints,
		Blood, //idk Need blood Splats

		BulletHole, //Applied Decal call
	}
}