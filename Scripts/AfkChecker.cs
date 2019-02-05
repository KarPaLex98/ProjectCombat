using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AfkChecker : Photon.MonoBehaviour {

    public bool IsDis = false;
    private GameObject AfkUI;

    public float AfkTimer = 20f;
    public float DisTimer = 10f;


    private void Awake()
    {
        AfkUI = GameObject.Find("AfkUI");
    }

    private void Update()
    {
        if (PhotonNetwork.connected && photonView.isMine)
        {

            if (Input.anyKey)
            {
                ResetTimer();
            }

            AfkTimer -= Time.deltaTime;

            if (AfkTimer <= 0)
            {
                PlayerIsAFK();
            }

            if (IsDis)
            {
                DisTimer -= Time.deltaTime;
                AfkUI.GetComponentInChildren<Text>().text = "Disconected in: " + DisTimer.ToString("F0");

                if (DisTimer <= 0)
                {
                    PhotonNetwork.LeaveRoom();
                    PhotonNetwork.LoadLevel(0);
                }
            }

        }
    }

    private void ResetTimer()
    {
        AfkTimer = 20f;

        if (AfkUI != null && AfkUI.active)
        {
            AfkUI.transform.GetChild(0).gameObject.SetActive(false);
            DisTimer = 10f;
            IsDis = false;
        }
    }

    private void PlayerIsAFK()
    {
        IsDis = true;
        AfkUI.transform.GetChild(0).gameObject.SetActive(true);
    }



}
