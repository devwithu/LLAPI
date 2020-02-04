using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class Client : MonoBehaviour
{
    public static Client Instance {get; private set;}

    private const int MAX_USER = 100;
    private const int PORT = 26000;
    private const int PORT_WEB = 26001;
    private const string SERVER_IP = "127.0.0.1";
    private const int BYTE_SIZE = 1024;

    private byte reliableChannel;
    private int connectionId;
    private int hostId;
    private int webHostId;
    private byte error;

    private bool isStarted = false;

    public Text log;

     private void Start() {
         Instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }

    private void Update() {
        UpdateMessagePump();
    }
    public void Init() {
        NetworkTransport.Init();

        ConnectionConfig cc = new ConnectionConfig();
        reliableChannel = cc.AddChannel(QosType.Reliable);
        HostTopology topo = new HostTopology(cc, MAX_USER);

        // Client only
        hostId =  NetworkTransport.AddHost(topo, 0);
        //webHostId = NetworkTransport.AddWebsocketHost(topo, PORT_WEB, null);
#if UNITY_WEBGL
            //Web Client
        connectionId = NetworkTransport.Connect(hostId, SERVER_IP, PORT_WEB, 0, out error);
        Debug.Log("Connecting WebGL ");
        log.text = log.text + "\n" + "Connecting WebGL ";
#else    
            //Standalone Clinet
        connectionId = NetworkTransport.Connect(hostId, SERVER_IP, PORT, 0, out error);
        Debug.Log("Connecting Standalone ");
        log.text = log.text + "\n" + "Connecting Standalone ";
#endif    

        Debug.Log("Start Client to " + SERVER_IP);
        isStarted = true;
    }
    public void Shutdown() {
        isStarted = false;
        NetworkTransport.Shutdown();
    }

    public void UpdateMessagePump() {
        if(!isStarted)
            return;
        
        int recHostId;
        int connectionId;
        int channelId;

        byte[] recBuffer = new byte[BYTE_SIZE];
        int dataSize;

        NetworkEventType type = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, BYTE_SIZE, out dataSize, out error);
        switch(type) {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connected server");
                log.text = log.text + "\n" + "Connected server ";
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("DisConnected server");
                break;
            case NetworkEventType.DataEvent:
                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(recBuffer);
                NetMsg msg = (NetMsg)formatter.Deserialize(ms);

                OnData(connectionId, channelId, recHostId, msg);
                break;

            default:
            case NetworkEventType.BroadcastEvent:
                Debug.Log("Undexpected network event type");
                break;
        }
        

    }



        private void OnData(int cnnId, int channelId, int recHostId, NetMsg msg){
        Debug.Log("Recieved a mesage of type" + msg.OP);
        switch(msg.OP) {
            case NetOP.None:
                Debug.Log("Unexpeted NETOP");
                break;

            case NetOP.OnCreateAccount:
                OnCreateAccount((Net_OnCreateAccount)msg);
                break;

            case NetOP.OnLoginRequest:
                OnLoginRequest((Net_OnLoginRequest)msg);
                break;

        }
    }

    private void OnCreateAccount(Net_OnCreateAccount oca) {
        LobbyScene.Instance.EnableInputs();
        LobbyScene.Instance.ChangeAthenticationMessage(oca.Infomation);
    }

    private void OnLoginRequest(Net_OnLoginRequest olr) {

        LobbyScene.Instance.ChangeAthenticationMessage(olr.Infomation);

        if(olr.Success != 1) {
            LobbyScene.Instance.EnableInputs();
        } else {
            // Success login
        }

    }
    public void SendServer(NetMsg msg) {
        // This is where we hold our data
        byte[] buffer = new byte[BYTE_SIZE];

        // this is where you would crush your data into a byte[]
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buffer);
        formatter.Serialize(ms, msg);

        NetworkTransport.Send(hostId, connectionId, reliableChannel, buffer, BYTE_SIZE, out error);


    }

    public void SendCreateAccount(string username, string password, string email) {
            Net_CreateAccount ca = new Net_CreateAccount();
            ca.Username = username;
            ca.Password = password;
            ca.Email = email;

            SendServer(ca);
    }
    
    public void SendLoginRequest(string usernameOrEmail, string password) {
            Net_LoginRequest lr = new Net_LoginRequest();
            lr.UsernameOrEmail= usernameOrEmail;
            lr.Password = password;
            
            SendServer(lr);
    }
}
