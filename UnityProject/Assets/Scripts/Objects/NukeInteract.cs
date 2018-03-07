using System.Collections;
using PlayGroup;
using PlayGroups.Input;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class NukeInteract : InputTrigger
{
    public float cooldownTimer = 2f;
    public string interactionMessage;
    public string deniedMessage;
    private int nukeCode;
    private GameObject Player;
    void Start()
    {
        //calling the code generator and setting up a 10 second timer to post the code in syndicate chat
        codeGenerator();
        StartCoroutine(WaitForShit());
    }
    IEnumerator WaitForShit()
    {
        yield return new WaitForSeconds(10f);
        PostToChatMessage.Send("We have intercepted the code for the nuclear weapon: " + nukeCode, ChatChannel.Syndicate);
    }
    //Method for when a player clicks on the nuke
    public override void Interact(GameObject originator, Vector3 position, string hand)
    {
        PlayerScript ps = originator.GetComponent<PlayerScript>();
        Player = originator;
        //Determining whether or not the player is syndicate
        if (ps.JobType == JobType.SYNDICATE)
        {
            //if yes, show GUI
            UIManager.Chat.AddChatEvent(new ChatEvent(interactionMessage, ChatChannel.Examine));
            GUI_Nuke window = (GUI_Nuke)UIManager.Display.nukeWindow.GetComponent("GUI_Nuke");
            window.Show();
            
        }
        else
        {
            //if no, say bad message
            UIManager.Chat.AddChatEvent(new ChatEvent(deniedMessage, ChatChannel.Examine));
        }
        //posts nuke code for testing
        Debug.Log(nukeCode);
    }
    [Server]
    //The serverside of the interaction code
    private bool ServernIteraction(GameObject originator, Vector3 position, string hand)
    {
        PlayerScript ps = originator.GetComponent<PlayerScript>();
        if (ps.canNotInteract() || !ps.IsInReach(position))
        {
            return false;
        }

        return true;
    }
    [Server]
    //Server validating the code sent back by the GUI
    public bool validate(string code)
    {
        Debug.Log("try " + code + " on " + nukeCode);
        if (code == ""+nukeCode)
        {
            //if yes, blow up the nuke
            RpcDetonate();
            return true;
        }
        else
        {
            //if no, tell the GUI that it was an incorrect code
            return false;
        }
    }
    [ClientRpc]
    //Server telling the nukes to explode
    void RpcDetonate()
    {
        //getting health and stopping the sound
        PlayerHealth health = (PlayerHealth)Player.GetComponent<PlayerHealth>();
        SoundManager.StopAmbient();
        //turning off all the UI except for the right panel
        UIManager.Display.hudRight.gameObject.SetActive(false);
        UIManager.Display.hudBottom.gameObject.SetActive(false);
        UIManager.Display.backGround.SetActive(false);
        UIManager.Display.logInWindow.SetActive(false);
        UIManager.Display.infoWindow.SetActive(false);
        
        //Playing the video
        VideoPlayer Video = UIManager.Display.video;
        Video.Play();
        //Playing the sound
        AudioSource audio = GetComponent<AudioSource>();
        audio.Play();
        //KILLING EVERYONE!!!!!1!
        health.Death();
    }
    [Server]
    //Generating the code for the nuke
    public void codeGenerator()
    {

        nukeCode = Random.Range(1000, 9999);
    }
    

}
