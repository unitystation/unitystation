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
			UIManager.control.isThrow = false;
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
			GameObject item = UIManager.control.hands.CurrentSlot.Clear ();

			if (item != null) {
				var targetPos = PlayerManager.control.LocalPlayer.transform.position;
				targetPos.z = -0.2f;
				item.transform.position = targetPos;
				item.transform.parent = null;
			}

		}

		public void Throw ()
		{
			PlayClick01 ();
			Debug.Log ("Throw Button");

			if (!UIManager.control.isThrow) {
				UIManager.control.isThrow = true;
				throwImage.sprite = throwSprites [1];

			} else {
				UIManager.control.isThrow = false;
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