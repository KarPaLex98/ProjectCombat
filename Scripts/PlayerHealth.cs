using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : Photon.MonoBehaviour {

    public bool IsDead = false;

    public float HealthAmount = 100f;

    public Image FillImage;

    public Player plMove;
    public Rigidbody2D Rigid;
    public CircleCollider2D Collider;
    public SpriteRenderer Sprite;
    public SpriteRenderer Gun;
    public GameObject PlayerCanvas;
    public GameObject KilledText_PREFAB;

    private void Awake()
    {
        if(photonView.isMine)
        GameManager.Instance.LocalPlayer = this.gameObject;
    }

    //Вызывается из скрипта пули
    [PunRPC] public void ReduceHealth(float amount)
    {
        ModifyHealth(amount);
    }

    private void CheckHealth()
    {
        FillImage.fillAmount = HealthAmount / 100f;

        if (photonView.isMine && HealthAmount <= 0)
        {
            IsDead = true;
            //Если игровой режим - "Выживание", то возрождать игрока не надо
            if (GameManager.Instance.Mode == "DM")
            { 
                GameManager.Instance.EnableRespawn();
                plMove.DisableInput = true;
                this.GetComponent<PhotonView>().RPC("Dead", PhotonTargets.AllBuffered);
            }
            else
            {
                GameManager.Instance.YouDead.text = "You are dead";
                plMove.DisableInput = true;
                this.GetComponent<PhotonView>().RPC("Dead", PhotonTargets.AllBuffered);
                GameManager.Instance.MainCamera.SetActive(true);
            }
        }
    }


    [PunRPC] //Вызывается из скрипта пули
    private void YouGotKilledBy(string name)
    {
        GameObject obj = Instantiate(KilledText_PREFAB, new Vector2(0, 0), Quaternion.identity);
        obj.transform.SetParent(GameManager.Instance.PlayerKillsGrid.transform, false);
        obj.GetComponent<Text>().text = "YOU GOT KILLED BY: " + name;
        obj.GetComponent<Text>().color = Color.red;
    }

    [PunRPC] //Вызывается из скрипта пули
    private void YouKilled(string name)
    {
        GameObject obj = Instantiate(KilledText_PREFAB, new Vector2(0, 0), Quaternion.identity);
        obj.transform.SetParent(GameManager.Instance.PlayerKillsGrid.transform, false);
        obj.GetComponent<Text>().text = "YOU KILLED: " + name;
        obj.GetComponent<Text>().color = Color.green;
    }


    public void EnableInput()
    {
        IsDead = false;
        plMove.DisableInput = false;
    }

    [PunRPC]
    private void Dead()
    {
        if (GameManager.Instance.Mode != "DM")
        {
            Player.PlayersCount--;
            Debug.Log(Player.PlayersCount.ToString());
        }
        GameManager.Instance.Target_List.Remove(this.GetComponent<Player>().transform);
        Rigid.gravityScale = 0;
        this.GetComponent<Player>().AllowMovement = false;
        Collider.enabled = false;
        Sprite.enabled = false;
        Gun.enabled = false;
        PlayerCanvas.SetActive(false);
    }

    [PunRPC] //Вызывается из игрового менеджера
    private void RevivePlayer()
    {
        Rigid.gravityScale = 3f;
        this.GetComponent<Player>().AllowMovement = true;
        Collider.enabled = true;
        Sprite.enabled = true;
        Gun.enabled = true;
        PlayerCanvas.SetActive(true);
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
