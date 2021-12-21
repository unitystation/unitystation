using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry.Components;

public class MakesFootPrints : MonoBehaviour
{
	public ReagentContainer spillContents;
	private PlayerScript me;
	private Vector3Int oldPosition;
	public void Awake()
	{
		//spillContents = gameObject.GetComponent<ReagentContainer>();
		oldPosition = gameObject.AssumedWorldPosServer().RoundToInt();
	}

	// Start is called before the first frame update
	void Start()
    {
		me = gameObject.GetComponentInParent<PlayerScript>();
	}

	// Update is called once per frame
	void Update()
    {
		if (spillContents.ReagentMixTotal > 0f)
		{
			Vector3Int currentPosition = gameObject.AssumedWorldPosServer().RoundToInt();
			if(currentPosition != oldPosition && !MatrixManager.IsSpaceAt(gameObject.AssumedWorldPosServer().RoundToInt(), true))
			{
			 
				MatrixManager.ReagentReact(spillContents.TakeReagents(0.1f), gameObject.AssumedWorldPosServer().RoundToInt());
				oldPosition = currentPosition;
			}

		}
	}
}
