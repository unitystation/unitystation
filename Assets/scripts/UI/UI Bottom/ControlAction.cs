using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PlayGroup;


namespace UI
{
	public class ControlAction: MonoBehaviour
	{

		public Sprite[] throwSprites;
		public Image throwImage;

		void Start ()
		{
			UIManager.IsThrow = false;
		}

		/* 
		 * Button OnClick methods
		 */

		void Update ()
		{
			CheckKeyboardInput ();
		}

		void CheckKeyboardInput ()
		{
			if (Input.GetKeyDown (KeyCode.Q)) {
				Drop ();
			}
		}

		public void Resist ()
		{
			PlayClick01 ();
			Debug.Log ("Resist Button");
		}

		public void Drop ()
		{
			PlayClick01 ();
			Debug.Log ("Drop Button");
			GameObject item = UIManager.Hands.CurrentSlot.Clear ();

			if (item != null) {
                item.transform.parent = this.transform;

                BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);


				var targetPos = PlayerManager.LocalPlayer.transform.position;
				targetPos.z = -0.2f;
				item.transform.position = targetPos;
				item.transform.parent = null;
			}

		}

		public void Throw ()
		{
			PlayClick01 ();
			Debug.Log ("Throw Button");

			if (!UIManager.IsThrow) {
				UIManager.IsThrow = true;
				throwImage.sprite = throwSprites [1];

			} else {
				UIManager.IsThrow = false;
				throwImage.sprite = throwSprites [0];
			}
		}

		//SoundFX

		void PlayClick01 ()
		{
			if (SoundManager.control != null) {
				SoundManager.control.Play ("Click01");
			}
		}
	}
}