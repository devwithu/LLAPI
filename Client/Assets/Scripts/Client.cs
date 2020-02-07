using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

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

    public Account self;
    private string token;

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
            
            case NetOP.OnAddFollow:
                OnAddFollow((Net_OnAddFollow)msg);
                break;
            
            case NetOP.OnRequestFollow:
                OnRequestFollow((Net_OnRequestFollow)msg);
                break;
                
            default:
                Debug.Log("Recieved a mesage of type unknown " + msg.OP);
                break;

        }
    }

    private void OnRequestFollow(Net_OnRequestFollow orf)
    {
        foreach(var follow in orf.Follows) {
            HubScene.Instance.AddFollowToUi(follow);
        }
    }

    private void OnAddFollow(Net_OnAddFollow oaf)
    {
         HubScene.Instance.AddFollowToUi(oaf.Follow);
    }

    private void OnCreateAccount(Net_OnCreateAccount oca) {
        LobbyScene.Instance.EnableInputs();
        LobbyScene.Instance.ChangeAthenticationMessage(oca.Infomation);
    }

    private void OnLoginRequest(Net_OnLoginRequest olr) {

        LobbyScene.Instance.ChangeAthenticationMessage(olr.Infomation);

        if(olr.Success != 1) {
            Debug.Log("if(olr.Success != 1) {");
            LobbyScene.Instance.EnableInputs();
        } else {
            Debug.Log("Success login");

            // Success login
            self = new Account();
            self.ActiveConnection = olr.ConnectionId;
            self.Username = olr.Username;
            self.Discriminator = olr.Discriminator;
            
            token = olr.Token;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Hub");
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
            

            if(!Utility.IsUsername(username)) {
                LobbyScene.Instance.ChangeAthenticationMessage("Username is invalid");
                LobbyScene.Instance.EnableInputs();
                return;
            }

            if(!Utility.IsEmail(email)) {
                LobbyScene.Instance.ChangeAthenticationMessage("Email is invalid");
                LobbyScene.Instance.EnableInputs();
                return;
            }

            if(password == null || password == "") {
                LobbyScene.Instance.ChangeAthenticationMessage("Password is empty");
                LobbyScene.Instance.EnableInputs();
                return;
            }

            Net_CreateAccount ca = new Net_CreateAccount();

            ca.Username = username;
            ca.Password = Utility.Sha256FromString(password) ;
            ca.Email = email;

            LobbyScene.Instance.ChangeAthenticationMessage("Sending request...");
            SendServer(ca);
    }
    
    public void SendLoginRequest(string usernameOrEmail, string password) {

            if(!Utility.IsUsernameAndDiscriminator(usernameOrEmail) && !Utility.IsEmail(usernameOrEmail)) {
                LobbyScene.Instance.ChangeAthenticationMessage("Email or Username#Discriminator is invalid");
                LobbyScene.Instance.EnableInputs();
                return;
            }

            if(password == null || password == "") {
                LobbyScene.Instance.ChangeAthenticationMessage("Password is empty");
                LobbyScene.Instance.EnableInputs();
                return;
            }


            Net_LoginRequest lr = new Net_LoginRequest();
            lr.UsernameOrEmail= usernameOrEmail;
            lr.Password = Utility.Sha256FromString(password) ;
            
            LobbyScene.Instance.ChangeAthenticationMessage("Sending login request...");
            SendServer(lr);
    }

    public void SendAddFollow(string usernameOrEmail) {

        Net_AddFollow af = new Net_AddFollow();

        af.Token = token;
        af.UsernameDiscriminatorOrEmail = usernameOrEmail;

        SendServer(af);
    }

    public void SendRemoveFollow(string username) {

        Net_RemoveFollow rf = new Net_RemoveFollow();

        rf.Token = token;
        rf.UsernameDiscriminator = username;

        SendServer(rf);
    }

    public void SendRequestFollow() {
        Net_RequestFollow rf = new Net_RequestFollow();

        rf.Token = token;

        SendServer(rf);
    }
}
