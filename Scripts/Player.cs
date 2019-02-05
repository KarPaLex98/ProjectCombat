using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : Photon.MonoBehaviour {

    [Header("General")]
    public string LocalPlayerName;//имя локального игрока
    public PhotonView photonView;//компонент PhotonView данного игрока
    public Rigidbody2D Rigid;
    public Animator Anim;
    public SpriteRenderer Sprite;
    public GameObject PlayerCam; //Камера игрока
    public GameObject Bullet_PREFAB;
    public Transform FirePoint; //точка, откуда будет вылетать пуля
    public Text PlayerNameText;
    static public int PlayersCount = 0; //переменная для подсчта игроков в текущем матче,
    //она нужна для логики игрового режима "Выживание"

    [Space]

    [Header("Booleans")]
    public bool DisableInput = false;//Запрет ввода
    public bool AllowMovement = true;//управление движением
    public bool IsGrouned = false;///Флаг того, что персонаж на земле

    [Space]

    [Header("Floats and Ints")]
    public float MoveSpeed = 100f; //скорость передвижения
    public float JumpForce = 100f; //сила прыжка
    public float FlyForce = 100f; //скорость полёта

    [Space]

    [Header("For Gun")]
    public float speed = 30; // скорость пули
    public float fireRate = 10; // скорострельность
    public Camera PlayCam; //камера игрока
    public Transform zRotate; // объект для вращения по оси Z

    // ограничение вращения
    public float minAngle = -60;
    public float maxAngle = 60;

    private float curTimeout, angle;
    private int invert = 1;
    private Vector3 mouse;


    private void Awake()
    {
        SetUpData_LOCAL_NONLOCAL();
        PlayersCount++;
    }
    //инициализация игрока
    private void SetUpData_LOCAL_NONLOCAL()
    {
        if (photonView.isMine) //Only for our local player
        {
            PlayerCam.SetActive(true);

            PlayerCam.transform.SetParent(null, false);

            PlayerNameText.text = "YOU: " + PhotonNetwork.playerName;
            LocalPlayerName = PhotonNetwork.playerName;
        }
        else
        {
            PlayerNameText.text = photonView.owner.name;
            PlayerNameText.color = Color.red;
        }
    }

    private void Update()
    {
        //Если матч не закончился
        if (!ScoreManager.Instance.EndOfGame)
        {
            if (photonView.isMine && !DisableInput)
            {
                CheckInput();
            }

            if (Input.GetMouseButton(0) && AllowMovement)
            {
                Fire();
            }
            else
            {
                curTimeout = 1000;
            }
 
            if ((!DisableInput) && (photonView.isMine) && (zRotate)) SetRotation();//Вращение пушки по координатам мыши

            if (photonView.isMine)
            {   
                //отражение спрайта при поворотах
                if ((mouse.x < zRotate.position.x) && (!Sprite.flipX))
                {
                    PlayerCam.GetComponent<CameraFollow2D>().offset = new Vector3(-1.1f, 1.31f, 0f);
                    photonView.RPC("FlipSprite_LEFT", PhotonTargets.AllBuffered);
                }
                else if ((mouse.x > zRotate.position.x) && (Sprite.flipX))
                {
                    PlayerCam.GetComponent<CameraFollow2D>().offset = new Vector3(1.1f, 1.31f, 0f);
                    photonView.RPC("FlipSprite_RIGHT", PhotonTargets.AllBuffered);
                }
            }
        }
    }

    void FixedUpdate()
    {
        //Оработчик движений
        if (AllowMovement && !ScoreManager.Instance.EndOfGame)
        {
            if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && photonView.isMine)
            {
                var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0);
                transform.position += move * MoveSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.W) && photonView.isMine)
            {
                Rigid.AddForce(Vector2.up * FlyForce * Time.deltaTime);
            }
        }
    }

    //Обработка анимаций и прыжка
    private void CheckInput()
    {
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            Anim.SetBool("IsMove", true);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            PlayerCam.GetComponent<CameraFollow2D>().offset = new Vector3(-1.1f, 1.31f,0f);
        }
        else if(Input.GetKeyUp(KeyCode.A))
        {
            Anim.SetBool("IsMove", false);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            PlayerCam.GetComponent<CameraFollow2D>().offset = new Vector3(1.1f, 1.31f, 0f);
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            Anim.SetBool("IsMove", false);
        }

        if (Input.GetKeyDown(KeyCode.Space) && IsGrouned)
        {
            Rigid.AddForce(Vector2.up * JumpForce * Time.deltaTime);
        }

    }

    void SetRotation()
    {
        mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //diff - будет смещением нашего нажатия от объекта
        Vector3 diff = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        //номализация приводит каждое значение в промежуток
        //от -1 до 1
        diff.Normalize();
        //по нормализованному виду мы находим угол, так как в diff
        //находится вектор, который можно перенести на тригонометрическую окружность
        float rot_z = Mathf.Atan2(diff.y * invert, diff.x * invert) * Mathf.Rad2Deg;
        //и приваиваем наш угол персонажу
        zRotate.transform.rotation = Quaternion.Euler(0f, 0f, rot_z);
        //Показывает, нашли ли мы выключенный объект в нашем массиве
    }
    //Процедура выстрела
    void Fire()
    {
        curTimeout += Time.deltaTime;
        if ((curTimeout > fireRate) && (photonView.isMine))
        {
            curTimeout = 0;
            Vector3 pos = Input.mousePosition;
            pos.z = transform.position.z - Camera.main.transform.position.z;
            pos = Camera.main.ScreenToWorldPoint(pos);
            Quaternion q = Quaternion.FromToRotation(Vector3.right, pos - transform.position);
            GameObject obj = PhotonNetwork.Instantiate(Bullet_PREFAB.name,
                new Vector2(FirePoint.transform.position.x, FirePoint.transform.position.y), q, 0);
            obj.GetComponent<Bullet>().CreatedFatherOBJ = this.gameObject;
            this.GetComponent<PhotonView>().RPC("AddForceBullet", PhotonTargets.All, obj.gameObject.GetPhotonView().viewID);
            Anim.SetBool("IsShot", false);
            AllowMovement = true;
        }
    }

    [PunRPC]
    private void AddForceBullet(int bul)//Добавление ускорения пуле, чтоб она полетела
    {
        PhotonView.Find(bul).GetComponent<Rigidbody2D>().AddForce(PhotonView.Find(bul).transform.right * speed);
    }


    [PunRPC]
    private void FlipSprite_RIGHT()//Отражение спрайта
    {
        Sprite.flipX = false;
        invert = 1;
        Vector3 theScale = zRotate.transform.localScale;
        theScale.x *= -1;
        zRotate.transform.localScale = theScale;
    }

    [PunRPC]
    private void FlipSprite_LEFT()//Отражение спрайта
    {
        Sprite.flipX = true;
        invert = -1;
        Vector3 theScale = zRotate.transform.localScale;
        theScale.x *= -1;
        zRotate.transform.localScale = theScale;
    }

    void OnCollisionEnter2D(Collision2D col)//Если игрок стоит на коллайдере
    {
        IsGrouned = true;
    }

    void OnCollisionExit2D(Collision2D col)//Если игрок не стоит на коллайдере
    {
        IsGrouned = false;
    }

}
