using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticEggLogic : MonoBehaviour
{

	public List<MutationSO> CarryingMutations;

	public int StandardHatchTime = 300;

	public int ChosenHatchTime;

	public List<GameObject> Mobs000 = new List<GameObject>();
	public List<GameObject> Mobs100 = new List<GameObject>();
	public List<GameObject> Mobs200 = new List<GameObject>();
    void Start()
    {
	    ChosenHatchTime = Mathf.RoundToInt( StandardHatchTime * Random.Range(0.5f, 1.5f));
	    StartCoroutine(countdown());

    }

    //Chasmosaurus?




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


    private IEnumerator countdown()
    {
	    yield return WaitFor.Seconds(ChosenHatchTime * 0.90f);
	    Chat.AddActionMsgToChat(this.gameObject, $"The {this.gameObject.ExpensiveName()} Wobbles", $"The {this.gameObject.ExpensiveName()} Wobbles");
	    yield return WaitFor.Seconds(ChosenHatchTime * 0.10f);

	    int difficulty = 0;

	    foreach (var mutationSo in CarryingMutations)
	    {
		    var  Settings =  BodyPartMutations.GetMutationRoundData(mutationSo);
		    difficulty += Settings.ResearchDifficult;

	    }

	    //TODO Separate prefabs for hostile and non-hostile dinosaurs
	    GameObject Dinosaur = null;


	    if (difficulty > 200)
	    {
		    Dinosaur = Spawn.ServerPrefab(Mobs200.PickRandom(), gameObject.AssumedWorldPosServer()).GameObject;
	    }
	    else if (difficulty > 100)
	    {
		    Dinosaur = Spawn.ServerPrefab(Mobs100.PickRandom(), gameObject.AssumedWorldPosServer()).GameObject;
	    }
	    else if (difficulty > 0)
	    {
		    Dinosaur = Spawn.ServerPrefab(Mobs000.PickRandom(), gameObject.AssumedWorldPosServer()).GameObject;
	    }

	    var  DLMC =  Dinosaur.GetComponent<DinosaurLivingMutationCarrier>();
	    DLMC.CarryingMutations = CarryingMutations;
	    DLMC.DifficultyLevel = difficulty;

	    _ = Despawn.ServerSingle(this.gameObject);
    }




}
