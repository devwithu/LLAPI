using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScene : MonoBehaviour
{
    public static LobbyScene Instance {set;get;}
    private void Start() {
        Instance = this;
    }
    public void OnClickCreateAccount() {
        DisableInputs();

        string username = GameObject.Find("CreateUsername").GetComponent<InputField>().text;
        string password = GameObject.Find("CreatePassword").GetComponent<InputField>().text;
        string email = GameObject.Find("CreateEmail").GetComponent<InputField>().text;

        Client.Instance.SendCreateAccount(username, password, email);
    }
    public void OnClickLoginRequest() {
        DisableInputs();

        string usernameOrEmail = GameObject.Find("LoginUsernameEmail").GetComponent<InputField>().text;
        string password = GameObject.Find("LoginPassword").GetComponent<InputField>().text;

        Client.Instance.SendLoginRequest(usernameOrEmail, password);
    }

    public void ChangeWelcomeMessage(string msg) {
        GameObject.Find("WelcomeMessageText").GetComponent<Text>().text = msg;    
    }

    public void ChangeAthenticationMessage(string msg) {
        GameObject.Find("AuthenticationMessageText").GetComponent<Text>().text = msg;    
    }

    public void EnableInputs() {
        GameObject.Find("Canvas").GetComponent<CanvasGroup>().interactable = true;
    }
    public void DisableInputs() {
        GameObject.Find("Canvas").GetComponent<CanvasGroup>().interactable = false;
    }
}
