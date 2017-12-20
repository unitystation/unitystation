﻿using System.Collections;
using PlayGroup;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Toggles the active state of the object by gathering all components and setting
///     their active state. It ignores network components so item can be synced
/// </summary>
public class VisibleBehaviour : NetworkBehaviour
{
    //Ignore these types
    private const string networkId = "NetworkIdentity";

    private const string networkT = "NetworkTransform";
    private const string objectBehaviour = "ObjectBehaviour";
    private const string regTile = "RegisterTile";
    private const string inputController = "InputController";
    private const string playerSync = "PlayerSync";

    public bool isPlayer;
    public RegisterTile registerTile;

    /// <summary>
    ///     This will also set the enabled state of every component
    /// </summary>
    [SyncVar(hook = "UpdateState")] public bool visibleState = true;

    protected virtual void Awake()
    {
        registerTile = GetComponent<RegisterTile>();
    }

    public override void OnStartClient()
    {
        StartCoroutine(WaitForLoad());
        base.OnStartClient();
        var pS = GetComponent<PlayerScript>();
        if (pS != null)
        {
            isPlayer = true;
        }
    }

    private IEnumerator WaitForLoad()
    {
        yield return new WaitForSeconds(3f);
        UpdateState(visibleState);
    }

    //For ObjectBehaviour to handle specific states with the various objects like players
    public virtual void OnVisibilityChange(bool state)
    {
    }

    private void UpdateState(bool _aliveState)
    {
        OnVisibilityChange(_aliveState);

        MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>(true);
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < scripts.Length; i++)
        {
            if (scripts[i].GetType().Name != networkId && scripts[i].GetType().Name != networkT
                && scripts[i].GetType().Name != objectBehaviour
                && scripts[i].GetType().Name != regTile && scripts[i].GetType().Name
                != inputController && scripts[i].GetType().Name
                != playerSync)
            {
                scripts[i].enabled = _aliveState;
            }
        }

        for (var i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = _aliveState;
        }

        for (var i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = _aliveState;
        }

        if (registerTile != null)
        {
            if (_aliveState)
            {
                var eC = gameObject.GetComponent<EditModeControl>();
                if (eC != null)
                {
                    eC.Snap();
                }

                registerTile.UpdatePosition();
            }
            else
            {
                registerTile.Unregister();
            }
        }
    }
}