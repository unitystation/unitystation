﻿using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using UI;

//Handles control and spawn of player prefab
namespace PlayGroup
{
    public class PlayerManager: MonoBehaviour
    {
        public static GameObject LocalPlayer { get; private set; }

		public static Equipment.Equipment Equipment { get; private set; }

        public static PlayerScript LocalPlayerScript { get; private set; }
        
        //For access via other parts of the game
        public static PlayerScript PlayerScript { get; private set; }

        public static bool HasSpawned { get; private set; }

        private static PlayerManager playerManager;

        public static PlayerManager Instance
        {
            get
            {
                if (!playerManager)
                {
                    playerManager = FindObjectOfType<PlayerManager>();
                }

                return playerManager;
            }
        }
           
        public static void Reset()
        {
            HasSpawned = false;
        }

        public static void SetPlayerForControl(GameObject playerObjToControl)
        {
            LocalPlayer = playerObjToControl;
            LocalPlayerScript = playerObjToControl.GetComponent<PlayerScript>();
	
            PlayerScript = LocalPlayerScript; // Set this on the manager so it can be accessed by other components/managers
            Camera2DFollow.followControl.target = LocalPlayer.transform;

			HasSpawned = true;
        }

        public static bool PlayerInReach(Transform transform)
        {
            if (PlayerScript != null)
            {
                return PlayerScript.IsInReach(transform);
            }
            else
            {
                return false;
            }
        }
    }
}
