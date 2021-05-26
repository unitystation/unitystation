using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Electricity
{
	public class ElectricalMetaData
	{
		public MetaDataNode IsOn;
		public Vector3Int NodeLocation;
		public IntrinsicElectronicData InData;
		public Matrix Locatedon;
		public ElectricalCableTile RelatedTile;

		public void Initialise(WireConnect DataToTake, MetaDataNode metaDataNode, Vector3Int searchVec, Matrix locatedon)
		{
			IsOn = metaDataNode;
			InData = new IntrinsicElectronicData();
			InData.SetUp(DataToTake.InData);
			NodeLocation = searchVec;
			Locatedon = locatedon;
			InData.MetaDataPresent = this;
			InData.Present = null;
		}

		public void Initialise(ElectricalCableTile DataToTake, MetaDataNode metaDataNode, Vector3Int searchVec, Matrix locatedon)
		{
			RelatedTile = DataToTake;
			IsOn = metaDataNode;
			InData = new IntrinsicElectronicData();
			InData.SetUp(DataToTake);
			InData.MetaDataPresent = this;
			NodeLocation = searchVec;
			Locatedon = locatedon;
			InData.MetaDataPresent = this;
			InData.Present = null;
		}

		public virtual void FindPossibleConnections()
		{
			InData.Data.connections.Clear();
			ElectricityFunctions.FindPossibleConnections(
				Locatedon,
				InData.CanConnectTo,
				InData.GetConnPoints(),
				InData,
				InData.Data.connections
			);
		}
	}
}
