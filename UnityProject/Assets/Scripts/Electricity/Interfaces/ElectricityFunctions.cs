using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

public static class ElectricityFunctions  {

	public static List<IElectricityIO> FindPossibleConnections(Vector2 searchVec,Matrix matrix, HashSet<PowerTypeCategory> CanConnectTo, ConnPoint ConnPoints ){
		List<IElectricityIO> possibleConns = new List<IElectricityIO>();
		List<IElectricityIO> connections = new List<IElectricityIO>();
		int progress = 0;
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
						(io.InData.Categorytype)) {
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
	public static bool CalculateDirectionBool(IElectricityIO From, IElectricityIO To,bool Upstream ){
		bool isTrue = false;
		int UesID = From.Data.FirstPresent;
		if (Upstream) {
			if (From.Data.Upstream [UesID].Contains (To)) {
				isTrue = true;
				return(isTrue);
			} else {
				return(isTrue);
			}
		} else {
			if (From.Data.Downstream[UesID].Contains(To)){
				isTrue = true;
				return(isTrue);
			} else {
				return(isTrue);
			}
		}
	}

	public static bool CalculateDirectionFromID(IElectricityIO On, int TheID ){
		bool isTrue = false;
		if (!(On.Data.ResistanceComingFrom.ContainsKey (TheID))) {
			return(true);
		}
		if (!(On.Data.ResistanceComingFrom.ContainsKey (On.Data.FirstPresent))) {
			return(true);
		}
		isTrue = true;
		foreach (KeyValuePair<IElectricityIO,float>  CurrentItem in On.Data.ResistanceComingFrom [On.Data.FirstPresent]) { 

			if (!(On.Data.ResistanceComingFrom [TheID].ContainsKey (CurrentItem.Key))) {
				isTrue = false;
			}
		}
		return(isTrue);


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
	public static Electricity WorkOutActualNumbers(IElectricityIO ElectricItem){ 
		float Current = 0;
		float Voltage = 0;
		Dictionary<IElectricityIO,float> AnInterestingDictionary = new Dictionary<IElectricityIO, float> ();
		foreach (KeyValuePair<int, float> CurrentIDItem in ElectricItem.Data.SourceVoltages) { 
			Voltage += CurrentIDItem.Value;
		}
		foreach (KeyValuePair<int, Dictionary<IElectricityIO,float>> CurrentIDItem in ElectricItem.Data.CurrentComingFrom) { 
			foreach (KeyValuePair<IElectricityIO,float> CurrentItem in CurrentIDItem.Value) {
				if (AnInterestingDictionary.ContainsKey (CurrentItem.Key)) {
					AnInterestingDictionary [CurrentItem.Key] += CurrentItem.Value;
				} else {
					AnInterestingDictionary [CurrentItem.Key] = CurrentItem.Value;
				}
			}
			if (ElectricItem.Data.CurrentGoingTo.ContainsKey (CurrentIDItem.Key)) {
				foreach (KeyValuePair<IElectricityIO,float> CurrentItem in ElectricItem.Data.CurrentGoingTo[CurrentIDItem.Key]) {
					if (AnInterestingDictionary.ContainsKey (CurrentItem.Key)) {
						AnInterestingDictionary [CurrentItem.Key] += -CurrentItem.Value;
					} else {
						AnInterestingDictionary [CurrentItem.Key] = -CurrentItem.Value;
					}
				}
			}
		}
		foreach (KeyValuePair<IElectricityIO,float> CurrentItem in AnInterestingDictionary) { //!! It's not working somewhere at 17 and 9?
			if (CurrentItem.Value > 0) {
				Current += CurrentItem.Value;
			}
		}
		//Logger.Log (Voltage.ToString () + " < yeah Those voltage " + Current.ToString() + " < yeah Those Current " + (Voltage/Current).ToString() + " < yeah Those Resistance" + ElectricItem.GameObject().name.ToString() + " < at", Category.Electrical);
		Electricity Cabledata = new Electricity ();
		Cabledata.Current = Current;
		Cabledata.Voltage = Voltage;
		Cabledata.EstimatedResistant = Voltage / Current;
		return(Cabledata);
	} 

	public static Tuple<float,float> TransformerCalculations( Itransformer TransformInformation, float ResistanceToModify = 0, float Voltage = 0, float ResistanceModified = 0, float ActualCurrent = 0 ){
		if (!(ResistanceToModify == 0)) {
			float R2 = ResistanceToModify;
			float I2 = 1/ResistanceToModify;
			float V2 = 1;

			float Turn_ratio = TransformInformation.TurnRatio;

			float V1 = (V2*Turn_ratio);
			float I1 = (V2/V1)*I2;
			float R1 = V1/I1;
			Tuple<float,float> returns = new Tuple<float, float>(
				R1, 
				0
			);
			return(returns);
		}
		if (!(Voltage == 0)) {
			float offcut = 0;
			float V1 = Voltage;
			float R1 = ResistanceModified;
			float I1 = V1/R1;
			float Turn_ratio = TransformInformation.TurnRatio;
			float V2 = V1/Turn_ratio;
			float IntervalI2 = (V1 / V2) * I1;
			float R2 = V2 / IntervalI2;
			if (!(TransformInformation.VoltageLimiting == 0)){ //if Total Voltage greater than that then  Push some of it to ground  to == VoltageLimitedTo And then everything after it to ground/
				float ActualVoltage = ActualCurrent * ResistanceModified;

				float SUBV1 = ActualVoltage;
				float SUBR1 = ResistanceModified;
				float SUBI1 = ActualCurrent;

				float SUBV2 = SUBV1/Turn_ratio;
				float SUBI2 = (SUBV1 / SUBV2) * SUBI1;
				float SUBR2 = SUBV2 / SUBI2;
				if ((V2 + SUBV2) > TransformInformation.VoltageLimiting) { 
					offcut = ((V2 + SUBV2) - TransformInformation.VoltageLimitedTo)/ R2;
					V2 = TransformInformation.VoltageLimitedTo - SUBV2;
					if (V2 < 0) {
						V2 = 0;
					}
				}
			}
			float I2 = V2/R2;
			Tuple<float,float> returns = new Tuple<float, float>(
				I2, 
				offcut
			);
			return(returns);
		}
		Tuple<float,float> returnsE = new Tuple<float, float>(0.0f, 0);
		return(returnsE);
	}
		
	public static void CircuitSearchLoop(IElectricityIO Thiswire, IProvidePower ProvidingPower){
		DirectionOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject(), Thiswire);
		bool Break = true;
		List<IElectricityIO> IterateDirectionWorkOnNextList = new List<IElectricityIO> ();
		while (Break) {
			IterateDirectionWorkOnNextList = new List<IElectricityIO> (ProvidingPower.DirectionWorkOnNextList);
			ProvidingPower.DirectionWorkOnNextList.Clear();
			for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
				IterateDirectionWorkOnNextList [i].DirectionOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject());
			}
			if (ProvidingPower.DirectionWorkOnNextList.Count <= 0) {
				IterateDirectionWorkOnNextList = new List<IElectricityIO> (ProvidingPower.DirectionWorkOnNextListWait);
				ProvidingPower.DirectionWorkOnNextListWait.Clear();
				for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
					IterateDirectionWorkOnNextList [i].DirectionOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject());
				}
			}
			if (ProvidingPower.DirectionWorkOnNextList.Count <= 0 && ProvidingPower.DirectionWorkOnNextListWait.Count <= 0) {
				//Logger.Log ("stop!");
				Break = false;
			}
		}
	}

	public static void CircuitResistanceLoop(IElectricityIO Thiswire, IProvidePower ProvidingPower ){
		bool Break = true;
		List<IElectricityIO> IterateDirectionWorkOnNextList = new List<IElectricityIO> ();
		while (Break) {
			IterateDirectionWorkOnNextList = new List<IElectricityIO> (ProvidingPower.ResistanceWorkOnNextList);
			ProvidingPower.ResistanceWorkOnNextList.Clear();
			for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
				IterateDirectionWorkOnNextList [i].ResistancyOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject());
			}
			if (ProvidingPower.ResistanceWorkOnNextList.Count <= 0) {
				IterateDirectionWorkOnNextList = new List<IElectricityIO> (ProvidingPower.ResistanceWorkOnNextListWait);
				ProvidingPower.ResistanceWorkOnNextListWait.Clear();
				for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
					IterateDirectionWorkOnNextList [i].ResistancyOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject());
				}
			}
			if (ProvidingPower.ResistanceWorkOnNextList.Count <= 0 && ProvidingPower.ResistanceWorkOnNextListWait.Count <= 0) {
				Break = false;
			}
		}
	}






	public static void ElectricityOutput(int tick, float Current, GameObject SourceInstance,IElectricityIO Thiswire ){
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		float SimplyTimesBy = 0; 
		float SupplyingCurrent = 0; 
		Dictionary<IElectricityIO,float> ThiswireResistance = new Dictionary<IElectricityIO, float>();
		if (Thiswire.Data.ResistanceComingFrom.ContainsKey(SourceInstanceID)){
			ThiswireResistance = Thiswire.Data.ResistanceComingFrom[SourceInstanceID];
		} else {
			Logger.LogError ("now It doesn't" + SourceInstanceID.ToString() + " with this "+ Thiswire.GameObject().name.ToString(), Category.Electrical);

		}
		float Voltage = Current*(ElectricityFunctions.WorkOutResistance (ThiswireResistance));
		foreach (KeyValuePair<IElectricityIO,float> JumpTo in Thiswire.Data.ResistanceComingFrom[SourceInstanceID]) {
			if (Voltage > 0) {
				SupplyingCurrent = (Voltage / JumpTo.Value);
			} else {
				SupplyingCurrent = Current;
			}
			if (!(Thiswire.Data.CurrentGoingTo.ContainsKey (SourceInstanceID))) {
				Thiswire.Data.CurrentGoingTo [SourceInstanceID] = new Dictionary<IElectricityIO, float> ();
			}
			Thiswire.Data.CurrentGoingTo [SourceInstanceID] [JumpTo.Key] = SupplyingCurrent;
			JumpTo.Key.ElectricityInput (tick, SupplyingCurrent, SourceInstance, Thiswire);
		}
	}
		
	public static void ElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom,IElectricityIO Thiswire ){ 
		//Logger.Log (tick.ToString () + " <tick " + Current.ToString () + " <Current " + SourceInstance.ToString () + " <SourceInstance " + ComingFrom.ToString () + " <ComingFrom " + Thiswire.ToString () + " <Thiswire ", Category.Electrical);
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		if (!(Thiswire.Data.SourceVoltages.ContainsKey(SourceInstanceID))){
			Thiswire.Data.SourceVoltages [SourceInstanceID] = new float ();
		} 
		if (!(Thiswire.Data.CurrentComingFrom.ContainsKey(SourceInstanceID))){
			Thiswire.Data.CurrentComingFrom [SourceInstanceID] = new Dictionary<IElectricityIO, float> ();
		}
		Thiswire.Data.CurrentComingFrom [SourceInstanceID] [ComingFrom] = Current;
		Thiswire.Data.SourceVoltages[SourceInstanceID] = Current * (WorkOutResistance (Thiswire.Data.ResistanceComingFrom [SourceInstanceID]));
		Thiswire.ElectricityOutput(tick, ElectricityFunctions.WorkOutCurrent(Thiswire.Data.CurrentComingFrom [SourceInstanceID]),SourceInstance);

	}

	public static void ResistancyOutput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO Thiswire){
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		float ResistanceSplit = 0;
		if (Thiswire.Data.Upstream[SourceInstanceID].Count > 1) {
			float CalculatedCurrent = 1000 / Resistance;
			float CurrentSplit = CalculatedCurrent / (Thiswire.Data.Upstream[SourceInstanceID].Count);
			ResistanceSplit = 1000 / CurrentSplit;
		} else {
			ResistanceSplit = Resistance;
		}
		foreach (IElectricityIO JumpTo in Thiswire.Data.Upstream[SourceInstanceID]) {
			Thiswire.Data.ResistanceGoingTo [SourceInstanceID] [JumpTo] = ResistanceSplit;
			JumpTo.ResistanceInput (tick, ResistanceSplit, SourceInstance, Thiswire);
		}
	}
		
	public static void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO Thiswire ){
		if (ComingFrom == null){
			foreach (PowerTypeCategory ConnectionFrom in Thiswire.Data.ResistanceToConnectedDevices[SourceInstance.GetComponent<IElectricityIO>()]) {
				Resistance = Thiswire.InData.ConnectionReaction[ConnectionFrom].ResistanceReactionA.Resistance.Float;
				//Logger.Log (Resistance.ToString () + " < to man Resistance |            " + ConnectionFrom.ToString() + " < to man ConnectionFrom |      " + Thiswire.GameObject().name + " < to man IS ");
				ComingFrom = ElectricalSynchronisation.DeadEnd;
			}
		}
		int SourceInstanceID = SourceInstance.GetInstanceID();
		if (!(Thiswire.Data.ResistanceComingFrom.ContainsKey (SourceInstanceID))) {
			Thiswire.Data.ResistanceComingFrom [SourceInstanceID] = new Dictionary<IElectricityIO,float> ();
		}
		if (!(Thiswire.Data.ResistanceGoingTo.ContainsKey (SourceInstanceID))) {
			Thiswire.Data.ResistanceGoingTo [SourceInstanceID] = new Dictionary<IElectricityIO,float> ();
		}
		Thiswire.Data.ResistanceComingFrom [SourceInstanceID] [ComingFrom] = Resistance;
		if (Thiswire.Data.connections.Count > 2) {
			SourceInstance.GetComponent<IProvidePower> ().ResistanceWorkOnNextListWait.Add (Thiswire);
		} else {
			SourceInstance.GetComponent<IProvidePower> ().ResistanceWorkOnNextList.Add (Thiswire);
		}

	}
		
	public static void DirectionOutput(int tick, GameObject SourceInstance, IElectricityIO Thiswire) {
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		if (!(Thiswire.Data.Upstream.ContainsKey (SourceInstanceID))) {
			Thiswire.Data.Upstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
		}
		if (!(Thiswire.Data.Downstream.ContainsKey (SourceInstanceID))) {
			Thiswire.Data.Downstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
		}
		if (Thiswire.Data.connections.Count <= 0) {
			Thiswire.FindPossibleConnections ();
		}
		for (int i = 0; i < Thiswire.Data.connections.Count; i++) {
			if (!(Thiswire.Data.Upstream [SourceInstanceID].Contains (Thiswire.Data.connections [i])) && (!(Thiswire == Thiswire.Data.connections [i]))) {

				if (!(Thiswire.Data.Downstream.ContainsKey (SourceInstanceID))) {
					Thiswire.Data.Downstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
				}
				if (!(Thiswire.Data.Downstream[SourceInstanceID].Contains (Thiswire.Data.connections [i]))) {
					Thiswire.Data.Downstream [SourceInstanceID].Add (Thiswire.Data.connections [i]);

					Thiswire.Data.connections [i].DirectionInput (tick, SourceInstance, Thiswire);
				}


			} 
		}
	}

	public static void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO Thiswire){
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		if (Thiswire.Data.FirstPresent == 0) {
			Thiswire.Data.FirstPresent = SourceInstanceID;
		}
		if (!(Thiswire.Data.Upstream.ContainsKey (SourceInstanceID))) {
			Thiswire.Data.Upstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
		}
		if (!(Thiswire.Data.Downstream.ContainsKey (SourceInstanceID))) {
			Thiswire.Data.Downstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
		}
		if (ComingFrom != null) {
			Thiswire.Data.Upstream [SourceInstanceID].Add(ComingFrom);
		}

		bool CanPass = true;
		if (Thiswire.InData.ConnectionReaction.ContainsKey (ComingFrom.InData.Categorytype)) {
			if (Thiswire.InData.ConnectionReaction [ComingFrom.InData.Categorytype].DirectionReaction) {
				if (Thiswire.InData.ConnectionReaction [ComingFrom.InData.Categorytype].DirectionReactionA.AddResistanceCall.Bool) {
					IProvidePower SourceInstancPowerSupply = SourceInstance.GetComponent<IProvidePower> ();
					if (SourceInstancPowerSupply != null){
						IElectricityIO IElectricityIOPowerSupply = SourceInstance.GetComponent<IElectricityIO> ();
						if (!Thiswire.Data.ResistanceToConnectedDevices.ContainsKey (IElectricityIOPowerSupply)) {
							Thiswire.Data.ResistanceToConnectedDevices [IElectricityIOPowerSupply] = new HashSet<PowerTypeCategory> ();
						}
						Thiswire.Data.ResistanceToConnectedDevices [IElectricityIOPowerSupply].Add (ComingFrom.InData.Categorytype);
						SourceInstancPowerSupply.connectedDevices.Add (Thiswire);
					}
				}
				if (Thiswire.InData.ConnectionReaction [ComingFrom.InData.Categorytype].DirectionReactionA.YouShallNotPass) {
					CanPass = false;
				} 
			}
		}
		if (CanPass){
			if (Thiswire.Data.connections.Count > 2) {
				SourceInstance.GetComponent<IProvidePower> ().DirectionWorkOnNextListWait.Add (Thiswire);
			} else {
				SourceInstance.GetComponent<IProvidePower> ().DirectionWorkOnNextList.Add (Thiswire);
			}
		}
	} 


	public static void CleanConnectedDevices(IElectricityIO Thiswire){
		Logger.Log ("Cleaning it out");
		foreach (KeyValuePair<IElectricityIO,HashSet<PowerTypeCategory>> IsConnectedTo in Thiswire.Data.ResistanceToConnectedDevices) {
			IsConnectedTo.Key.connectedDevices.Remove (Thiswire);
		}
		Thiswire.Data.ResistanceToConnectedDevices.Clear();
	}

	public static void CleanConnectedDevicesFromPower(IElectricityIO Thiswire){
		Logger.Log ("Cleaning it out");
		foreach (IElectricityIO IsConnectedTo in Thiswire.connectedDevices) {
			IsConnectedTo.Data.ResistanceToConnectedDevices.Remove (Thiswire);
		}
		Thiswire.connectedDevices.Clear();
	}

	public static class PowerSupplies{
		public static void FlushConnectionAndUp (IElectricityIO Object){
			if (Object.Data.connections.Count > 0) {
				List<IElectricityIO> Backupconnections = Object.Data.connections;
				Object.Data.connections.Clear();
				foreach (IElectricityIO JumpTo in Backupconnections) {
					JumpTo.FlushConnectionAndUp ();
				}
				Object.Data.Upstream.Clear();
				Object.Data.Downstream.Clear();
				Object.Data.ResistanceComingFrom.Clear();
				Object.Data.ResistanceGoingTo.Clear();
				Object.Data.CurrentGoingTo.Clear();
				Object.Data.CurrentComingFrom.Clear();
				Object.Data.SourceVoltages = new Dictionary<int, float> ();
				Object.Data.CurrentInWire = new float ();
				Object.Data.ActualVoltage = new float ();
				Object.Data.ResistanceToConnectedDevices.Clear();
				Object.connectedDevices.Clear();
			}

		}

		public static void FlushResistanceAndUp (IElectricityIO Object,  GameObject SourceInstance = null  ){
			if (SourceInstance == null) {
				if (Object.Data.ResistanceComingFrom.Count > 0) {
					Object.Data.ResistanceComingFrom.Clear ();
					foreach (IElectricityIO JumpTo in Object.Data.connections) {
						JumpTo.FlushResistanceAndUp ();
					}
					Object.Data.ResistanceGoingTo.Clear ();
					Object.Data.CurrentGoingTo.Clear ();
					Object.Data.CurrentComingFrom.Clear ();
					Object.Data.SourceVoltages.Clear ();
					Object.Data.CurrentInWire = new float ();
					Object.Data.ActualVoltage = new float ();
				}

			} else {
				int InstanceID = SourceInstance.GetInstanceID ();
				if (Object.Data.ResistanceComingFrom.ContainsKey (InstanceID) || Object.Data.ResistanceGoingTo.ContainsKey (InstanceID)) {
					Object.Data.ResistanceComingFrom.Remove (InstanceID);
					Object.Data.ResistanceGoingTo.Remove (InstanceID);
					foreach (IElectricityIO JumpTo in Object.Data.connections) {
						JumpTo.FlushResistanceAndUp (SourceInstance);
					}
					Object.Data.CurrentGoingTo.Remove (InstanceID);
					Object.Data.CurrentComingFrom.Remove (InstanceID);
					Object.Data.SourceVoltages.Remove (InstanceID);
					Object.Data.CurrentInWire = new float ();
					Object.Data.ActualVoltage = new float ();

				}
			}
		}

		public static void FlushSupplyAndUp (IElectricityIO Object,GameObject SourceInstance = null ){
			if (SourceInstance == null) {
				if (Object.Data.CurrentComingFrom.Count > 0) {
					Object.Data.CurrentComingFrom.Clear();
					foreach (IElectricityIO JumpTo in Object.Data.connections) {
						JumpTo.FlushSupplyAndUp();
					}
					Object.Data.CurrentGoingTo.Clear();

					Object.Data.SourceVoltages.Clear();
					Object.Data.CurrentInWire = new float ();
					Object.Data.ActualVoltage = new float ();
				}

			} else {
				int InstanceID = SourceInstance.GetInstanceID ();
				if (Object.Data.CurrentComingFrom.ContainsKey (InstanceID)) {
					Object.Data.CurrentGoingTo.Remove (InstanceID);
					Object.Data.CurrentComingFrom.Remove (InstanceID);
					foreach (IElectricityIO JumpTo in Object.Data.connections) {
						JumpTo.FlushSupplyAndUp (SourceInstance);
					}
				} else if (Object.Data.CurrentGoingTo.ContainsKey (InstanceID)) {
					Object.Data.CurrentGoingTo.Remove (InstanceID);
					Object.Data.CurrentComingFrom.Remove (InstanceID);
					foreach (IElectricityIO JumpTo in Object.Data.connections) {
						JumpTo.FlushSupplyAndUp (SourceInstance);
					}
				}
				Object.Data.CurrentGoingTo.Remove (InstanceID);
				Object.Data.SourceVoltages.Remove (InstanceID);
				Object.Data.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers (Object);
				Object.Data.CurrentInWire = Object.Data.ActualCurrentChargeInWire.Current;
				Object.Data.ActualVoltage = Object.Data.ActualCurrentChargeInWire.Voltage;
				Object.Data.EstimatedResistance = Object.Data.ActualCurrentChargeInWire.EstimatedResistant;
			}
		}

		public static void RemoveSupply(IElectricityIO Object,GameObject SourceInstance = null ){		
			if (SourceInstance == null) {
				if (Object.Data.Downstream.Count > 0 || Object.Data.Upstream.Count > 0) {
					Object.Data.Downstream.Clear();
					Object.Data.Upstream.Clear();
					Object.Data.FirstPresent = new int ();
					foreach (IElectricityIO JumpTo in Object.Data.connections) {
						JumpTo.RemoveSupply ();
					}
					Object.Data.Upstream.Clear();
					Object.Data.SourceVoltages.Clear();
					Object.Data.ResistanceGoingTo.Clear();
					Object.Data.ResistanceComingFrom.Clear();
					Object.Data.CurrentGoingTo.Clear();
					Object.Data.CurrentComingFrom.Clear();
					Object.Data.SourceVoltages.Clear();
					Object.Data.CurrentInWire = new float ();
					Object.Data.ActualVoltage = new float ();
					Object.Data.EstimatedResistance = new float ();
					Object.Data.UpstreamCount = new int ();
					Object.Data.DownstreamCount = new int ();
					Object.Data.ResistanceToConnectedDevices.Clear();
					Object.connectedDevices.Clear();
				}
			} else {
				int InstanceID = SourceInstance.GetInstanceID ();
				if (Object.Data.Downstream.ContainsKey (InstanceID)) {
					Object.Data.Downstream.Remove (InstanceID);
					if (Object.Data.FirstPresent == InstanceID) {
						Object.Data.FirstPresent = new int ();
					}
					foreach (IElectricityIO JumpTo in Object.Data.connections) {
						JumpTo.RemoveSupply (SourceInstance);
					}
					if (InstanceID == Object.GameObject ().GetInstanceID ()) {
						CleanConnectedDevicesFromPower (Object);
						Object.Data.ResistanceToConnectedDevices.Clear();
					}
					Object.Data.Upstream.Remove (InstanceID);
					Object.Data.SourceVoltages.Remove (InstanceID); 
					Object.Data.ResistanceGoingTo.Remove (InstanceID);
					Object.Data.ResistanceComingFrom.Remove (InstanceID);
					Object.Data.CurrentGoingTo.Remove (InstanceID);
					Object.Data.CurrentComingFrom.Remove (InstanceID);
					Object.Data.SourceVoltages.Remove (InstanceID);
					Object.Data.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(Object);;
					Object.Data.CurrentInWire = Object.Data.ActualCurrentChargeInWire.Current;
					Object.Data.ActualVoltage = Object.Data.ActualCurrentChargeInWire.Voltage;
					Object.Data.EstimatedResistance = Object.Data.ActualCurrentChargeInWire.EstimatedResistant;
					Object.Data.UpstreamCount = new int ();
					Object.Data.DownstreamCount = new int ();
				}
			}

		}
	}
}
