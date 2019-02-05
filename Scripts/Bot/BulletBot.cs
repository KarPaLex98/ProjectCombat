using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBot : Photon.MonoBehaviour {

    public float MoveSpeed = 6f;
    public float DestroyTime = 2f;
    public float BulletDamage = 0.3f;

    private void Awake()
    {
        StartCoroutine("DestroyByTime");
    }

    IEnumerator DestroyByTime()
    {
        yield return new WaitForSeconds(DestroyTime);
        this.GetComponent<PhotonView>().RPC("DestroyOBJ", PhotonTargets.AllBuffered);
    }


    [PunRPC]
    private void DestroyOBJ()
    {
        Destroy(this.gameObject);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!photonView.isMine)
            return;

        PhotonView target = collision.gameObject.GetComponent<PhotonView>();

        if (target != null && target.tag == "Bot" && GameManager.Instance.Mode == "DM")
        {
            target.RPC("ReduceHealth", PhotonTargets.AllBuffered, BulletDamage);
            target.GetComponent<BlinkColor>().GotHit();
        }

        if (target != null && target.tag == "Player")
        {
            target.RPC("ReduceHealth", PhotonTargets.AllBuffered, BulletDamage);
            target.GetComponent<BlinkColor>().GotHit();
            if (target.GetComponent<PlayerHealth>().FillImage.fillAmount <= 0)
            {
                ScoreManager.Instance.GetComponent<PhotonView>().RPC("ChangeScore", PhotonTargets.AllBuffered, target.GetComponent<PhotonView>().owner.name, "deaths", 1);
            }
        }

        if (!collision.isTrigger) this.GetComponent<PhotonView>().RPC("DestroyOBJ", PhotonTargets.AllBuffered);
    }

}
