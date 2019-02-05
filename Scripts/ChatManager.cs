using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : Photon.MonoBehaviour {

    public Player plMove; //Игрок
    public PhotonView photonView;  //компонент PhotonView данного игрока
    public GameObject BubbleSpeechOBJ;  //облако сообщения над игроком
    public Text UpdatedText; //Текст сообщения

    private InputField ChatInputField; //Поле ввода для сообщений

    private bool DisableSend; //Флаг запрета отправки сообщений


    private void Awake()
    {
        GameManager.Instance.ChatInputField.gameObject.SetActive(true);
        ChatInputField = GameManager.Instance.ChatInputField;
    }

    private void Update()
    {
        if (photonView.isMine)
        { 
            if (!DisableSend && Input.GetKeyDown(KeyCode.RightShift))
            {
                ChatInputField.gameObject.SetActive(true);
                ChatInputField.ActivateInputField();
            }

            if (ChatInputField.isFocused)
            {
                plMove.DisableInput = true;
            }
            else if (plMove.gameObject.GetComponent<PlayerHealth>().IsDead == false)
            {
                plMove.DisableInput = false;
            }
            if (!DisableSend && ChatInputField.isFocused)
            {
                if (Input.GetKeyDown(KeyCode.RightShift))
                    if (ChatInputField.text != "" && ChatInputField.text.Length > 0 && ChatInputField.text.Length < 60)
                    {
                        photonView.RPC("SendMsg", PhotonTargets.AllBuffered, ChatInputField.text);
                    
                        BubbleSpeechOBJ.SetActive(true);
                    
                        ChatInputField.text = "";
                        plMove.DisableInput = false;
                        DisableSend = true;
                        ChatInputField.gameObject.SetActive(false);
                    }
                    else
                    {
                        Debug.LogError("Length of message >60 or empty");
                    }

            }


        }
    }


    [PunRPC]
    private void SendMsg(string msg)
    {
        UpdatedText.text = msg;

        StartCoroutine("Remove");
    }

    IEnumerator Remove()
    {
        yield return new WaitForSeconds(3f);
        BubbleSpeechOBJ.SetActive(false);
        DisableSend = false;
    }

    private void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting) //Если локальный игрок "стримит"
        {
            stream.SendNext(BubbleSpeechOBJ.active);
        }
        else if(stream.isReading)
        {
            BubbleSpeechOBJ.SetActive((bool)stream.ReceiveNext());
        }
    }

}
