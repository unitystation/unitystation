using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

public static class ElectricityFunctions  {

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
	public static bool CalculateDirectionBool(IElectricityIO From, IElectricityIO To,bool Upstream ){
		bool isTrue = false;
		int UesID = From.FirstPresent;
		if (Upstream) {
			if (From.Upstream [UesID].Contains (To)) {
				isTrue = true;
				return(isTrue);
			} else {
				return(isTrue);
			}
		} else {
			if (From.Downstream[UesID].Contains(To)){
				isTrue = true;
				return(isTrue);
			} else {
				return(isTrue);
			}
		}

	}
	//CalculateDirectionFromID
	public static bool CalculateDirectionFromID(IElectricityIO On, int TheID ){
		bool isTrue = false;
		if (!(On.ResistanceComingFrom.ContainsKey (TheID))) {
			return(true);
		}
		if (!(On.ResistanceComingFrom.ContainsKey (On.FirstPresent))) {
			return(true);
		}
		isTrue = true;
		foreach (KeyValuePair<IElectricityIO,float>  CurrentItem in On.ResistanceComingFrom [On.FirstPresent]) { 

			if (!(On.ResistanceComingFrom [TheID].ContainsKey (CurrentItem.Key))) {
				isTrue = false;
			}
		}

		//if (On.ResistanceComingFrom [TheID] == On.ResistanceComingFrom [On.FirstPresent]) {
		//isTrue = true;
		//}
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
	public static Electricity WorkOutActualNumbers(IElectricityIO ElectricItem){ //add Resistance to SMso they get properly worked out
		float Current = 0;
		float Voltage = 0;
		Dictionary<IElectricityIO,float> AnInterestingDictionary = new Dictionary<IElectricityIO, float> ();
		foreach (KeyValuePair<int, float> CurrentIDItem in ElectricItem.SourceVoltages) { 
			Voltage += CurrentIDItem.Value;
			//Logger.Log ("yeah no" + Voltage.ToString ());
		}
		//Logger.Log("yeah no Number > " + ElectricItem.SourceVoltages.Count.ToString ());


		foreach (KeyValuePair<int, Dictionary<IElectricityIO,float>> CurrentIDItem in ElectricItem.CurrentComingFrom) { 
			foreach (KeyValuePair<IElectricityIO,float> CurrentItem in CurrentIDItem.Value) {
				if (AnInterestingDictionary.ContainsKey (CurrentItem.Key)) {
					AnInterestingDictionary [CurrentItem.Key] += CurrentItem.Value;
				} else {
					AnInterestingDictionary [CurrentItem.Key] = CurrentItem.Value;
				}
			}
			if (ElectricItem.CurrentGoingTo.ContainsKey (CurrentIDItem.Key)) {
				foreach (KeyValuePair<IElectricityIO,float> CurrentItem in ElectricItem.CurrentGoingTo[CurrentIDItem.Key]) {
					if (AnInterestingDictionary.ContainsKey (CurrentItem.Key)) {
						AnInterestingDictionary [CurrentItem.Key] += -CurrentItem.Value;
					} else {
						AnInterestingDictionary [CurrentItem.Key] = -CurrentItem.Value;
					}
				}


			}
		}
		//		foreach (KeyValuePair<IElectricityIO,float> CurrentItem in AnInterestingDictionary) {
		//			Logger.Log (CurrentItem.Key.ToString () + " <key yeah Value> " + CurrentItem.Value.ToString () + " THSo>  " + ElectricItem.GameObject().name.ToString(), Category.Electrical );
		//		}
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
			//			Logger.Log (R2.ToString () + " Reasonable?", Category.Electrical);
			if (!(TransformInformation.VoltageLimiting == 0)){ //if Total Voltage greater than that then  Push some of it to ground  to == VoltageLimitedTo And then everything after it to ground/
				float ActualVoltage = ActualCurrent * ResistanceModified;

				float SUBV1 = ActualVoltage;
				float SUBR1 = ResistanceModified;
				float SUBI1 = ActualCurrent;


				float SUBV2 = SUBV1/Turn_ratio;
				float SUBI2 = (SUBV1 / SUBV2) * SUBI1;
				float SUBR2 = SUBV2 / SUBI2;


				//				Logger.Log (SUBI2.ToString () + " Current C", Category.Electrical);
				//				Logger.Log (SUBR2.ToString () + " Resistance C", Category.Electrical);
				//
				//				Logger.Log (SUBV2.ToString () + " Voltage C", Category.Electrical);
				//				Logger.Log ((V2 + SUBV2) .ToString () + " V2 + SUBV2 ", Category.Electrical);
				if ((V2 + SUBV2) > TransformInformation.VoltageLimiting) { 
					offcut = ((V2 + SUBV2) - TransformInformation.VoltageLimitedTo)/ R2;
					V2 = TransformInformation.VoltageLimitedTo - SUBV2;
					if (V2 < 0) {
						V2 = 0;
					}
				}
			}
			float I2 = V2/R2;
			//			Logger.Log (I2.ToString() + "Outputting >", Category.Electrical );
			Tuple<float,float> returns = new Tuple<float, float>(
				I2, 
				offcut
			);
			return(returns);
		}
		//Logger.LogWarning ("what HELP!!!! TransformerCalculations", Category.Electrical); Doesn't matter too much for now but can indicate places to optimise
		Tuple<float,float> returnsE = new Tuple<float, float>(0.0f, 0);
		return(returnsE);
	}









