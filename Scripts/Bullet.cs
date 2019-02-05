using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : Photon.MonoBehaviour {


    public float MoveSpeed = 6f;

    public float DestroyTime = 2f;

    public float BulletDamage = 0.3f;

    [HideInInspector] public string GotKilledBy; 
    [HideInInspector] public GameObject CreatedFatherOBJ; //Тот, кто создал пулю

    private void Awake()
    {
        StartCoroutine("DestroyByTime");
    }

    private void Start()
    {
        if (photonView.isMine)
          GotKilledBy =  CreatedFatherOBJ.GetComponent<Player>().LocalPlayerName;
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

        if (target != null && target.tag == "Bot")
        {
            target.RPC("ReduceHealth", PhotonTargets.AllBuffered, BulletDamage);

            if (target.GetComponent<BotHealth>().FillImage.fillAmount <= 0)
            {
                ScoreManager.Instance.GetComponent<PhotonView>().RPC("ChangeScore", PhotonTargets.AllBuffered, GotKilledBy, "kills", 1);
            }

            target.GetComponent<BlinkColor>().GotHit();
        }

        if (target != null && (!target.isMine || target.isSceneView))
        {

            if (target.tag == "Player" && GameManager.Instance.Mode == "DM")
            {
                target.RPC("ReduceHealth", PhotonTargets.AllBuffered, BulletDamage);

                if (target.GetComponent<PlayerHealth>().FillImage.fillAmount <= 0)
                {
                    target.RPC("YouKilled", PhotonPlayer.Find(CreatedFatherOBJ.GetComponent<PhotonView>().ownerId), target.GetComponent<PhotonView>().owner.name);
                    ScoreManager.Instance.GetComponent<PhotonView>().RPC("ChangeScore", PhotonTargets.AllBuffered, target.GetComponent<PhotonView>().owner.name, "deaths", 1);
                    target.RPC("YouGotKilledBy", PhotonPlayer.Find(target.ownerId), GotKilledBy);
                    ScoreManager.Instance.GetComponent<PhotonView>().RPC("ChangeScore", PhotonTargets.AllBuffered, GotKilledBy, "kills", 1);
                }

                target.GetComponent<BlinkColor>().GotHit();
            }

            this.GetComponent<PhotonView>().RPC("DestroyOBJ", PhotonTargets.AllBuffered);//уничтожение пули
        }
        if (!collision.isTrigger) this.GetComponent<PhotonView>().RPC("DestroyOBJ", PhotonTargets.AllBuffered);
    }

}
