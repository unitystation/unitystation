using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReagentContainer : MonoBehaviour {
	public float Temperature = 20;
	public int MaxCapacity = 100;
	public float CurrentCapacity;
	public Dictionary<string, float> Contents = new Dictionary<string, float>();
	// Use this for initialization
	void Start () {
	}
	public void AddReagents (Dictionary<string, float> Reagents,float TemperatureContainer){//Automatic overflow If you Don't want  to lose check before adding
		float HowMany = AmountOfReagents (Reagents);
		CurrentCapacity = AmountOfReagents (Contents);
		if (Reagents.Count > 1) {
			double  DivideAmount =  new double ();
			DivideAmount = 1;
			if ((CurrentCapacity + HowMany) > MaxCapacity) {
				DivideAmount = MaxCapacity / (CurrentCapacity + HowMany);
				Logger.Log ("The container overflows spilling the excess");
			}
			foreach (KeyValuePair<string,float> Chemical in  Reagents) {
				float TheAmount = (float) (Chemical.Value * DivideAmount);
				if (Contents.ContainsKey (Chemical.Key)) {
					Contents [Chemical.Key] = Contents [Chemical.Key] + TheAmount;
				} else {
					Contents [Chemical.Key] = TheAmount;
				}
			}
				
		} else {
			foreach (KeyValuePair<string,float> Chemical in  Reagents) {
				float TheAmount = Chemical.Value;
				if ((CurrentCapacity + HowMany) > MaxCapacity) {
					TheAmount += (MaxCapacity - (CurrentCapacity + HowMany));
					Logger.Log ("The container overflows spilling the excess");
				} 
				if (Contents.ContainsKey (Chemical.Key)) {
					Contents [Chemical.Key] = Contents [Chemical.Key] + TheAmount;
				} else {
					Contents [Chemical.Key] = TheAmount;
				}
			}
		}
		CheckForEmptys ();
		Contents = Calculations.Reactions (Contents, Temperature);

		HowMany = ((AmountOfReagents (Contents) - CurrentCapacity) * TemperatureContainer) + (CurrentCapacity * Temperature);
		CurrentCapacity = AmountOfReagents (Contents); 
		Temperature = HowMany / CurrentCapacity; 


	}
	public void  MoveReagentsTo (int Amount, ReagentContainer To = null ){
		float Using = new float ();
		CurrentCapacity = AmountOfReagents (Contents);
		if (To !=null) {

			if ((To.CurrentCapacity + Amount) > To.MaxCapacity) {
				Using = To.MaxCapacity - To.CurrentCapacity;
			} else {
				Using = Amount;
			}
		} else {
			Using = Amount;
		}
		double DivideAmount = Using / CurrentCapacity;

		Dictionary<string, float> Transfering = new Dictionary<string, float> ();
		Dictionary<string, float> BrokenCS = new Dictionary<string, float> (Contents); 
		foreach (KeyValuePair<string,float> Chemical in  BrokenCS) {
			if ((Chemical.Value * DivideAmount) > Contents [Chemical.Key]) {
				Transfering [Chemical.Key] = Contents [Chemical.Key];
				Contents [Chemical.Key] = 0;
			} else {
				Transfering [Chemical.Key] = (float) (Chemical.Value * DivideAmount);
				Contents [Chemical.Key] = Contents [Chemical.Key] - Transfering [Chemical.Key];
			}

		} 
		if (To != null) {
			To.AddReagents (Transfering, Temperature);
		}

		CurrentCapacity = AmountOfReagents (Contents);
		CheckForEmptys ();
	}

	public float AmountOfReagents(Dictionary<string, float> Reagents){
		float Numbers = new float ();

		foreach (KeyValuePair<string,float> Chemical in  Reagents) {
			Numbers += Chemical.Value;
		}
		return(Numbers);
	}
	public void CheckForEmptys (){
		List<string> ToRemove = new List<string> ();
		foreach (KeyValuePair<string,float> Chemical in  Contents) {
			if (Chemical.Value == 0) {
				ToRemove.Add (Chemical.Key);
			}
		}
		for (int i = 0; i < ToRemove.Count; i++)
		{
			Contents.Remove (ToRemove [i]);
		} 
	} 

	[ContextMethod("Contents", "Science_flask")]
	public void logReagents(){
		foreach (KeyValuePair<string,float> Chemical in  Contents) {
			Logger.Log (Chemical.Key + " at " + Chemical.Value.ToString (), Category.Chemistry);
		}
	}
	[ContextMethod("Add to", "Pour_into")]
	public void AddTo(){
		Dictionary<string, float> Transfering = new Dictionary<string, float> ();

		Transfering ["ethanol"] = 10;
		Transfering ["toxin"] = 15;
		Transfering ["ammonia"] = 5;

		AddReagents (Transfering, 20);
	}

	[ContextMethod("Pour out", "Pour_away")]
	public void RemoveSome(){
		MoveReagentsTo (10);
	}
}
