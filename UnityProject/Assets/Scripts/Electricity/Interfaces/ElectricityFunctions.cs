using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricityFunctions : MonoBehaviour {

	public static List<IElectricityIO> FindPossibleConnections(Vector2 searchVec,Matrix matrix, HashSet<PowerTypeCategory> CanConnectTo, ConnPoint ConnPoints ){
		List<IElectricityIO> possibleConns = new List<IElectricityIO>();
		List<IElectricityIO> connections = new List<IElectricityIO>();
		//possibleConns.Clear();
		//connections.Clear();
		int progress = 0;
		//Vector2 searchVec = transform.localPosition;
		searchVec.x -= 1;
		searchVec.y -= 1;
		for (int x = 0; x < 3; x++){
			for (int y = 0; y < 3; y++){
				Vector3Int pos = new Vector3Int((int)searchVec.x + x,
					(int)searchVec.y + y, 0);
				var conns = matrix.GetElectricalConnections(pos);
				foreach(IElectricityIO io in conns){
					possibleConns.Add(io);
					if (CanConnectTo.Contains
						(io.Categorytype)) {
						//Check if InputPosition and OutputPosition connect with this wire
						if(ConnectionMap.IsConnectedToTile(ConnPoints, (AdjDir)progress, io.GetConnPoints())){
							connections.Add(io);
						} 
					}

				}
				progress++;
			}
		}
		return(connections);
	}
	public static bool CalculateDirectionBool(IElectricityIO From, IElectricityIO To){
		bool isTrue = false;
		int UesID = From.FirstPresent;
		if (From.Upstream[UesID].Contains(To)){
			isTrue = true;
			return(isTrue);
		} else {
			return(isTrue);
		}
	}



	public static float WorkOutResistance(Dictionary<IElectricityIO,float> ResistanceSources){
		float Resistance = 0;
		float ResistanceXAll = 0;
		foreach (KeyValuePair<IElectricityIO, float> Source in ResistanceSources) {
			ResistanceXAll += 1 / Source.Value;
		}
		Resistance = 1 / ResistanceXAll;
		return(Resistance);
	}

	public static float WorkOutCurrent(Dictionary<IElectricityIO,float> ReceivingCurrents){
		float Current = 0;
		foreach (KeyValuePair<IElectricityIO, float> Source in ReceivingCurrents) {
			Current += Source.Value;
		}
		return(Current);
	}
	public static float WorkOutActualNumbers(IElectricityIO ElectricItem){
		float Current = 0;
		foreach (KeyValuePair<int, Dictionary<IElectricityIO,float>> CurrentIDItem in ElectricItem.CurrentComingFrom) { // yehahah Fix this!#
			foreach (KeyValuePair<IElectricityIO,float> CurrentItem in CurrentIDItem.Value) {
				bool directionbool = CalculateDirectionBool (ElectricItem, CurrentItem.Key);
				if (directionbool) {
					Current += CurrentItem.Value;
				} else {
					Current += -CurrentItem.Value;
				}
			}

		}
//		Electricity Elect = new Electricity ();
//		Elect.current = Current;
		return(Current);
	} 






	public static void ElectricityOutput(int tick, float Current, GameObject SourceInstance,IElectricityIO Thiswire ){
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		float SimplyTimesBy = 0; 
		float SupplyingCurrent = 0; 
		//Logger.Log (Thiswire.ResistanceTosource.ToString () + " yeah it is odd", Category.Electrical);
//		foreach (KeyValuePair<int, Dictionary<IElectricityIO,float>> CurrentIDItem in Thiswire.ResistanceTosource) { // yehahah Fix this!#
//			Logger.Log(CurrentIDItem.Key.ToString () + " <The master  vs GetInstanceID > " +  SourceInstanceID.ToString(), Category.Electrical);
//			if (CurrentIDItem.Key == SourceInstanceID) {
//				Logger.Log ("Mind Blown.gif ", Category.Electrical);
//			}
//			foreach (KeyValuePair<IElectricityIO,float> CurrentItem in CurrentIDItem.Value) {
//				Logger.Log (CurrentItem.Key.ToString () + " <key and Value > " + CurrentItem.Value.ToString (), Category.Electrical);
//			}
//		}
		Dictionary<IElectricityIO,float> ThiswireResistance = new Dictionary<IElectricityIO, float>();
		if (Thiswire.ResistanceTosource.ContainsKey(SourceInstanceID)){
			ThiswireResistance = Thiswire.ResistanceTosource[SourceInstanceID];
			//Logger.Log ("It does", Category.Electrical);

		} else {
			Logger.LogError ("now It doesn't" + SourceInstanceID.ToString() + " with this "+ Thiswire.GameObject().name.ToString(), Category.Electrical);
			
		}

		float Voltage = Current*(ElectricityFunctions.WorkOutResistance (ThiswireResistance));

		if (Thiswire.ResistanceTosource[SourceInstanceID].Count > 1) {
			float CurrentAll = 0; 
			foreach (KeyValuePair<IElectricityIO,float> JumpTo in Thiswire.ResistanceTosource[SourceInstanceID]) {
				if (!(SourceInstance == Thiswire.GameObject())) {
					CurrentAll += Voltage / ElectricityFunctions.WorkOutResistance (JumpTo.Key.ResistanceTosource [SourceInstanceID]);
				}

			}
			SimplyTimesBy = Current / CurrentAll;
		}
		foreach (KeyValuePair<IElectricityIO,float> JumpTo in Thiswire.ResistanceTosource[SourceInstanceID]) {
			if (!(SourceInstance == JumpTo.Key.GameObject ())) { 
				bool DirectionBool = ElectricityFunctions.CalculateDirectionBool (Thiswire, JumpTo.Key);
				if (SimplyTimesBy > 0) {
					SupplyingCurrent = SimplyTimesBy * (Voltage / ElectricityFunctions.WorkOutResistance (JumpTo.Key.ResistanceTosource [SourceInstanceID]));
				} else {
					SupplyingCurrent = Current;
				}
				JumpTo.Key.ElectricityInput (tick, SupplyingCurrent, SourceInstance, Thiswire);
			}
		}
	}


	public static void ElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom,IElectricityIO Thiswire ){ 
		//Logger.Log (tick.ToString () + " <tick " + Current.ToString () + " <Current " + SourceInstance.ToString () + " <SourceInstance " + ComingFrom.ToString () + " <ComingFrom " + Thiswire.ToString () + " <Thiswire ", Category.Electrical);
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		//if (electricity.voltage > 1f) {
		//	connected = true;
		//}
		if (!(Thiswire.CurrentComingFrom.ContainsKey(SourceInstanceID))){
			Thiswire.CurrentComingFrom [SourceInstanceID] = new Dictionary<IElectricityIO, float> ();
		}
		Thiswire.CurrentComingFrom [SourceInstanceID] [ComingFrom] = Current;
		Thiswire.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers (Thiswire);
		//Logger.Log (Thiswire.ActualCurrentChargeInWire.ToString () + " How much current", Category.Electrical);
		//Pass the charge on:
		Thiswire.ElectricityOutput(tick, ElectricityFunctions.WorkOutCurrent(Thiswire.CurrentComingFrom [SourceInstanceID]),SourceInstance);

	}



	public static void ResistancyOutput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO Thiswire){
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		//VisibleResistance = Resistance;
		float ResistanceSplit = 0;
		if (Thiswire.Upstream[SourceInstanceID].Count > 1) {
			float CalculatedCurrent = 1000 / Resistance;
			float CurrentSplit = CalculatedCurrent / (Thiswire.Upstream[SourceInstanceID].Count);
			ResistanceSplit = 1000 / CurrentSplit;
		} else {
			ResistanceSplit = Resistance;
		}
		//currentTick = tick;
		foreach (IElectricityIO JumpTo in Thiswire.Upstream[SourceInstanceID]) {
			//Logger.Log ( "ResistanceSplit " + ResistanceSplit.ToString () + "JumpTo " + JumpTo.ToString () + "From" + Thiswire.ToString (), Category.Electrical);
			JumpTo.ResistanceInput (tick, ResistanceSplit, SourceInstance, Thiswire);
		}
	}



	public static void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO Thiswire ){
		int SourceInstanceID = SourceInstance.GetInstanceID();
		//ResistanceTosource
		//Logger.Log ( "ResistanceSplit " + Resistance.ToString () + "JumpTo " + ComingFrom.ToString () + "From" + this.gameObject.ToString (), Category.Electrical);
		if (!(Thiswire.ResistanceTosource.ContainsKey (SourceInstanceID))) {
			Thiswire.ResistanceTosource [SourceInstanceID] = new Dictionary<IElectricityIO,float> ();
		}
		Thiswire.ResistanceTosource [SourceInstanceID] [ComingFrom] = Resistance;
		Thiswire.ResistancyOutput(tick, ElectricityFunctions.WorkOutResistance(Thiswire.ResistanceTosource[SourceInstanceID]), SourceInstance);
	}






	public static void DirectionOutput(int tick, GameObject SourceInstance, IElectricityIO Thiswire) {
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		int Count = 0;
		for (int i = 0; i < Thiswire.connections.Count; i++) {
			if (Thiswire.Upstream [SourceInstanceID].Contains (Thiswire.connections [i])) {
				Count++;
			}
		}
		for (int i = 0; i < Thiswire.connections.Count; i++) {
			if (!(Thiswire.Upstream [SourceInstanceID].Contains (Thiswire.connections [i])) && (!(Thiswire == Thiswire.connections [i]))) {

				if (!(Thiswire.Downstream.ContainsKey (SourceInstanceID))) {
					Thiswire.Downstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
				}
				Thiswire.Downstream [SourceInstanceID].Add (Thiswire.connections [i]);

				Thiswire.connections [i].DirectionInput (tick, SourceInstance, Thiswire);
			} 
		}
	}




	public static void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO Thiswire){
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		if (Thiswire.FirstPresent == 0) {
			Logger.Log ("to It's been claimed", Category.Electrical);
			Thiswire.FirstPresent = SourceInstanceID;
			//Thiswire.FirstPresentInspector = SourceInstanceID;
		}
		if (!(Thiswire.Upstream.ContainsKey (SourceInstanceID))) {
			Thiswire.Upstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
		}
		if (!(Thiswire.Downstream.ContainsKey (SourceInstanceID))) {
			Thiswire.Downstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
		}
		Thiswire.Upstream [SourceInstanceID].Add(ComingFrom);

		Thiswire.DirectionOutput(tick, SourceInstance);
	} 
}
