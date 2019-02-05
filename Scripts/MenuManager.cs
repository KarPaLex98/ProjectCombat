using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using PhotonHashTable = ExitGames.Client.Photon.Hashtable;

public class MenuManager : MonoBehaviour {

    public static MenuManager Instance;

    public string VersionName = "0.1";

    [SerializeField] private GameObject UserNamePanel, ConnectPanel;

    [SerializeField] private GameObject CreateUserNameButton;

    [SerializeField] private InputField UserNameInput, CreateRoomInput, JoinRoomInput;

    public Text StatusText;

    public Text NumberOfBots;

    public Slider slider;

    public Toggle DM;
    public Toggle Sur;
    public Toggle min5;
    public Toggle min10;
    public Toggle min15;




    private void Awake()
    {
        Instance = this;
        PhotonNetwork.ConnectUsingSettings(VersionName);

        Debug.Log("Connecting to Photon...");
    }

    //Вызывается Фотоном
    private void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby(TypedLobby.Default);

        Debug.Log("We are connected to master");
    }
    //Вызывается Фотоном
    private void OnJoinedLobby()
    {
        UserNamePanel.SetActive(true);
        Debug.Log("Joined lobby");
    }

    //Вызывается Фотоном
    private void OnDisconnectedFromPhoton()
    {
        Debug.Log("Lost connection to Photon");
    }


    /////////////////////////////////////////////// UI SECTION //////////////////////////////////////////////////

    public void OnChange_UserNameInput()
    {
        //ПРоверка на длину имени
        if (UserNameInput.text.Length >= 2)
        {
            CreateUserNameButton.SetActive(true);
        }
        else
        {
            CreateUserNameButton.SetActive(false);
        }
    }

    public void OnChange_Slider()
    {
        NumberOfBots.text = slider.value.ToString();
    }

    public void OnClick_CreateUserName()
    {
        PhotonNetwork.playerName = UserNameInput.text;

        UserNamePanel.SetActive(false);
        ConnectPanel.SetActive(true);

        Debug.Log("This player name is: " + PhotonNetwork.playerName);
    }

    public void OnClick_CreateRoom()
    {
        bool roomExists = false;

        foreach (var room in PhotonNetwork.GetRoomList())
        {
            if(room.Name == CreateRoomInput.text)
            {
                roomExists = true;
                break;
            }
        }

        if(!roomExists)
        {
            RoomOptions options = new RoomOptions();
            options.cleanupCacheOnLeave = true;
            options.DeleteNullProperties = true;
            options.EmptyRoomTtl = 1000;
            options.PlayerTtl = 0;
            options.IsOpen = true;
            options.maxPlayers = 8;
            string Mode;
            if (DM.isOn)
                Mode = " - DM";
            else
                Mode = " - Survival";
            PhotonNetwork.CreateRoom(CreateRoomInput.text + Mode, options, TypedLobby.Default);
        }
        else
        {
            StatusText.text = "Server name already exists";
            StatusText.color = Color.red;
        }
          
    }

    public void JoinRoomFromList(string name)
    {
        PhotonNetwork.JoinRoom(name);
    }

    public void OnClick_OnJoinRoom()
    {
        RoomOptions options = new RoomOptions();
        options.cleanupCacheOnLeave = true;
        options.DeleteNullProperties = true;
        options.EmptyRoomTtl = 1000;
        options.PlayerTtl = 0;
        options.IsOpen = true;
        options.maxPlayers = 8;
        //PhotonNetwork.JoinOrCreateRoom(JoinRoomInput.text, options, TypedLobby.Default);
        PhotonNetwork.JoinRoom(JoinRoomInput.text);
    }

    private void OnCreatedRoom()
    {
        Debug.Log("We have create the room");

        PhotonHashTable HashTable1 = new PhotonHashTable();
        PhotonHashTable HashTable2 = new PhotonHashTable();
        PhotonHashTable HashTable3 = new PhotonHashTable();
        PhotonHashTable HashTable4 = new PhotonHashTable();
        PhotonHashTable HashTable5 = new PhotonHashTable();

        HashTable1.Add("seed", Time.time.ToString());
        HashTable2.Add("NumOfBots", Int32.Parse(NumberOfBots.text));
        HashTable3.Add("StartGameFlag", false);
        if (DM.isOn)
            HashTable4.Add("Mode", "DM");
        else
            HashTable4.Add("Mode", "Survival");
        if (min5.isOn) HashTable5.Add("Time", (int)180);
        if (min10.isOn) HashTable5.Add("Time", (int)360);
        if (min15.isOn) HashTable5.Add("Time", (int)540);

        PhotonNetwork.room.SetCustomProperties(HashTable1);
        PhotonNetwork.room.SetCustomProperties(HashTable2);
        PhotonNetwork.room.SetCustomProperties(HashTable3);
        PhotonNetwork.room.SetCustomProperties(HashTable4);
        PhotonNetwork.room.SetCustomProperties(HashTable5);
        Debug.Log("We have created the room");
    }

    private void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("MainGame");
        Debug.Log("We have joined the room");
    }

    public void OnClickExitButton()
    {
        Application.Quit();
    }
}
