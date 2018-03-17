using UI;
using UnityEngine;
using UnityEngine.UI;
using PlayGroup;

public class GUI_Nuke : MonoBehaviour
{
    public InputField code;
	private GameObject nuke;

	public void SetNukeInteracting(GameObject theNuke){
		nuke = theNuke;
	}
    //what needs to happen when the button is clicked
    public void BtnOk()
    {
        SoundManager.Play("Click01");
		//send the code to NukeInteract
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdInputNukeCode(nuke, code.text);
		UIManager.Display.nukeWindow.SetActive(false);
    }

    public void Update()
    {
        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            BtnOk();
        }
    }
}