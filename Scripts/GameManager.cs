using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PhotonHashTable = ExitGames.Client.Photon.Hashtable;

public class GameManager : MonoBehaviour {

    public static GameManager Instance;//Указатель на GameManager

    [Header("General Stuff")]
    public MapGenerator MapGen;//MapGenerator для генерации карты и кучи другого всего
    public Text PingText;//контрол текста для отображения пинга
    public GameObject Player_PREFAB;
    public GameObject PlayerFeed_PREFAB;//Надпись "Player have joined/left the room"
    public GameObject PlayerFeedGrid;//Таблица для отображения подключения или выхода из комнаты
    public GameObject PlayerKillsGrid;//Таблица отображения убийства игроков
    public GameObject MainCanvas;//Главная канва
    public GameObject SceneCam;
    public GameObject DisUI;//Панель выхода
    public Text CountOfPlayers; //Количество игроков
    public GameObject A_;
    public GameObject Bot;
    public Button StartButton;
    public float MatchTime = 5f;
    public ScoreManager ScorMan;
    public GameObject ScoreBoard;
    public LayerMask targetMask;
    public Text YouDead;
    public GameObject MainCamera;
    public InputField ChatInputField;

    [HideInInspector] public string Mode;
    [HideInInspector] public List<Transform> Target_List;
    [HideInInspector] public GameObject LocalPlayer; //Set from playerhealth class

    [Space]

    [Header("Respawn stuff")]
    public Text SpawnTimerText;
    public GameObject RespawnUI;
    private float TimerAmount = 4f;
    private bool RunSpawnTimer = false;
    public List<Vector3> RespawnLocationList = new List<Vector3>();
    public string seed;
    public bool StartGameFlag = false;

    private void Awake()
    {
        Instance = this;
        PhotonNetwork.sendRate = 40;
        PhotonNetwork.sendRateOnSerialize = 30;
        MapGen.seed = (string)PhotonNetwork.room.CustomProperties["seed"];//Параметр для генерации карты
        MatchTime = (int)PhotonNetwork.room.CustomProperties["Time"];//Время матча
        MapGen.GenerateMap();//Запуск генерации карты
        A_.GetComponent<Grid_A>().CreateGridStart();
        System.Random rndSpawn = new System.Random(MapGen.seed.GetHashCode());
        CreateRespawnLocationList(rndSpawn);//Заполнение списка точек респавна
        MainCanvas.SetActive(true);
    }

    private void CreateRespawnLocationList(System.Random rndSpawn)
    {
        MapGenerator.Room mainRoom;
        int i;
        //Нужно взять случайную комнату
        do
        {
            mainRoom = MapGen.roomss[rndSpawn.Next(MapGen.roomss.Count)];
            i = rndSpawn.Next(1, mainRoom.tiles.Count);
            if (MapGen.map[mainRoom.tiles[i].tileX, mainRoom.tiles[i].tileY] != 1 &&
                (GetSurroundingWallCount(mainRoom.tiles[i].tileX, mainRoom.tiles[i].tileY) == 0))
            {
                RespawnLocationList.Add(MapGen.CoordToWorldPoint(mainRoom.tiles[i]));
                //GameObject Sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //Sphere.transform.position = MapGen.CoordToWorldPoint(mainRoom.tiles[i]);
            }
        }
        while (RespawnLocationList.Count != 20);
    }
    //Проверка плиток рядом
    int GetSurroundingWallCount(int mapX, int mapY)
    {
        int wallCount = 0;
        for (int neighboutX = mapX - 4; neighboutX <= mapX + 4; neighboutX++)
        {
            if (MapGen.IsInMapRange(neighboutX, mapY))
            {
                wallCount += MapGen.map[neighboutX, mapY];
            }
        }
        for (int neighboutY = mapY - 4; neighboutY <= mapY + 4; neighboutY++)
        {
            if (MapGen.IsInMapRange(mapX, neighboutY))
            {
                wallCount += MapGen.map[mapX, neighboutY];
            }
        }
        return wallCount;
    }

