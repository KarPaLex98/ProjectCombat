using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BotHealth : Photon.MonoBehaviour {

    bool IsDead = false;

    public float HealthAmount = 100f;

    public Image FillImage;

    public Rigidbody2D Rigid;
    public CircleCollider2D Collider;
    public SpriteRenderer Sprite;
    public SpriteRenderer Gun;
    public GameObject UnitCanvas;

    [HideInInspector] public GameObject LocalBot;

    [Space]

    [Header("Respawn stuff")]
    private float TimerAmountBot = 4f;
    private bool RunSpawnTimerBot = false;

    private void Awake()
    {
            LocalBot = this.gameObject;
    }

    private void Update()
    {
        if (RunSpawnTimerBot)
        {
            StartRespawnBot();
        }
    }

    //Called from bullet 
    [PunRPC] public void ReduceHealth(float amount)
    {
        ModifyHealth(amount);
    }

    public void EnableRespawnBot()
    {
        TimerAmountBot = 4f;
        RunSpawnTimerBot = true;
    }

    private void StartRespawnBot()
    {
        TimerAmountBot -= Time.deltaTime;

        if (TimerAmountBot <= 0)
        {
            //LocalBot.GetComponent<PhotonView>().RPC("ReviveBot", PhotonTargets.AllBuffered);
            LocalBot.GetComponent<BotHealth>().EnableInput();
            RespawnLocationBot();
            RunSpawnTimerBot = false;
        }
    }

    public void RespawnLocationBot()
    {
        this.GetComponent<PhotonView>().RPC("DestroyBot", PhotonTargets.AllBuffered);
        Collider2D[] targetsInViewRaius;
        System.Random tmpRnd = new System.Random();
        Vector3 Resp;
        //do
        //{
            Resp = GameManager.Instance.RespawnLocationList[tmpRnd.Next(0, GameManager.Instance.RespawnLocationList.Count)];
            //targetsInViewRaius = Physics2D.OverlapCircleAll(Resp, 10, GameManager.Instance.targetMask);
        //    Debug.Log("Хуйня");
        //}while (targetsInViewRaius.Length != 0);
        GameObject Bottmp = PhotonNetwork.Instantiate(GameManager.Instance.Bot.name, new Vector2(Resp.x, Resp.y), Quaternion.identity, 0);
        Bottmp.transform.localPosition = new Vector2(Resp.x, Resp.y);
        if (GameManager.Instance.Mode == "DM")
            GameManager.Instance.Target_List.Add(Bottmp.transform);
    }

    [PunRPC]
    void DestroyBot()
    {
        Destroy(LocalBot.gameObject);
    }

    private void CheckHealth()
    {
        FillImage.fillAmount = HealthAmount / 100f;

        if (photonView.isMine && HealthAmount <= 0)
        {
            IsDead = true;
            EnableRespawnBot();
            this.GetComponent<PhotonView>().RPC("Dead", PhotonTargets.AllBuffered);
            //RespawnLocationBot();
        }
    }

    //Called when timer respawn hit 0
    public void EnableInput()
    {
        IsDead = false;
    }

    [PunRPC]
    private void Dead()
    {

        this.GetComponent<Unit>().allowMovement = false;
        if (GameManager.Instance.Mode == "DM")
            GameManager.Instance.Target_List.Remove(LocalBot.transform);
        StopCoroutine(LocalBot.GetComponent<Unit>().UpdatePath());
        Collider.enabled = false;
        Sprite.enabled = false;
        Gun.enabled = false;
        UnitCanvas.SetActive(false);
    }

    [PunRPC]
    private void ReviveBot()
    {
        this.GetComponent<Unit>().allowMovement = true;
        //StartCoroutine(LocalBot.GetComponent<Unit>().UpdatePath());
        Collider.enabled = true;
        Sprite.enabled = true;
        Gun.enabled = true;
        UnitCanvas.SetActive(true);
        FillImage.fillAmount = 1f;
        HealthAmount = 100f;
    }

    private void ModifyHealth(float amount)
    {
        if (photonView.isMine)
        {
            HealthAmount -= amount;
            FillImage.fillAmount -= amount;
        }        
        else
        {
            HealthAmount -= amount;
            FillImage.fillAmount -= amount;
        }

        CheckHealth();
    }

}