	public static void ElectricityOutput(int tick, float Current, GameObject SourceInstance,IElectricityIO Thiswire ){
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		float SimplyTimesBy = 0; 
		float SupplyingCurrent = 0; 

		Dictionary<IElectricityIO,float> ThiswireResistance = new Dictionary<IElectricityIO, float>();
		if (Thiswire.ResistanceComingFrom.ContainsKey(SourceInstanceID)){
			ThiswireResistance = Thiswire.ResistanceComingFrom[SourceInstanceID];
			//Logger.Log ("It does", Category.Electrical);

		} else {
			Logger.LogError ("now It doesn't" + SourceInstanceID.ToString() + " with this "+ Thiswire.GameObject().name.ToString(), Category.Electrical);

		}

		float Voltage = Current*(ElectricityFunctions.WorkOutResistance (ThiswireResistance));

		if (Thiswire.ResistanceComingFrom[SourceInstanceID].Count > 1) {
			float CurrentAll = 0; 
			foreach (KeyValuePair<IElectricityIO,float> JumpTo in Thiswire.ResistanceComingFrom[SourceInstanceID]) {
				if (!(SourceInstance == Thiswire.GameObject())) {
					CurrentAll += Voltage /JumpTo.Value;
				}

			}
			SimplyTimesBy = Current / CurrentAll;
		}
		foreach (KeyValuePair<IElectricityIO,float> JumpTo in Thiswire.ResistanceComingFrom[SourceInstanceID]) {
			if (JumpTo.Key.Categorytype != PowerTypeCategory.DeadEndConnection) { 
				//bool DirectionBool = ElectricityFunctions.CalculateDirectionBool (Thiswire, JumpTo.Key);
				if (SimplyTimesBy > 0) {
					SupplyingCurrent = SimplyTimesBy * (Voltage / ElectricityFunctions.WorkOutResistance (JumpTo.Key.ResistanceComingFrom [SourceInstanceID]));
				} else {
					SupplyingCurrent = Current;
				}
				if (!(Thiswire.CurrentGoingTo.ContainsKey (SourceInstanceID))) {
					Thiswire.CurrentGoingTo [SourceInstanceID] = new Dictionary<IElectricityIO, float> ();
				}
				Thiswire.CurrentGoingTo [SourceInstanceID] [JumpTo.Key] = SupplyingCurrent;

				JumpTo.Key.ElectricityInput (tick, SupplyingCurrent, SourceInstance, Thiswire);
			}
		}
		//Thiswire.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers (Thiswire); //Maybe something akin tos
	}


