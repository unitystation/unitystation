// Booster Transport (c) Mirror-Networking.com
//
//   gameserver <-> booster <-> client
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Mirror
{
    public class BoosterTransport : Transport
    {
        // scheme used by this transport
        // "tcp4" means tcp with 4 bytes header, network byte order
        public const string Scheme = "tcp4";

        [Header("Game Client to Booster Server Connection")]
        public ushort port = 7777;

        [Header("Game Server to Booster Server Connection")]
        public ushort boosterPort = 7776;

      //  [Header("Beam file")]
      //  public TextAsset beamFile;

        public byte[] beamData;
        string beamOutput = "booster.beam";

        // client to server connection
        protected Telepathy.Client client = new Telepathy.Client();

        // server to booster connection
        protected Telepathy.Client booster = new Telepathy.Client();

        void Awake()
        {
            // tell Telepathy to use Unity's Debug.Log
            Telepathy.Logger.Log = Debug.Log;
            Telepathy.Logger.LogWarning = Debug.LogWarning;
            Telepathy.Logger.LogError = Debug.LogError;

            // configure
            booster.NoDelay = false;
            client.NoDelay = false;

            Debug.Log("Booster initialized!");
        }

        public override bool Available()
        {
            return Application.platform == RuntimePlatform.OSXEditor ||
                   Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.WindowsEditor ||
                   Application.platform == RuntimePlatform.WindowsPlayer ||
                   Application.platform == RuntimePlatform.LinuxEditor ||
                   Application.platform == RuntimePlatform.LinuxPlayer;
        }

        // client
        public override bool ClientConnected() { return client.Connected; }
        public override void ClientConnect(string address) { client.Connect(address, port); }
        public override bool ClientSend(int channelId, ArraySegment<byte> segment)
        {
            // telepathy doesn't support allocation-free sends yet.
            // previously we allocated in Mirror. now we do it here.
            byte[] data = new byte[segment.Count];
            Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);
            return client.Send(data);
        }

        bool ProcessClientMessage()
        {
            Telepathy.Message message;
            if (client.GetNextMessage(out message))
            {
                switch (message.eventType)
                {
                    // convert Telepathy EventType to TransportEvent
                    case Telepathy.EventType.Connected:
                        OnClientConnected.Invoke();
                        break;
                    case Telepathy.EventType.Data:
                        OnClientDataReceived.Invoke(new ArraySegment<byte>(message.data), Channels.DefaultReliable);
                        break;
                    case Telepathy.EventType.Disconnected:
                        OnClientDisconnected.Invoke();
                        break;
                    default:
                        // TODO:  Telepathy does not report errors at all
                        // it just disconnects,  should be fixed
                        OnClientDisconnected.Invoke();
                        break;
                }
                return true;
            }
            return false;
        }
        public override void ClientDisconnect() { client.Disconnect(); }

        public void LateUpdate()
        {
            // note: we need to check enabled in case we set it to false
            // when LateUpdate already started.
            // (https://github.com/vis2k/Mirror/pull/379)
            while (enabled && ProcessClientMessage()) { }
            while (enabled && ProcessServerMessage()) { }
        }

        // server
        void CreateBeam()
        {
            // delete old .beam file if exists
            if (File.Exists(beamOutput))
                File.Delete(beamOutput);

            // copy beam file to current folder so erlang can read it
            //Debug.Log("beamfile size: " + beamFile.bytes.Length + " bytes");
            FileStream resourceFile = new FileStream(beamOutput, FileMode.Create, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(resourceFile);
            writer.Write(beamData);
            writer.Close();
            resourceFile.Close();
        }

        bool ExecuteBeam()
        {
            // create beam file
            CreateBeam();
            Debug.Log("Booster .beam created.");

            string args = beamOutput + " " + boosterPort + " " + port;

            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                // why is 'escript' command known in osx terminal?
                // because etc/paths contains 'usr/local/bin', which contains
                // 'erl', 'erlc', 'escript' symlinks.
                // => in other words: we can use 'usr/local/bin/escript' on osx!
                string escriptPath = "/usr/local/bin/escript";

                // check if erlang is installed
                if (File.Exists(escriptPath))
                {
                    Debug.Log("Running escript: " + escriptPath + " with args: " + args);
                    Process.Start(escriptPath, args);
                    Debug.Log("Booster .beam started.");
                    return true;
                }
                else Debug.LogError("Erlang for OSX not found. Install it via 'brew install erlang'. Make sure that this file exist afterwards: " + escriptPath);
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                // 'whereis escript' on linux results in:
                //   /usr/bin/escript
                string escriptPath = "/usr/bin/escript";

                // check if erlang is installed
                if (File.Exists(escriptPath))
                {
                    Debug.Log("Running escript: " + escriptPath + " with args: " + args);
                    Process.Start(escriptPath, args);
                    Debug.Log("Booster .beam started.");
                    return true;
                }
                else Debug.LogError("Erlang for Linux not found. Install it via 'sudo apt-get install erlang'. Make sure that this file exist afterwards: " + escriptPath);
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                // on windows, erlang is not added to PATH automatically. we
                // have to assume the default install folder.
                string escriptPath = "C:\\Program Files\\erl10.2\\bin\\escript.exe";

                // check if erlang is installed
                if (File.Exists(escriptPath))
                {
                    Debug.Log("Running escript: " + escriptPath + " with args: " + args);
                    Process.Start(escriptPath, args);
                    Debug.Log("Booster .beam started.");
                    return true;
                }
                else Debug.LogError("Erlang for Windows not found. Download and install it from https://www.erlang.org/downloads. Make sure that this file exist afterwards: " + escriptPath);
            }
            else Debug.LogError("Booster: platform " + Application.platform + " not supported yet.");

            return false;
        }

        public override Uri ServerUri()
        {
            UriBuilder builder = new UriBuilder();
            builder.Scheme = Scheme;
            builder.Host = Dns.GetHostName();
            builder.Port = port;
            return builder.Uri;
        }
        public override bool ServerActive() { return booster.Connected; }
        public override void ServerStart()
        {
            // try to run beam file. only works if erlang is installed.
            if (ExecuteBeam())
            {
                // erlangen was started. wait for it to start the server.
                Thread.Sleep(1000);

                // erlangen runs on same system, so use localhost
                booster.Connect("localhost", boosterPort);
                Debug.Log("Booster: connect port=" + boosterPort);
            }
            else Debug.LogError("Booster: failed run .beam file on this platform.");
        }
        public override bool ServerSend(List<int> connectionIds, int channelId, ArraySegment<byte> segment)
        {
            // telepathy doesn't support allocation-free sends yet.
            // previously we allocated in Mirror. now we do it here.
            byte[] data = new byte[segment.Count];
            Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);

            // send to all
            bool result = true;
            foreach (int connectionId in connectionIds)
            {
                byte[] connectionIdBytes = Telepathy.Utils.IntToBytesBigEndian(connectionId);

                // <<0x03, connectionId, data>>
                byte[] message = new byte[1 + connectionIdBytes.Length + data.Length];
                message[0] = 0x03;
                Array.Copy(connectionIdBytes, 0, message, 1, connectionIdBytes.Length);
                Array.Copy(data, 0, message, 1 + connectionIdBytes.Length, data.Length);

                result &= booster.Send(message);
            }
            return result;
        }

        // bytes to int big endian from telepathy, but with offset so we don't
        // have to allocate a new array each time
        public static int BytesToIntBigEndian(byte[] bytes, int offset)
        {
            return
                (bytes[offset + 0] << 24) |
                (bytes[offset + 1] << 16) |
                (bytes[offset + 2] << 8) |
                bytes[offset + 3];
        }

        void ProcessErlangenMessage(byte[] message)
        {
            // check first byte: 1 = connected, 2 = disconnected, 3 = data
            if (message.Length == 5 && message[0] == 0x01)
            {
                // extract connectionId
                int connectionId = BytesToIntBigEndian(message, 1);
                OnServerConnected.Invoke(connectionId);
                //Debug.Log("Booster: client connected. connId=" + connectionId);
            }
            else if (message.Length == 5 && message[0] == 0x02)
            {
                // extract connectionId
                int connectionId = BytesToIntBigEndian(message, 1);
                OnServerDisconnected.Invoke(connectionId);
                //Debug.Log("Booster: client disconnected. connId=" + connectionId);
            }
            else if (message.Length >= 5 && message[0] == 0x03)
            {
                // extract connectionId
                int connectionId = BytesToIntBigEndian(message, 1);

                // create data segment
                int dataOffset = 1 + 4; // type=1, connectionid=4
                int dataLength = message.Length - dataOffset;
                ArraySegment<byte> segment = new ArraySegment<byte>(message, dataOffset, dataLength);
                OnServerDataReceived.Invoke(connectionId, segment, Channels.DefaultReliable);
                //Debug.Log("Booster: client data. connId=" + connectionId + " data=" + BitConverter.ToString(segment, segment.Offset, segment.Count));
            }
            else Debug.LogWarning("Booster: parse failed: " + BitConverter.ToString(message));
        }

        public bool ProcessServerMessage()
        {
            Telepathy.Message message;
            if (booster.GetNextMessage(out message))
            {
                switch (message.eventType)
                {
                    case Telepathy.EventType.Connected:
                        Debug.Log("Booster connected.");
                        break;
                    case Telepathy.EventType.Disconnected:
                        Debug.Log("Booster disconnected.");
                        ServerStop();
                        break;
                    case Telepathy.EventType.Data:
                        //Debug.Log("Booster data.");
                        ProcessErlangenMessage(message.data);
                        break;
                    default:
                        // TODO handle errors from Telepathy when telepathy can report errors
                        OnServerDisconnected.Invoke(message.connectionId);
                        break;
                }
                return true;
            }
            return false;
        }
        public override bool ServerDisconnect(int connectionId)
        {
            // ignore for localplayer. the game ends then anyway.
            if (connectionId > 0)
            {
                byte[] connectionIdBytes = Telepathy.Utils.IntToBytesBigEndian(connectionId);

                // <<0x02, connectionId>>
                byte[] message = new byte[1 + connectionIdBytes.Length];
                message[0] = 0x02;
                Array.Copy(connectionIdBytes, 0, message, 1, connectionIdBytes.Length);

                return booster.Send(message);
            }
            return true;
        }
        public override string ServerGetClientAddress(int connectionId) { return "UNKNOWN"; }
        public override void ServerStop() { booster.Disconnect(); }

        // common
        public override void Shutdown()
        {
            Debug.Log("Booster Shutdown()");
            client.Disconnect();
            booster.Disconnect();
        }

        public override int GetMaxPacketSize(int channelId)
        {
            // Telepathy's limit is Array.Length, which is int
            return int.MaxValue;
        }

        public override string ToString()
        {
            if (booster.Connected)
            {
                return "Booster Server connected ip: " + booster.client.Client.RemoteEndPoint;
            }
            else if (client.Connecting || client.Connected)
            {
                return "Booster Client ip: " + client.client.Client.RemoteEndPoint;
            }
            return "Booster (inactive/disconnected)";
        }
    }
}
