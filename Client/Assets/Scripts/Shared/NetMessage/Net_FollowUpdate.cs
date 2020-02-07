
[System.Serializable]
public class Net_FollowUpdate : NetMsg
{
     public Net_FollowUpdate() {
         OP = NetOP.FollowUpdate;
     }

     public Account Follow {set; get;}
     
}
