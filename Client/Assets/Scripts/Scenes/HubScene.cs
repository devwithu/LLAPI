using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HubScene : MonoBehaviour
{
    public static HubScene Instance {set;get;}

    [SerializeField]
    private  Text selfInfomation;
    [SerializeField]
    private InputField addFollowInput;
    [SerializeField]
    private GameObject followPrefab;
    [SerializeField]
    private Transform followContainer;

    private Dictionary<string, GameObject> uiFollows = new Dictionary<string, GameObject>();


    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        selfInfomation.text = Client.Instance.self.Username + "#" + Client.Instance.self.Discriminator;
        Client.Instance.SendRequestFollow();
    }

    public void AddFollowToUi(Account follow) {
        GameObject followItem = Instantiate(followPrefab, followContainer);
        followItem.GetComponentInChildren<Text>().text = follow.Username + "#" + follow.Discriminator;
        followItem.transform.GetChild(1).GetComponentInChildren<Image>().color = (follow.Status != 0) ? Color.green : Color.gray;
        followItem.transform.GetChild(2).GetComponentInChildren<Button>().onClick.AddListener(delegate {Destroy(followItem);});
        followItem.transform.GetChild(2).GetComponentInChildren<Button>().onClick.AddListener(delegate {OnClickRemoveFollow(follow.Username, follow.Discriminator);});

        uiFollows.Add(follow.Username + "#" + follow.Discriminator, followItem);

    }

    public void UpdateFollow(Account follow) {
        
         uiFollows[follow.Username + "#" + follow.Discriminator].transform.GetChild(1).GetComponent<Image>().color = (follow.Status != 0) ? Color.green : Color.gray;
          
    }

    public void OnClickAddFollow() {
        string usernameDiscriminator = addFollowInput.text;
        if(!Utility.IsUsernameAndDiscriminator(usernameDiscriminator) && !Utility.IsEmail(usernameDiscriminator)) {
            Debug.Log("Invalid format");
            return;

        }

        Client.Instance.SendAddFollow(usernameDiscriminator);
    }

    public void OnClickRemoveFollow(string username, string discriminator) {
        
        Client.Instance.SendRemoveFollow(username + "#" + discriminator);
        uiFollows.Remove(username + "#" + discriminator);
    }


}
