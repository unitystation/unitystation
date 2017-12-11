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

    // Use this for initialization
    void Start()
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
            if (active > occupationGo.GetComponent<OccupationRoster>().limit)
                continue;

            occupation.GetComponentInChildren<Text>().text = jobType + " (" + active + ")";
            occupation.transform.SetParent(screen_Jobs.transform);
            occupation.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            occupation.GetComponent<Button>().onClick.AddListener(() => { this.BtnOk(jobType); });

            occupation.SetActive(true);
        }
        screen_Jobs.SetActive(true);
    }

    public void BtnOk(JobType preference)
    {
        SoundManager.Play("Click01");
        PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestJob(preference);
        UIManager.Instance.GetComponent<ControlDisplays>().jobSelectWindow.SetActive(false);
    }
}
