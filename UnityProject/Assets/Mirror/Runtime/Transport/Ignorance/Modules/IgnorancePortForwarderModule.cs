// Ignorance 1.3.x
// A Unity LLAPI Replacement Transport for Mirror Networking
// https://github.com/SoftwareGuy/Ignorance
// -----------------
// Port Forwarding Module
// -----------------
using Mirror;
using System;
using UnityEngine;

namespace Mirror
{
    public class IgnorancePortForwarderModule : MonoBehaviour
    {
        protected Ignorance coreModule;

        public void Awake()
        {
            coreModule = GetComponent<Ignorance>();
            if(!coreModule)
            {
                // Can't continue without our core module.
                Debug.LogError("Ignorance Port Forwarder Module requires a Ignorance Transport script on the gameObject as the one you have this script on. I can't find it.");
                enabled = false;
                return;
            }

            coreModule.OnIgnoranceServerStartup += OnIgnoranceServerStart;
            coreModule.OnIgnoranceServerShutdown += OnIgnoranceServerShutdown;
        }

        /// <summary>
        /// Called when the core module is starting the server.
        /// </summary>
        public void OnIgnoranceServerStart()
        {
            Debug.LogError("Doing nothing - this module is not implemented.");
            // throw new NotImplementedException();
        }

        /// <summary>
        /// Called when the core module is shutting down the server.
        /// </summary>
        public void OnIgnoranceServerShutdown()
        {
            // throw new NotImplementedException();
        }

        /// <summary>
        /// The script is being disposed.
        /// </summary>
        public void OnDestroy()
        {
            coreModule.OnIgnoranceServerStartup -= OnIgnoranceServerStart;
            coreModule.OnIgnoranceServerShutdown -= OnIgnoranceServerShutdown;
        }
    }
}
