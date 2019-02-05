using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkColor : Photon.MonoBehaviour {

    public SpriteRenderer Sprite;

    public enum EventCodes
    {
        ColorChange = 0
    }

    private void OnEnable()
    {
        PhotonNetwork.OnEventCall += OnPhotonEvent;
    }

    private void OnDisable()
    {
        PhotonNetwork.OnEventCall -= OnPhotonEvent;
    }

    private void OnPhotonEvent(byte eventCode, object content, int senderID)
    {
        EventCodes code = (EventCodes)eventCode;
        if (code == EventCodes.ColorChange)
        {
            object[] datas = content as object[];
            if (datas.Length == 4)
            {
                if ((int)datas[0] == base.photonView.viewID)
                    Sprite.color = new Color((float)datas[1], (float)datas[2], (float)datas[3]);
            }
        }
    }

    //called from bullet script
    public void GotHit()
    {
        ChangeColor_RED();

        StartCoroutine("ChangeColorOverTime");
    }

    //Called from gamemanager class when respawning
    public void ResetToWhite()
    {
        ChangeColor_WHITE();
    }

    IEnumerator ChangeColorOverTime()
    {
        yield return new WaitForSeconds(0.2f);
        ChangeColor_WHITE();
    }

    private void ChangeColor_RED()
    {
        float r = 1f, g = 0f, b = 0f; //red color

        object[] datas = new object[] { base.photonView.viewID, r, g, b };

        RaiseEventOptions options = new RaiseEventOptions()
        {
            CachingOption = EventCaching.DoNotCache,
            Receivers = ReceiverGroup.All
        };

        PhotonNetwork.RaiseEvent((byte)EventCodes.ColorChange, datas, false, options);
    }

    private void ChangeColor_WHITE()
    {
        float r = 1f, g = 1f, b = 1f; //white color

        object[] datas = new object[] { base.photonView.viewID, r, g, b };

        RaiseEventOptions options = new RaiseEventOptions()
        {
            CachingOption = EventCaching.DoNotCache,
            Receivers = ReceiverGroup.All
        };

        PhotonNetwork.RaiseEvent((byte)EventCodes.ColorChange, datas, false, options);
    }

}
