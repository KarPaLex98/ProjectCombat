using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoolRoomList : MonoBehaviour {
   
    public Transform Grid;  //таблица Списка серверов на цене с главным меню
    private List<GameObject> serverOBJS = new List<GameObject>();  //список серверов


   public void OnReceivedRoomListUpdate()
   {
        UpdateRoomList();
        Debug.Log("Getting new info");
   }


    private void UpdateRoomList()
    {
        int i = 0;
        RoomInfo[] rooms = PhotonNetwork.GetRoomList();

        for (int h = 0; h < serverOBJS.Count; h++)
        {
            Destroy(serverOBJS[h]);
        }
        serverOBJS.Clear();

        if (rooms != null)
        {
            for (i = 0; i < rooms.Length; i++)
            {
                if (!rooms[i].open)
                    continue;

                GameObject roomButton = (GameObject)Instantiate(Resources.Load("ServerPrefab"));
                serverOBJS.Add(roomButton);
                roomButton.transform.SetParent(Grid.transform, false);
                roomButton.transform.Find("ServerText").GetComponent<Text>().text = rooms[i].name;
            }
        }


    }


}
