using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Mongo
{
     //private const string MONGO_URI = "mongodb+srv://daejin4u:daejin4u@cluster0-zvmuy.mongodb.net/test?retryWrites=true&w=majority";

     private const string MONGO_URI = "mongodb://127.0.0.1:27017/admin";
     private const string DATABASE_NAME = "unity";

     private MongoClient client;
     private MongoServer server;
     private MongoDatabase db;

    private MongoCollection<Model_Account> accounts;
    private MongoCollection<Model_Follow> follows;
     
    

    public void Init() {
        client = new MongoClient(MONGO_URI);
        server = client.GetServer();
        db = server.GetDatabase(DATABASE_NAME);
        //Mongod

        // This is where we wooud initize collections

        accounts = db.GetCollection<Model_Account>("account");
        follows = db.GetCollection<Model_Follow>("follow");




        Debug.Log("database has been initialized");
    }
    public void Shutdown() {
        client = null;
        server.Shutdown();
        db = null;
        
    }

    public bool InsertFollow(string token, string emailOrUsername){

        Model_Follow newFollow = new Model_Follow();
        newFollow.Sender = new MongoDBRef("account", FindAccountByToken(token)._id );

        if(!Utility.IsEmail(emailOrUsername)) {
            string[] data = emailOrUsername.Split('#');
            if(data[1] != null) {
                Model_Account follow = FindAccountByUsernameAndDiscriminator(data[0], data[1]);
                if(follows != null) {
                    newFollow.Target = new MongoDBRef("account", follow._id);
                } else {
                    return false;
                } 
            } 
        } else {
            Model_Account follow = FindAccountByEmail(emailOrUsername);
            if(follows != null) {
                newFollow.Target = new MongoDBRef("account", follow._id);
            } else {
                return false;
            } 
        }

        if(newFollow.Target != newFollow.Sender) {
            var query = Query.And(
                Query<Model_Follow>.EQ(u => u.Sender, newFollow.Sender),
                Query<Model_Follow>.EQ(u => u.Target, newFollow.Target));
            
            if(follows.FindOne(query) == null){
                follows.Insert(newFollow);
            }

            return true;
        }
        return false;
    }
    public bool InsertAccount(string username, string password, string email) {

        if(!Utility.IsEmail(email)) {
            Debug.Log(email + "is not a email");
            return false;
        }

        if(!Utility.IsUsername(username)) {
            Debug.Log(email + "is not a username");
            return false;
        }

        //check if the account aleady exist
        if(FindAccountByEmail(email) != null) {
            Debug.Log(email + "is already being userd");
            return false;
        }

        Model_Account newAccount = new Model_Account();
        newAccount.Username = username;
        newAccount.ShaPassword = password;
        newAccount.Email = email;
        newAccount.Discriminator = "0000";

        int rollCount = 0;
        while (FindAccountByUsernameAndDiscriminator(newAccount.Username, newAccount.Discriminator) != null) {
            newAccount.Discriminator = UnityEngine.Random.Range(0, 9999).ToString("0000");

            rollCount++;
            if (rollCount > 1000) {
                Debug.Log("We rolled to many time, suggest uesrname change");
                return false;
            }

        }
        accounts.Insert(newAccount);

        return true;
    }
    public Model_Account LoginAccount(string usernameOrEmail, string password, int cnnId, string token) {
        Model_Account myAccount = null;
        IMongoQuery query = null;

        if(Utility.IsEmail(usernameOrEmail)) {
            query = Query.And(Query<Model_Account>.EQ(u => u.Email, usernameOrEmail),
                    Query<Model_Account>.EQ(u => u.ShaPassword, password));
            myAccount = accounts.FindOne(query);
        } else {
            string[] data = usernameOrEmail.Split('#');
            if(data[1] != null) {
                query = Query.And(Query<Model_Account>.EQ(u => u.Username, data[0]),
                        Query<Model_Account>.EQ(u => u.Discriminator, data[1]),
                        Query<Model_Account>.EQ(u => u.ShaPassword, password));
                myAccount = accounts.FindOne(query);

            }
        }

        if(myAccount != null) {
            myAccount.ActiveConnection = cnnId;
            myAccount.Token = token;
            myAccount.Status = 1;
            myAccount.LastLogin = System.DateTime.Now;

            accounts.Update(query, Update<Model_Account>.Replace(myAccount));
            
        } else {


        }

        return myAccount;
    }
    
    public Model_Account FindAccountByObjectId (ObjectId id) {
        
        
        return accounts.FindOne(Query<Model_Account>.EQ(u => u._id, id));
        
    }

    public Model_Account FindAccountByEmail (string email) {
        
        
        return accounts.FindOne(Query<Model_Account>.EQ(u => u.Email, email));
        
    }

    public Model_Account FindAccountByUsernameAndDiscriminator(string uesrname, string discriminator) {
         var query = Query.And(Query<Model_Account>.EQ(u => u.Username, uesrname), Query<Model_Account>.EQ(u => u.Discriminator, discriminator));
         return accounts.FindOne(query);
    }
    public Model_Account FindAccountByToken(string token) {

        return accounts.FindOne(Query<Model_Account>.EQ(u => u.Token, token));
    }
    
    public List<Account> FindAllFollowBy(string token) {
        var self = new MongoDBRef("account", FindAccountByToken(token)._id);
        var query = Query<Model_Follow>.EQ(f => f.Sender, self);

        List<Account> followsResponse = new List<Account>();
        foreach(var f in follows.Find(query)) {
            followsResponse.Add(FindAccountByObjectId(f.Target.Id.AsObjectId).GetAccount());
        }
        return followsResponse;
    }

    public Model_Follow FindFollowByUsernameDicriminator(string token, string usernameDiscriminator) {
        string[] data = usernameDiscriminator.Split('#');
        if(data[1] != null) {
            var sender = new MongoDBRef("account", FindAccountByToken(token)._id);
            var follow = new MongoDBRef("account", FindAccountByUsernameAndDiscriminator(data[0], data[1])._id);
            var query = Query.And(
                Query<Model_Follow>.EQ(f => f.Sender, sender), 
                Query<Model_Follow>.EQ(f => f.Target, follow));
            
            return follows.FindOne(query);

             
        }
        return null;
    }
    
    public bool UpdateAccount(string username, string password, string email) {
        return true;
    }

    public bool DeleteAccount(string username, string password, string email) {
        return true;
    }
    
    public void RemoveFollow(string token, string usernameDiscriminator) {
        ObjectId id = FindFollowByUsernameDicriminator(token, usernameDiscriminator)._id;

        follows.Remove(Query<Model_Follow>.EQ(f => f._id, id));
    }
}
