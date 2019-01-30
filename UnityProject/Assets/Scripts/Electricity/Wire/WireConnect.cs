using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class WireConnect : ElectricalOIinheritance
{
	public CableLine RelatedLine;

	public override void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, CableLine PassOn  = null){
		InputOutputFunctions.DirectionInput (tick, SourceInstance, ComingFrom, this);
		if (PassOn == null) {
			if (RelatedLine != null) {
				if (RelatedLine.TheEnd == this.GetComponent<IElectricityIO> ()) {
					Logger.Log ("looc");
				} else if (RelatedLine.TheStart == this.GetComponent<IElectricityIO> ()) {
					Logger.Log ("cool");
				} else {
					Logger.Log ("hELP{!!!");
				}
			} else {
				if (!(Data.connections.Count > 2)) {
					RelatedLine = new CableLine ();
					if (RelatedLine == null) {
						Logger.Log ("HE:LP:::::::::::::::::::niniinininininin");
					}
					RelatedLine.InitialGenerator = SourceInstance;
					RelatedLine.TheStart = this;
					lineExplore (RelatedLine, SourceInstance);
					if (RelatedLine.TheEnd == null) {
						RelatedLine = null;
					}
				}
			}
		}



	}

	public override void DirectionOutput(int tick, GameObject SourceInstance){
		
		int SourceInstanceID = SourceInstance.GetInstanceID();
		Logger.Log (this.gameObject.GetInstanceID().ToString() + " <ID | Downstream = "+Data.Downstream[SourceInstanceID].Count.ToString() + " Upstream = " + Data.Upstream[SourceInstanceID].Count.ToString () + this.name + " <  name! ", Category.Electrical);
		if (RelatedLine == null) {
			InputOutputFunctions.DirectionOutput (tick, SourceInstance, this);
		} else {
			
			IElectricityIO GoingTo; 
			if (RelatedLine.TheEnd == this.GetComponent<IElectricityIO> ()) {
				GoingTo = RelatedLine.TheStart;
			} else if (RelatedLine.TheStart == this.GetComponent<IElectricityIO> ()) {
				GoingTo = RelatedLine.TheEnd;
			} else {
				GoingTo = null;
				return; 
			}

			if (!(Data.Upstream.ContainsKey(SourceInstanceID)))
			{
				Data.Upstream[SourceInstanceID] = new HashSet<IElectricityIO>();
			}
			if (!(Data.Downstream.ContainsKey(SourceInstanceID)))
			{
				Data.Downstream[SourceInstanceID] = new HashSet<IElectricityIO>();
			}

			if (GoingTo != null) {
				Logger.Log ("to" + GoingTo.GameObject ().name);///wow
			}


			foreach (IElectricityIO bob in Data.Upstream [SourceInstanceID]){
				Logger.Log("Upstream" + bob.GameObject ().name );
			}
			if (!(Data.Downstream [SourceInstanceID].Contains (GoingTo) || Data.Upstream [SourceInstanceID].Contains (GoingTo)) )  {
				Data.Downstream [SourceInstanceID].Add (GoingTo);

				RelatedLine.DirectionInput (tick, SourceInstance, this);
			} else {
				InputOutputFunctions.DirectionOutput (tick, SourceInstance, this,RelatedLine);
			}
		}

		Data.DownstreamCount = Data.Downstream[SourceInstanceID].Count;
		Data.UpstreamCount = Data.Upstream[SourceInstanceID].Count;
	}
	public void lineExplore(CableLine PassOn, GameObject SourceInstance = null){
		RelatedLine = PassOn;
		if (!(this == PassOn.TheStart)) 
		{
			if (PassOn.TheEnd != null) 
			{
				PassOn.Covering.Add (PassOn.TheEnd);
				PassOn.TheEnd = this;
			} 
			else 
			{
				PassOn.TheEnd = this;
			}
		}
		if (Data.connections.Count <= 0)
		{
			FindPossibleConnections();
		}

		if (!(Data.connections.Count > 2)) 
		{
			for (int i = 0; i < Data.connections.Count; i++)
			{
				if (!(RelatedLine.Covering.Contains(Data.connections[i]) || RelatedLine.TheStart == Data.connections[i]))
				{
					bool canpass = true;
					if (SourceInstance != null) {
						int SourceInstanceID = SourceInstance.GetInstanceID();
						if (Data.Upstream[SourceInstanceID].Contains(Data.connections[i])){
							canpass = false;
						}
					}
					if (canpass) {
						if (Data.connections [i].GameObject ().GetComponent<WireConnect> () != null) {
							Data.connections [i].GameObject ().GetComponent<WireConnect> ().lineExplore (RelatedLine);
						}
					}

				}
			}
		}
	}
}