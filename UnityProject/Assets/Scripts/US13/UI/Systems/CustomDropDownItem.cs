using UnityEngine;
using UnityEngine.UI;
using US13.Core.Sprite_Handler;
using US13.UI.Systems.Lobby.SubCustomisation.BodyPartCustomisations;

namespace US13.UI.Systems
{

	public class CustomDropDownItem : MonoBehaviour
	{
		public GameObject Provider;

		public SpriteHandler Image;

		public Dropdown Associated;

		public Text Key;

		public void Start()
		{
//how Do I find associated
//so has name yes
//but the have to look up the data
//humm, Custom interface??
//humm
			Provider.GetComponent<DropDownCustomProvider>().CustomSetup(this, Key.text);


		}
	}


}