    private void Start()
    {
        if (PhotonNetwork.isMasterClient)
            StartButton.enabled = true;
        else
            StartButton.enabled = false;
        //Доавление в таблицу результатов текущего игрока
        ScoreManager.Instance.GetComponent<PhotonView>().RPC("SetScore", PhotonTargets.AllBuffered, PhotonNetwork.playerName, "kills", 0);
        ScoreManager.Instance.GetComponent<PhotonView>().RPC("SetScore", PhotonTargets.AllBuffered, PhotonNetwork.playerName, "deaths", 0);
        StartGameFlag = (bool)PhotonNetwork.room.CustomProperties["StartGameFlag"];
        Mode = (string)PhotonNetwork.room.CustomProperties["Mode"];
        Debug.Log("Start OnPhotonPlayerConnected = " + StartGameFlag.ToString() + "  isMine = " + Instance.GetComponent<PhotonView>().isMine);
        if ((StartGameFlag == true) /*&& (Instance.GetComponent<PhotonView>().isMine)*/)
            SpawnPlayer();
    }

    private void OnPhotonPlayerConnected(PhotonPlayer player)
    {
        //Доавление в таблицу результатов пришедшего игрока
        ScoreManager.Instance.GetComponent<PhotonView>().RPC("SetScore", PhotonTargets.AllBuffered, player.NickName, "kills", 0);
        ScoreManager.Instance.GetComponent<PhotonView>().RPC("SetScore", PhotonTargets.AllBuffered, player.NickName, "deaths", 0);

        GameObject obj = Instantiate(PlayerFeed_PREFAB, new Vector2(0, 0), Quaternion.identity);
        obj.transform.SetParent(PlayerFeedGrid.transform, false);
        obj.GetComponent<Text>().text = player.name + " Joined the room";
        obj.GetComponent<Text>().color = Color.green;
        if (StartGameFlag && Instance.GetComponent<PhotonView>().isMine)
            Instance.GetComponent<PhotonView>().RPC("StartTimer", player, Timer.timeLeft-1);
    }

    private void OnPhotonPlayerDisconnected(PhotonPlayer player)
    {
        //Удаление из талицы результатов ушедшего игрока
        ScoreManager.Instance.GetComponent<PhotonView>().RPC("DeleteScore", PhotonTargets.AllBuffered, player.NickName);
        GameObject obj = Instantiate(PlayerFeed_PREFAB, new Vector2(0, 0), Quaternion.identity);
        obj.transform.SetParent(PlayerFeedGrid.transform, false);
        obj.GetComponent<Text>().text = player.name + " left the room";
        obj.GetComponent<Text>().color = Color.red;
    }

    private void Update()
    {
        PingText.text = "Network ping: " + PhotonNetwork.GetPing(); //Display the ping amount
        CountOfPlayers.text = PhotonNetwork.room.PlayerCount.ToString();



        CheckInput();
        
        if (RunSpawnTimer)
        {
            StartRespawn();
        }
        //Если все игроки в режиме "выживание" умерли
        if (StartGameFlag && Player.PlayersCount == 0)
        {
            //сбрасываем таймер
            Timer.timeLeft = 0;
        }

    }

