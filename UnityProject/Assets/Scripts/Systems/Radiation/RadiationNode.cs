using System;
using System.Collections.Generic;

namespace Systems.Radiation
{
	/// <summary>
	/// Used for Gathering the radiation each node
	/// </summary>
	public class RadiationNode
	{
		public float MidCalculationNumbers = 0;

		public float RadiationLevel => CalculateRadiationLevel();

		public List<RadiationPulseRecord> RecordedPulses = new List<RadiationPulseRecord>();

		public float CalculateRadiationLevel(int BlockId = 0)
		{
			float RadiationLevel = 0;
			for (var i = 0; i < RecordedPulses.Count; i++)
			{
				if (RecordedPulses[i].RadiationStrength == 0) continue;
				if ((DateTime.Now - RecordedPulses[i].Timestamp).TotalSeconds > 10)
				{
					RecordedPulses[i] = RadiationPulseRecord.ToReplace();
				}
				else
				{
					if (RecordedPulses[i].SourceID != BlockId)
					{
						RadiationLevel += RecordedPulses[i].RadiationStrength;
					}
				}
			}
			return (RadiationLevel);
		}

		public void AddRadiationPulse(float InRadiationStrength, DateTime InTimestamp, int InSourceID)
		{
			int ReplaceIndex = -1;
			for (var i = 0; i < RecordedPulses.Count; i++)
			{
				if ( RecordedPulses[i].Destroyed || RecordedPulses[i].SourceID == InSourceID )
				{
					if (RecordedPulses[i].Destroyed)
					{
						if (ReplaceIndex == -1)
						{
							ReplaceIndex = i;
						}
					}
					else
					{
						RecordedPulses[i] = new RadiationPulseRecord(InRadiationStrength, InTimestamp,InSourceID);
						return;
					}
				}
			}

			if (ReplaceIndex != -1)
			{
				RecordedPulses[ReplaceIndex] = new RadiationPulseRecord(InRadiationStrength, InTimestamp,InSourceID);
			}
			else
			{
				RecordedPulses.Add(new RadiationPulseRecord(InRadiationStrength, InTimestamp,InSourceID));
			}
		}


		public struct RadiationPulseRecord
		{
			public bool Destroyed;
			public float RadiationStrength;
			public DateTime Timestamp;
			public int SourceID;
			public RadiationPulseRecord(float InRadiationStrength, DateTime InTimestamp, int InSourceID, bool InDestroyed = false)
			{
				RadiationStrength = InRadiationStrength;
				Timestamp = InTimestamp;
				SourceID = InSourceID;
				Destroyed = false;
			}

			public static RadiationPulseRecord ToReplace()
			{
				return new RadiationPulseRecord(0, DateTime.Now, 0, true);
			}
		}
	}
}
