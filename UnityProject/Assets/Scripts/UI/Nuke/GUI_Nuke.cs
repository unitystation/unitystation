using UI;
using UnityEngine;
using UnityEngine.UI;

public class GUI_Nuke : MonoBehaviour
{
    //setting up the window
    public GameObject buttonPrefab;
    private static readonly Color banColor = new Color(255f / 255f, 103f / 255f, 103f / 255f);
    private static readonly Color infoColor = new Color(200f / 255f, 200f / 255f, 200f / 255f);
    public Text title;
    public InputField code;
    //what needs to happen when the button is clicked
    public void BtnOk()
    {
        SoundManager.Play("Click01");
        UIManager.Display.nukeWindow.SetActive(false);
        GameObject nuke = GameObject.Find("nuke");
        NukeInteract ni = (NukeInteract)nuke.GetComponent("NukeInteract");
        string codestring = code.text;
        //send the code to NukeInteract
        ni.validate(codestring);
    }

    public void EndEditOnEnter()
    {
        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            BtnOk();
        }
    }
    public void Show()
    {
        UIManager.Display.nukeWindow.SetActive(true);
    }

}