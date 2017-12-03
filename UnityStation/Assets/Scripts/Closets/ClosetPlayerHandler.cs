using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cupboards
{
	//TODO make into a more generic component to handle coffins, disposal bins etc. Will
  	//require a rename also
	/// <summary>
	/// A temporary component added to the players localplayer 
	/// by ObjectBehaviour.cs when they are hidden in a cupboard. 
	/// It is also removed by ObjectBehaviour when they leave.
	/// It will wait for directional key inputs and then check 
	/// if the player can leave.
	/// </summary>
	public class ClosetPlayerHandler : MonoBehaviour
	{
		private ClosetControl closetControl;
		private bool monitor = false;

		void Start()
		{
			//Closets have healthbehaviours on them, search through the list for the cupboard you are in
			List<HealthBehaviour> getDamageables = Matrix.Matrix.At(transform.position).GetDamageables();
			for (int i = 0; i < getDamageables.Count; i++){
				ClosetControl tempControl = getDamageables[i].gameObject.GetComponent<ClosetControl>();
				if(tempControl != null){
					closetControl = tempControl;
					//Set the camera to follow the closet
					Camera2DFollow.followControl.target = closetControl.transform;
					Camera2DFollow.followControl.damping = 0.2f;
					break;
				}
			}

			if(!closetControl){
				//this is not a closet. Could be a coffin or disposals
				Debug.LogWarning("No closet found for ClosetPlayerHandler!" +
				                 " maybe it's time to update this component? (see the todo's)");
				Destroy(this);
			} else{
				monitor = true;
			}
		}

		void Update(){
			if(monitor){
				if(CheckForDirectionalKeyPress()){
					if(!closetControl.IsLocked){
						closetControl.Interact(gameObject, "lefthand");
					}
				}
				//take the player with the closet so they can interact with it
				if (closetControl.transform.position != transform.position){
					transform.position = closetControl.transform.position;

				}
			}
		}

		private bool CheckForDirectionalKeyPress(){
			if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.D)
			  || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A)
			   || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow)
			   || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow)){
				return true;
			}
			return false;
		}
	}
}