using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisManager : MonoBehaviour {

    public GameObject DisUi;
    public GameObject MenuButton;
    public GameObject ReconnectButton;
    public Text StatusText;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }


    private void Update()
    {
        if(Application.internetReachability == NetworkReachability.NotReachable)
        {
            DisUi.SetActive(true);

            if (Application.loadedLevelName == "MainMenu")
            {
                ReconnectButton.SetActive(true);
                StatusText.text = "Lost connection to Photon, please try to reconnect";
            }

            if (Application.loadedLevelName == "MainGame")
            {
                MenuButton.SetActive(true);
                StatusText.text = "Lost connection to Photon, please try to reconnect in the main menu";
            }
        }
    }

    //called by photon
    private void OnConnectedToMaster()
    {
        if(DisUi.active)
        {
            MenuButton.SetActive(false);
            ReconnectButton.SetActive(false);
            DisUi.SetActive(false);
        }
    }

    public void OnClick_TryConnect()
    {
        PhotonNetwork.ConnectUsingSettings(MenuManager.Instance.VersionName);
    }

    public void OnClick_Menu()
    {
        PhotonNetwork.LoadLevel("MainMenu");
    }



}
