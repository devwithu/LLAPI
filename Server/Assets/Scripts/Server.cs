using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

public class Server : MonoBehaviour
{

    private const int MAX_USER = 100;
    private const int PORT = 26000;
    private const int PORT_WEB = 26001;
    private const int BYTE_SIZE = 1024;

    private byte reliableChannel;
    private int hostId;
    private int webHostId;

    private bool isStarted = false;
    private byte error;

    private Mongo db;

    private void Start() {
        DontDestroyOnLoad(gameObject);
        Init();
    }
    private void Update() {
        UpdateMessagePump();
    }
    public void Init() {

        db = new Mongo();
        db.Init();

        NetworkTransport.Init();

        ConnectionConfig cc = new ConnectionConfig();
        reliableChannel = cc.AddChannel(QosType.Reliable);
        HostTopology topo = new HostTopology(cc, MAX_USER);

        // Server only
        hostId =  NetworkTransport.AddHost(topo, PORT, null);
        webHostId = NetworkTransport.AddWebsocketHost(topo, PORT_WEB, null);
        
        Debug.Log("Start server");
        isStarted = true;

        // TEST
        //db.InsertAccount("Bam","mmm","aaa");
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
                Debug.Log(string.Format("User {0} has connected through host {1}", connectionId, recHostId));
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log(string.Format("User {0} has disConnected!", connectionId));
                break;
            case NetworkEventType.BroadcastEvent:
                Debug.Log("Undexpected network event type");
                break;
            case NetworkEventType.DataEvent:
                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(recBuffer);
                NetMsg msg = (NetMsg)formatter.Deserialize(ms);

                OnData(connectionId, channelId, recHostId, msg);

                break;
        }
        

    }

    private void OnData(int cnnId, int channelId, int recHostId, NetMsg msg){
        Debug.Log("Recieved a mesage of type" + msg.OP);
        switch(msg.OP) {
            case NetOP.None:
                Debug.Log("Unexpeted NETOP");
                break;

            case NetOP.CreateAccount:
                CreateAccount(cnnId, channelId, recHostId, (Net_CreateAccount)msg);
                break;

            case NetOP.LoginRequest:
                LoginRequest(cnnId, channelId, recHostId, (Net_LoginRequest)msg);
                break;            

            case NetOP.AddFollow:
                AddFollow(cnnId, channelId, recHostId, (Net_AddFollow)msg);
                break;

            case NetOP.RemoveFollow:
                RemoveFollow(cnnId, channelId, recHostId, (Net_RemoveFollow)msg);
                break;

            case NetOP.RequestFollow:
                //AddFollow(cnnId, channelId, recHostId, (Net_AddFollow)msg);
               
                RequestFollow(cnnId, channelId, recHostId, (Net_RequestFollow)msg);
                break;

        }
    }

    private void RequestFollow(int cnnId, int channelId, int recHostId, Net_RequestFollow msg)
    {
         Net_OnRequestFollow orf = new Net_OnRequestFollow();

        orf.Follows = db.FindAllFollowBy(msg.Token);

         SendClient(recHostId, cnnId, orf);

    }

    private void RemoveFollow(int cnnId, int channelId, int recHostId, Net_RemoveFollow msg)
    {
       db.RemoveFollow(msg.Token, msg.UsernameDiscriminator);
    }

    private void AddFollow(int cnnId, int channelId, int recHostId, Net_AddFollow msg)
    {
        Net_OnAddFollow oaf = new Net_OnAddFollow();

        if(db.InsertFollow(msg.Token, msg.UsernameDiscriminatorOrEmail)){

            oaf.Success = 1;
            
            if(Utility.IsEmail(msg.UsernameDiscriminatorOrEmail)) {

                oaf.Follow = db.FindAccountByEmail(msg.UsernameDiscriminatorOrEmail).GetAccount();
            } else {
                string[] data = msg.UsernameDiscriminatorOrEmail.Split('#');
                if(data[1] == null) {
                    return;
                }
                oaf.Follow = db.FindAccountByUsernameAndDiscriminator(data[0], data[1]).GetAccount();
            }
        }
        SendClient(recHostId,cnnId,oaf);

    }

    private void CreateAccount(int cnnId, int channelId, int recHostId, Net_CreateAccount ca) {
        Debug.Log(string.Format("{0},{1},{2}", ca.Username, ca.Password, ca.Email ) );
        
        Net_OnCreateAccount oca = new Net_OnCreateAccount();

        if(db.InsertAccount(ca.Username, ca.Password, ca.Email)) {
            oca.Success = 0;
            oca.Infomation = "Account was created";
        } else {
            oca.Success = 0;
            oca.Infomation = "Ther was an error creating the account";
        }


        SendClient(recHostId,cnnId, oca);
    }

    private void LoginRequest(int cnnId, int channelId, int recHostId, Net_LoginRequest lr) {
        Debug.Log(string.Format("{0},{1}", lr.UsernameOrEmail, lr.Password) );
        
        string randomToken = Utility.GenerateRandom(256);
        Model_Account account = db.LoginAccount(lr.UsernameOrEmail,lr.Password, cnnId, randomToken);
        Net_OnLoginRequest olr = new Net_OnLoginRequest();

        if(account != null) {
            olr.Success = 1;
            olr.Infomation = "You've been logged in as " + account.Username;
            olr.Username = account.Username;
            olr.Discriminator = account.Discriminator;
            olr.Token = randomToken;
            olr.ConnectionId = cnnId;
        } else {
            olr.Success = 0;
        }
        

        

        SendClient(recHostId,cnnId, olr);
    }

    public void SendClient(int recHost, int cnnId, NetMsg msg) {
        // This is where we hold our data
        byte[] buffer = new byte[BYTE_SIZE];

        // this is where you would crush your data into a byte[]
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buffer);
        formatter.Serialize(ms, msg);

        if(recHost == 0) {
            NetworkTransport.Send(hostId, cnnId, reliableChannel, buffer, BYTE_SIZE, out error);

        } else {
            NetworkTransport.Send(webHostId, cnnId, reliableChannel, buffer, BYTE_SIZE, out error);

        }

        

    }

}
