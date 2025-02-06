using IgnoranceTransport;
using kcp2k;
using Logs;
using Mirror;
using Mirror.SimpleWeb;
using UnityEngine;

namespace Core.Networking
{
	[RequireComponent(typeof(CustomNetworkManager))]
    public class TransportSetter : MonoBehaviour
    {
	    public CustomNetworkManager networkManager;
	    public MultiplexTransport multiplexTransport;
	    public Ignorance ignoranceTransport;
	    public SimpleWebTransport webSocketTransport;
	    public KcpTransport kcpTransport;

	    public TransportType defaultTransport = TransportType.Multiplex;

        public enum TransportType
        {
            Multiplex = 1,
            KCP = 2,
            Ignorance = 3,
            WebSockets = 4
        }

        private void Awake()
        {
	        bool transportArgumentFound = false;
	        string[] args = System.Environment.GetCommandLineArgs();
	        for (int i = 0; i < args.Length; i++)
	        {
		        Debug.Log($"Argument {i}: {args[i]}");
	        }
	        for (int i = 0; i < args.Length; i++)
	        {
		        if (args[i] == "-transport" && i + 1 < args.Length)
		        {
			        if (int.TryParse(args[i + 1], out int transportValue) && transportValue is >= 1 and <= 4)
			        {
				        SetTransport((TransportType)transportValue);
				        transportArgumentFound = true;
				        break;
			        }
		        }
	        }

	        if (transportArgumentFound == false)
	        {
		        SetTransport(defaultTransport);
	        }
        }

        private void SetTransport(TransportType transportType)
        {
	        //(Max): For the future.
#if UNITY_WEBGL
	        networkManager.transport = multiplexTransport;
			Loggy.Info($"Transport set to {transportType}");
	        return;
#endif
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager is null! Transport cannot be set.");
                return;
            }

            switch (transportType)
            {
                case TransportType.Multiplex:
	                networkManager.transport = multiplexTransport;
                    break;
                case TransportType.KCP:
	                networkManager.transport = kcpTransport;
                    break;
                case TransportType.Ignorance:
	                networkManager.transport = ignoranceTransport;
                    break;
                case TransportType.WebSockets:
	                networkManager.transport = webSocketTransport;
                    break;
                default:
                    Debug.LogError("Invalid transport type.");
                    break;
            }
            Loggy.Info($"Transport set to {transportType}");
        }
    }
}