	public static void ElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom,IElectricityIO Thiswire ){ 
		//Logger.Log (tick.ToString () + " <tick " + Current.ToString () + " <Current " + SourceInstance.ToString () + " <SourceInstance " + ComingFrom.ToString () + " <ComingFrom " + Thiswire.ToString () + " <Thiswire ", Category.Electrical);
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		//if (electricity.voltage > 1f) {
		//	connected = true;
		//}
		if (!(Thiswire.SourceVoltages.ContainsKey(SourceInstanceID))){
			Thiswire.SourceVoltages [SourceInstanceID] = new float ();
		} 

		if (!(Thiswire.CurrentComingFrom.ContainsKey(SourceInstanceID))){
			Thiswire.CurrentComingFrom [SourceInstanceID] = new Dictionary<IElectricityIO, float> ();
		}
		Thiswire.CurrentComingFrom [SourceInstanceID] [ComingFrom] = Current;
		//Logger.Log (Current.ToString () + " <Current Using Resistance> " + WorkOutResistance (Thiswire.ResistanceComingFrom [SourceInstanceID]).ToString (), Category.Electrical);
		Thiswire.SourceVoltages[SourceInstanceID] = Current * (WorkOutResistance (Thiswire.ResistanceComingFrom [SourceInstanceID]));
		//Logger.Log (Thiswire.ActualCurrentChargeInWire.ToString () + " How much current", Category.Electrical);
		//Pass the charge on:

		if (Thiswire.CanProvideResistance) {
			IProvidePower SourceInstancPowerSupply = SourceInstance.GetComponent<IProvidePower> ();

			if (SourceInstancPowerSupply != null){
				IElectricityIO IElectricityIOPowerSupply = SourceInstance.GetComponent<IElectricityIO> ();
				Thiswire.ResistanceToConnectedDevices.Add (IElectricityIOPowerSupply);
				SourceInstancPowerSupply.connectedDevices.Add (Thiswire);
			}
		}

		Thiswire.ElectricityOutput(tick, ElectricityFunctions.WorkOutCurrent(Thiswire.CurrentComingFrom [SourceInstanceID]),SourceInstance);

	}



	public static void ResistancyOutput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO Thiswire){
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		//VisibleResistance = Resistance;
		float ResistanceSplit = 0;
		//Logger.Log (Thiswire.Categorytype.ToString() + " <YEAHAHHAH");
		if (Thiswire.Upstream[SourceInstanceID].Count > 1) {
			float CalculatedCurrent = 1000 / Resistance;
			float CurrentSplit = CalculatedCurrent / (Thiswire.Upstream[SourceInstanceID].Count);
			ResistanceSplit = 1000 / CurrentSplit;
		} else {
			ResistanceSplit = Resistance;
		}
		foreach (IElectricityIO JumpTo in Thiswire.Upstream[SourceInstanceID]) {
			//Logger.Log (JumpTo.Categorytype.ToString ());
			if (JumpTo.Categorytype != PowerTypeCategory.DeadEndConnection) {
				Thiswire.ResistanceGoingTo [SourceInstanceID] [JumpTo] = ResistanceSplit;
				JumpTo.ResistanceInput (tick, ResistanceSplit, SourceInstance, Thiswire);
			} 

		}
	}



	public static void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO Thiswire ){
		if (ComingFrom == null){
			Resistance = Thiswire.PassedDownResistance;
			ComingFrom = ElectricalSynchronisation.DeadEnd;
			//Logger.Log (ComingFrom.ToString () + " ComingFrom ");
		}

		int SourceInstanceID = SourceInstance.GetInstanceID();

		if (!(Thiswire.ResistanceComingFrom.ContainsKey (SourceInstanceID))) {
			Thiswire.ResistanceComingFrom [SourceInstanceID] = new Dictionary<IElectricityIO,float> ();
		}
		if (!(Thiswire.ResistanceGoingTo.ContainsKey (SourceInstanceID))) {
			Thiswire.ResistanceGoingTo [SourceInstanceID] = new Dictionary<IElectricityIO,float> ();
		}
		//Logger.Log (Resistance.ToString () + " Resistance 3w");
		//if (ComingFrom != null) {

		Thiswire.ResistanceComingFrom [SourceInstanceID] [ComingFrom] = Resistance;
		//}

		Thiswire.ResistancyOutput (tick, ElectricityFunctions.WorkOutResistance (Thiswire.ResistanceComingFrom [SourceInstanceID]), SourceInstance);
		//		} else {
		//			Logger.Log ("Is it this  ");
		//		}



	}






	public static void DirectionOutput(int tick, GameObject SourceInstance, IElectricityIO Thiswire) {
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		if (!(Thiswire.Upstream.ContainsKey (SourceInstanceID))) {
			Thiswire.Upstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
		}
		if (!(Thiswire.Downstream.ContainsKey (SourceInstanceID))) {
			Thiswire.Downstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
		}
		if (Thiswire.connections.Count <= 0) {
			Thiswire.FindPossibleConnections ();
		}
		for (int i = 0; i < Thiswire.connections.Count; i++) {
			if (!(Thiswire.Upstream [SourceInstanceID].Contains (Thiswire.connections [i])) && (!(Thiswire == Thiswire.connections [i]))) {

				if (!(Thiswire.Downstream.ContainsKey (SourceInstanceID))) {
					Thiswire.Downstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
				}
				if (!(Thiswire.Downstream[SourceInstanceID].Contains (Thiswire.connections [i]))) {
					Thiswire.Downstream [SourceInstanceID].Add (Thiswire.connections [i]);

					Thiswire.connections [i].DirectionInput (tick, SourceInstance, Thiswire);
				}


			} 
		}
	}




	public static void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO Thiswire){
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		if (Thiswire.FirstPresent == 0) {
			//Logger.Log ("to It's been claimed", Category.Electrical);
			Thiswire.FirstPresent = SourceInstanceID;
			//Thiswire.FirstPresentInspector = SourceInstanceID;
		}
		if (!(Thiswire.Upstream.ContainsKey (SourceInstanceID))) {
			Thiswire.Upstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
		}
		if (!(Thiswire.Downstream.ContainsKey (SourceInstanceID))) {
			Thiswire.Downstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
		}
		if (ComingFrom != null) {
			Thiswire.Upstream [SourceInstanceID].Add(ComingFrom);
		}


		SourceInstance.GetComponent<IProvidePower> ().DirectionWorkOnNextList.Add (Thiswire);

		//Thiswire.DirectionOutput(tick, SourceInstance);
	} 


	public static void CleanConnectedDevices(IElectricityIO Thiswire){
		Logger.Log ("Cleaning it out");
		foreach (IElectricityIO IsConnectedTo in Thiswire.ResistanceToConnectedDevices) {
			IsConnectedTo.connectedDevices.Remove (Thiswire);
		}
		Thiswire.ResistanceToConnectedDevices.Clear();
	}

	public static void CleanConnectedDevicesFromPower(IElectricityIO Thiswire){
		Logger.Log ("Cleaning it out");
		foreach (IElectricityIO IsConnectedTo in Thiswire.connectedDevices) {
			IsConnectedTo.ResistanceToConnectedDevices.Remove (Thiswire);
		}
		//Thiswire.ResistanceToConnectedDevices.Clear();
		Thiswire.connectedDevices.Clear();
	}

	public static class PowerSupplies{
		public static void FlushConnectionAndUp (IElectricityIO Object){
			if (Object.connections.Count > 0) {
				List<IElectricityIO> Backupconnections = Object.connections;
				Object.connections.Clear();
				foreach (IElectricityIO JumpTo in Backupconnections) {
					JumpTo.FlushConnectionAndUp ();
				}
				//Needs prompt to regenerate connections
				Object.Upstream.Clear();
				Object.Downstream.Clear();
				Object.ResistanceComingFrom.Clear();
				Object.ResistanceGoingTo.Clear();
				Object.CurrentGoingTo.Clear();
				Object.CurrentComingFrom.Clear();
				Object.SourceVoltages = new Dictionary<int, float> ();
				Object.CurrentInWire = new float ();
				Object.ActualVoltage = new float ();
				Object.ResistanceToConnectedDevices.Clear();
				Object.connectedDevices.Clear();
			}

		}

		public static void FlushResistanceAndUp (IElectricityIO Object,  GameObject SourceInstance = null  ){
			if (SourceInstance == null) {
				if (Object.ResistanceComingFrom.Count > 0) {
					Object.ResistanceComingFrom.Clear ();
					foreach (IElectricityIO JumpTo in Object.connections) {
						JumpTo.FlushResistanceAndUp ();
					}

					Object.ResistanceGoingTo.Clear ();
					Object.CurrentGoingTo.Clear ();
					Object.CurrentComingFrom.Clear ();
					Object.SourceVoltages.Clear ();
					Object.CurrentInWire = new float ();
					Object.ActualVoltage = new float ();
				}

			} else {
				//Logger.Log ("FlushResistanceAndUp");
				int InstanceID = SourceInstance.GetInstanceID ();
				if (Object.ResistanceComingFrom.ContainsKey (InstanceID) || Object.ResistanceGoingTo.ContainsKey (InstanceID)) {
					//if (Object.ResistanceComingFrom [InstanceID].Count > 0) {
					Object.ResistanceComingFrom.Remove (InstanceID);
					Object.ResistanceGoingTo.Remove (InstanceID);
					foreach (IElectricityIO JumpTo in Object.connections) {
						JumpTo.FlushResistanceAndUp (SourceInstance);
					}
					Object.CurrentGoingTo.Remove (InstanceID);
					Object.CurrentComingFrom.Remove (InstanceID);
					Object.SourceVoltages.Remove (InstanceID);
					Object.CurrentInWire = new float ();
					Object.ActualVoltage = new float ();

				}
			}
		}

		public static void FlushSupplyAndUp (IElectricityIO Object,GameObject SourceInstance = null ){
			if (SourceInstance == null) {
				if (Object.CurrentComingFrom.Count > 0) {
					//Logger.Log ("Flushing!!", Category.Electrical);
					Object.CurrentComingFrom.Clear();
					foreach (IElectricityIO JumpTo in Object.connections) {
						JumpTo.FlushSupplyAndUp();
					}
					Object.CurrentGoingTo.Clear();

					Object.SourceVoltages.Clear();
					Object.CurrentInWire = new float ();
					Object.ActualVoltage = new float ();
				}

			} else {
				int InstanceID = SourceInstance.GetInstanceID ();
				if (Object.CurrentComingFrom.ContainsKey (InstanceID)) {
					//Logger.Log ("Flushing!!", Category.Electrical);
					Object.CurrentGoingTo.Remove (InstanceID);
					Object.CurrentComingFrom.Remove (InstanceID);
					foreach (IElectricityIO JumpTo in Object.connections) {
						JumpTo.FlushSupplyAndUp (SourceInstance);
					}


				} else if (Object.CurrentGoingTo.ContainsKey (InstanceID)) {
					//Logger.Log ("Flushing!!", Category.Electrical);
					Object.CurrentGoingTo.Remove (InstanceID);
					Object.CurrentComingFrom.Remove (InstanceID);
					foreach (IElectricityIO JumpTo in Object.connections) {
						JumpTo.FlushSupplyAndUp (SourceInstance);
					}
				}
				Object.CurrentGoingTo.Remove (InstanceID);
				Object.SourceVoltages.Remove (InstanceID);

				Object.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers (Object);

				Object.CurrentInWire = Object.ActualCurrentChargeInWire.Current;
				Object.ActualVoltage = Object.ActualCurrentChargeInWire.Voltage;
				Object.EstimatedResistance = Object.ActualCurrentChargeInWire.EstimatedResistant;
			}
		}


		public static void RemoveSupply(IElectricityIO Object,GameObject SourceInstance = null ){		
			if (SourceInstance == null) {
				if (Object.Downstream.Count > 0 || Object.Upstream.Count > 0) {
					Object.Downstream.Clear();
					Object.Upstream.Clear();
					Object.FirstPresent = new int ();
					foreach (IElectricityIO JumpTo in Object.connections) {
						JumpTo.RemoveSupply ();
					}
					Object.Upstream.Clear();
					Object.SourceVoltages.Clear();
					Object.ResistanceGoingTo.Clear();
					Object.ResistanceComingFrom.Clear();
					Object.CurrentGoingTo.Clear();
					Object.CurrentComingFrom.Clear();
					Object.SourceVoltages.Clear();

					Object.CurrentInWire = new float ();
					Object.ActualVoltage = new float ();
					Object.EstimatedResistance = new float ();

					//VisibleResistance = new float ();
					Object.UpstreamCount = new int ();
					Object.DownstreamCount = new int ();
					Object.ResistanceToConnectedDevices.Clear();
					Object.connectedDevices.Clear();
				}
			} else {
				int InstanceID = SourceInstance.GetInstanceID ();
				if (Object.Downstream.ContainsKey (InstanceID)) {
					Object.Downstream.Remove (InstanceID);
					if (Object.FirstPresent == InstanceID) {
						Object.FirstPresent = new int ();
					}
					foreach (IElectricityIO JumpTo in Object.connections) {
						JumpTo.RemoveSupply (SourceInstance);
					}
					if (InstanceID == Object.GameObject ().GetInstanceID ()) {
						CleanConnectedDevicesFromPower (Object);
						Object.ResistanceToConnectedDevices.Clear();
						//Object.connectedDevices.Clear();
					}
					Object.Upstream.Remove (InstanceID);
					Object.SourceVoltages.Remove (InstanceID); 
					Object.ResistanceGoingTo.Remove (InstanceID);
					Object.ResistanceComingFrom.Remove (InstanceID);
					Object.CurrentGoingTo.Remove (InstanceID);
					Object.CurrentComingFrom.Remove (InstanceID);
					//Logger.Log (SourceVoltages.Count.ToString() + "yes");
					Object.SourceVoltages.Remove (InstanceID);

					Object.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(Object);
					//				foreach (KeyValuePair<int, Dictionary<IElectricityIO,float>> CurrentIDItem in CurrentComingFrom) { 
					//					Logger.Log (CurrentIDItem.Key.ToString() + " CurrentComingFrom sdd > " + InstanceID.ToString()  );
					//				}
					//				foreach (KeyValuePair<int, Dictionary<IElectricityIO,float>> CurrentIDItem in CurrentGoingTo) { 
					//					Logger.Log (CurrentIDItem.Key.ToString() + " CurrentGoingTo sdd > " + InstanceID.ToString() );
					//					//foreach (KeyValuePair<IElectricityIO,float> CurrentItem in CurrentIDItem.Value) {
					//					//}
					//
					//				}

					//Logger.Log(ActualCurrentChargeInWire.Current.ToString()+ "yoyoyoyoyyoyoyooy");
					Object.CurrentInWire = Object.ActualCurrentChargeInWire.Current;
					Object.ActualVoltage = Object.ActualCurrentChargeInWire.Voltage;
					Object.EstimatedResistance = Object.ActualCurrentChargeInWire.EstimatedResistant;

					//VisibleResistance = new float ();
					Object.UpstreamCount = new int ();
					Object.DownstreamCount = new int ();
				}
			}

		}
	}
}
