using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class GUI_PlayerJobs : MonoBehaviour
{
    public Text title;
    public GameObject screen_Jobs;
    public GameObject buttonPrefab;
    private CustomNetworkManager networkManager;

    private bool isInit = false;

    void Update()
    {
        //We only want the job selection screen to show up once
        //And only when we've received the connectedPlayers list from the server
        if (canBeInit())
        {
            Init();
        }
    }

    public void BtnOk(JobType preference)
    {
        SoundManager.Play("Click01");
        PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestJob(preference);
        UIManager.Instance.GetComponent<ControlDisplays>().jobSelectWindow.SetActive(false);
    }

    private void Init()
    {
        screen_Jobs.SetActive(false);
        foreach (Transform child in screen_Jobs.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        foreach (GameObject occupationGo in GameManager.Instance.Occupations)
        {
            GameObject occupation = Instantiate(buttonPrefab);
            JobType jobType = occupationGo.GetComponent<OccupationRoster>().Type;
            int active = GameManager.Instance.GetOccupationsCount(jobType);
            int available = GameManager.Instance.GetOccupationMaxCount(jobType);


            occupation.GetComponentInChildren<Text>().text = jobType + " (" + active + " of " + available + ")";
            occupation.transform.SetParent(screen_Jobs.transform);
            occupation.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            //Disabled button for full jobs
            if (active >= available)
            {
                occupation.GetComponentInChildren<Button>().interactable = false;
            }
            else //Enabled button with listener for vacant jobs
            {
                occupation.GetComponent<Button>().onClick.AddListener(() => { this.BtnOk(jobType); });
            }

            occupation.SetActive(true);
        }
        screen_Jobs.SetActive(true);
        isInit = true;
    }

    private bool canBeInit()
    {
        if (isInit)
        {
            return false;
        }

        //nameList is a syncvar with instant sync on join, while connectedPlayers only happens after player has been spawned
        //We should not show job selection if we haven't received all the connectedPlayers GOs
        if (PlayerList.Instance.nameList.Count != PlayerList.Instance.connectedPlayers.Count)
        {
            return false;
        }

        return true;
    }
}