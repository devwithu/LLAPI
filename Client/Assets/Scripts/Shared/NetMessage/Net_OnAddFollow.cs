
[System.Serializable]
public class Net_OnAddFollow : NetMsg
{
     public Net_OnAddFollow() {
         OP = NetOP.OnAddFollow;
     }

    //public string Token {set;get;}
    public byte Success {get;set;}
    public Account Follow {get;set;}

     

}
