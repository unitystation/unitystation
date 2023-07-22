using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GeneticEggLogic : MonoBehaviour
{

	public List<MutationSO> CarryingMutations;

	public int StandardHatchTime = 300;

	public int ChosenHatchTime;

	[FormerlySerializedAs("ALL")] public List<GameObject> ALLMobs = new List<GameObject>();
    void Start()
    {
	    ChosenHatchTime = Mathf.RoundToInt( StandardHatchTime * Random.Range(0.5f, 1.5f));
	    StartCoroutine(Countdown());

    }
    //Triceratops = Friendly
	//Dimetrodon angelensis = Friendly
	//troodon  = Friendly

	//raptor = Hostile,
	//Tarbosaurus = Hostile
	//SPINOSAURUS = Hostile

	//Saichania = Neutral
	//Stegosaurus = Neutral
	//Brtachiosaurus = Neutral

    // 300
    // 200 = Tarbosaurus, Brtachiosaurus, SPINOSAURUS >
    // 100 = Stegosaurus,Triceratops, saichania >
    // 0 = raptor, Dimetrodon angelensis, troodon >

    private IEnumerator Countdown()
    {
	    yield return WaitFor.Seconds(ChosenHatchTime * 0.90f);
	    Chat.AddActionMsgToChat(this.gameObject, $"The {this.gameObject.ExpensiveName()} Wobbles", $"The {this.gameObject.ExpensiveName()} Wobbles");
	    yield return WaitFor.Seconds(ChosenHatchTime * 0.10f);

	    GameObject Dinosaur = null;

	    Dinosaur = Spawn.ServerPrefab(ALLMobs.PickRandom(), gameObject.AssumedWorldPosServer()).GameObject;

	    var  DLMC =  Dinosaur.GetComponent<DinosaurLivingMutationCarrier>();
	    DLMC.CarryingMutations = CarryingMutations;
	    _ = Despawn.ServerSingle(this.gameObject);
    }




}
