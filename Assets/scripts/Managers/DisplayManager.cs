﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Resos:
//0: 1024x640
//1: 1280x720
//2: 1920x1080 //FIXME: Mouse Screen to world problems for 1080p
public class DisplayManager : MonoBehaviour
{

    public Dropdown optionsDropDown;
    public Light2D.LightingSystem lightingSystem;
    public Camera mainCamera;

    private int width;
    private int height;

    private bool hasInt = false;

    private void Start()
    {
        if (PlayerPrefs.HasKey("reso"))
        {
            SetResolution(PlayerPrefs.GetInt("reso"));
        }
        else
        {
            SetResolution(1);
        }
        hasInt = true;
    }
    public void SetResolution(int _value)
    {
		float xOffsetCamFollow = 0;
        switch (_value)
        {
			case 0:
				width = 1024;
				height = 640;
				xOffsetCamFollow = 1.7f;
                break;
            case 1:
                width = 1280;
                height = 720;
				xOffsetCamFollow = 2.5f;
                break;
            case 2:
                //FIXME: Interact problems at this reso
                width = 1920;
                height = 1080;
                break;
        }
        PlayerPrefs.SetInt("reso", _value);
        Screen.SetResolution(width, height, false);
		if (GameData.IsInGame) {
			Camera2DFollow.followControl.xOffset = xOffsetCamFollow;
		}
        if (optionsDropDown.value != _value)
            optionsDropDown.value = _value;
        if (lightingSystem != null)
        {

            lightingSystem._renderTargetTexture = new RenderTexture(width, height, -2, RenderTextureFormat.ARGB32);
        }
        else
        {
            lightingSystem = FindObjectOfType<Light2D.LightingSystem>();
            if (lightingSystem != null)
            {
                mainCamera = lightingSystem.GetComponent<Camera>();
                lightingSystem._renderTargetTexture = new RenderTexture(width, height, -2, RenderTextureFormat.ARGB32);
                lightingSystem._camera.targetTexture = lightingSystem._renderTargetTexture;
            }
        }
    }

    private void LateUpdate()
    {
        if (hasInt) { }
        if (Screen.width != width || Screen.height != height)
        {
            Screen.SetResolution(width, height, false);
        }
    }
}
