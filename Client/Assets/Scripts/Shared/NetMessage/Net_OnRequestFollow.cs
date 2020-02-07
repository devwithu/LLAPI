
using System.Collections.Generic;

[System.Serializable]
public class Net_OnRequestFollow : NetMsg
{
     public Net_OnRequestFollow() {
         OP = NetOP.OnRequestFollow;
     }

     //public string Token {set;get;}

     public List<Account> Follows {get; set;}

     

}
