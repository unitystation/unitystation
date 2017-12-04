using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DamageMonitorListener : MonoBehaviour
{
    public BodyPartType bodyPartType;
    private Image image;
    private Sprite initSprite;

    void Start()
    {
        image = GetComponent<Image>();
        initSprite = image.sprite;

    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    public void UpdateDamageSeverity(int severity)
    {

    }

    //Reset healthHUD
    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        Reset();
    }

    public void Reset()
    {
        if (image != null)
        {
            image.sprite = initSprite;
        }
    }
}