    private bool Off = false;
    private void CheckInput()
    {
        //Если матч не окончен
        if (!ScoreManager.Instance.EndOfGame)
        {
            if (Off && Input.GetKeyDown(KeyCode.Escape))
            {
                DisUI.SetActive(false);
                Off = false;
            }
            else if (!Off && Input.GetKeyDown(KeyCode.Escape))
            {
                DisUI.SetActive(true);
                Off = true;
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                RespawnLocation();
            }
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ScoreBoard.SetActive(!ScoreBoard.activeSelf);
            }
        }
        else
        {
            PhotonNetwork.room.IsVisible = false;
            if (Off && Input.GetKeyDown(KeyCode.Escape))
            {
                ScoreBoard.SetActive(true);
                DisUI.SetActive(false);
                Off = false;
            }
            else if (!Off && Input.GetKeyDown(KeyCode.Escape))
            {
                ScoreBoard.SetActive(false);
                DisUI.SetActive(true);
                Off = true;
            }
        }
    }

    //Вызывается из player health кога игрок умер
    public void EnableRespawn() 
    {
        TimerAmount = 4f;
        RunSpawnTimer = true;
        RespawnUI.SetActive(true);
    }
    //Начала респавна
    private void StartRespawn()
    {
        TimerAmount -= Time.deltaTime;
        SpawnTimerText.text = "Respawn in: " + TimerAmount.ToString("F0");

        if (TimerAmount <= 0)
        {
            LocalPlayer.GetComponent<PhotonView>().RPC("RevivePlayer", PhotonTargets.AllBuffered);
            LocalPlayer.GetComponent<PlayerHealth>().EnableInput();
            RespawnLocation();
            RespawnUI.SetActive(false);
            RunSpawnTimer = false;
        }
    }
    //Выбор точки респавна
    public void RespawnLocation()
    {
        System.Random rnd = new System.Random();
        Collider2D[] targetsInViewRaius;
        Vector3 Resp;
        do
        {
            Resp = RespawnLocationList[rnd.Next(0, RespawnLocationList.Count)];
            targetsInViewRaius = Physics2D.OverlapCircleAll(Resp, 5, targetMask);
        } while (targetsInViewRaius.Length != 0);

        LocalPlayer.transform.localPosition = new Vector2(Resp.x, Resp.y);
        Target_List.Add(LocalPlayer.transform);
        LocalPlayer.GetComponent<BlinkColor>().ResetToWhite();
    }
    //Обработчик нажатия кнопки "Start Game" мастер-клиентом
    public void StartGame()
    {
        System.Random tmpRnd = new System.Random();
        Vector3 Resp;
        Collider2D[] targetsInViewRaius;
        //Заспавнить себя
        tmpRnd = new System.Random();
        Resp = RespawnLocationList[tmpRnd.Next(0, RespawnLocationList.Count)];
        Transform Playertmp = PhotonNetwork.Instantiate(Player_PREFAB.name, new Vector2(Resp.x, Resp.y), Quaternion.identity, 0).GetComponent<Transform>();
        Instance.GetComponent<PhotonView>().RPC("AddTarget", PhotonTargets.AllBuffered, Playertmp.gameObject.GetPhotonView().viewID);
        //Заспавнить ботов
        if ((int)PhotonNetwork.room.CustomProperties["NumOfBots"] < 0 || (int)PhotonNetwork.room.CustomProperties["NumOfBots"] > 7)
            new System.IndexOutOfRangeException("The number of bots is more than 7 or less than 0");
        if (PhotonNetwork.isMasterClient)
        {
            if ((int)PhotonNetwork.room.CustomProperties["NumOfBots"] != 0)
            {
                for (int i = 0; i < (int)PhotonNetwork.room.CustomProperties["NumOfBots"]; i++)
                {
                    do
                    {
                        Resp = RespawnLocationList[tmpRnd.Next(0, RespawnLocationList.Count)];
                        targetsInViewRaius = Physics2D.OverlapCircleAll(Resp, 5, targetMask);
                    } while (targetsInViewRaius.Length != 0);
                    Transform Bottmp = PhotonNetwork.Instantiate(Bot.name, new Vector2(Resp.x, Resp.y), Quaternion.identity, 0).GetComponent<Transform>();
                    if (Mode == "DM")
                        Instance.GetComponent<PhotonView>().RPC("AddTarget", PhotonTargets.AllBuffered, Bottmp.gameObject.GetPhotonView().viewID);
                }
            }
        }
        StartGameFlag = true;
        PhotonHashTable HashTable3 = new PhotonHashTable();
        HashTable3.Add("StartGameFlag", StartGameFlag);
        PhotonNetwork.room.SetCustomProperties(HashTable3);
        //Спавн других игроков
        Instance.GetComponent<PhotonView>().RPC("SpawnPlayer", PhotonTargets.Others);
        MainCanvas.SetActive(false);
        SceneCam.SetActive(false);
        Instance.GetComponent<PhotonView>().RPC("StartTimer", PhotonTargets.All, (int)PhotonNetwork.room.CustomProperties["Time"]);
    }
    //Если мастер клиент вышел
    void OnMasterClientSwitched(PhotonPlayer newMasterClient)
    {
        Vector3 Resp;
        System.Random tmpRnd = new System.Random();
        Collider2D[] targetsInViewRaius;
        if (PhotonNetwork.isMasterClient)
        {
            if ((int)PhotonNetwork.room.CustomProperties["NumOfBots"] != 0)
            {
                for (int i = 0; i < (int)PhotonNetwork.room.CustomProperties["NumOfBots"]; i++)
                {
                    do
                    {
                        Resp = RespawnLocationList[tmpRnd.Next(0, RespawnLocationList.Count)];
                        targetsInViewRaius = Physics2D.OverlapCircleAll(Resp, 5, targetMask);
                    } while (targetsInViewRaius.Length != 0);
                    Transform Bottmp = PhotonNetwork.Instantiate(Bot.name, new Vector2(Resp.x, Resp.y), Quaternion.identity, 0).GetComponent<Transform>();
                    if (Mode == "DM")
                        Instance.GetComponent<PhotonView>().RPC("AddTarget", PhotonTargets.AllBuffered, Bottmp.gameObject.GetPhotonView().viewID);
                }
            }
        }
        PhotonHashTable HashTable = new PhotonHashTable();
        HashTable.Add("StartGameFlag", StartGameFlag);
        PhotonNetwork.room.SetCustomProperties(HashTable);
        StartGameFlag = (bool)PhotonNetwork.room.CustomProperties["StartGameFlag"];
    }

    [PunRPC]
    public void SpawnOneBot()
    {
        System.Random tmpRnd = new System.Random();
        Vector3 Resp;
        Collider2D[] targetsInViewRaius;
        //Заспавнить себя
        tmpRnd = new System.Random();
        Resp = RespawnLocationList[tmpRnd.Next(0, RespawnLocationList.Count)];
        Transform Bottmp = PhotonNetwork.Instantiate(Bot.name, new Vector2(Resp.x, Resp.y), Quaternion.identity, 0).GetComponent<Transform>();
        if (Mode == "DM")
            Instance.GetComponent<PhotonView>().RPC("AddTarget", PhotonTargets.AllBuffered, Bottmp.gameObject.GetPhotonView().viewID);
    }


    [PunRPC]
    public void SpawnPlayer()//функция спавна обычного игрока
    {
        Collider2D[] targetsInViewRaius;
        System.Random tmpRnd = new System.Random();
        Vector3 Resp;
        StartGameFlag = true;
        //do
        //{
            Resp = RespawnLocationList[tmpRnd.Next(0, RespawnLocationList.Count)];
        //    targetsInViewRaius = Physics2D.OverlapCircleAll(Resp, 10, targetMask);
        //} while (targetsInViewRaius.Length != 0);
        Transform Playertmp = PhotonNetwork.Instantiate(Player_PREFAB.name, new Vector2(Resp.x, Resp.y), Quaternion.identity, 0).GetComponent<Transform>();
        Instance.GetComponent<PhotonView>().RPC("AddTarget", PhotonTargets.AllBuffered, Playertmp.gameObject.GetPhotonView().viewID);
        MainCanvas.SetActive(false);
        SceneCam.SetActive(false);
    }

    //Добавление игрока в список целей ботов
    [PunRPC]
    public void AddTarget(int tmp)
    {
        Target_List.Add(PhotonView.Find(tmp).transform);
    }
    //Удаление игрока из списка целей игроков
    [PunRPC]
    public void RemoveTarget(int tmp)
    {
        Target_List.Remove(PhotonView.Find(tmp).transform);
    }

    //Выход из матча
    public void LeaveRoom()
    {
        Player.PlayersCount = 0;
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel(0);
    }
}
