using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ServerPrefabButton : MonoBehaviour {

    public Text RoomText;

    private void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(() => MenuManager.Instance.JoinRoomFromList(RoomText.text));
    }

}
