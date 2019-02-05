using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Unit : MonoBehaviour {

    public Transform target; //координаты противника
    public float speed = 5;  //Скоркость бота
    public float stoppingDst = 20; //Дистанция, когда нужно остановиться

    const float minPathUpdateTime = .2f;
    const float pathUpdateMoveThreshold = .5f;
    Vector3[] path;
    int targetIndex;

    [Space]

    [Header("For Gun")]
    public float speedBul = 30; // скорость пули
    public float fireRate = 10; // скорострельность
    public GameObject bulletPrefab;
    public Transform firePoint;
    public bool allowMovement = true;
    public Transform zRotate; // объект для вращения по оси Z
    // ограничение вращения
    public float minAngle = -90;
    public float maxAngle = 90;

    bool onePathSuccess = true;


    private float curTimeout, angle;
    private int invert = 1;

    public FieldOfView fow;

    void Start()
    {
        if (PhotonNetwork.isMasterClient)
        {
            StartCoroutine(UpdatePath());
        }
            
    }

    private void Update()
    {
        if (!ScoreManager.Instance.EndOfGame)
        {
            if (fow.visibleTargets != null && fow.visibleTargets.Count != 0 && fow.visibleTargets.Contains(target) && allowMovement)
            {
                Fire();
            }
            if (target != null && fow.visibleTargets.Contains(target))
            {
                SetRotation();
            }
        }
        else
        {
            StopCoroutine(UpdatePath());
            StopCoroutine("FollowPath");
        }
    }

    //Поворот оружием
    private void SetRotation()
    {
        //diff - будет смещением нашего нажатия от объекта
        Vector3 diff = target.transform.position - firePoint.transform.position;
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

    [PunRPC]
    private void FlipSprite_RIGHT()
    {
        invert = 1;
        Vector3 theScale = zRotate.transform.localScale;
        theScale.x *= -1;
        zRotate.transform.localScale = theScale;
    }

    [PunRPC]
    private void FlipSprite_LEFT()
    {
        invert = -1;
        Vector3 theScale = zRotate.transform.localScale;
        theScale.x *= -1;
        zRotate.transform.localScale = theScale;
    }

    public void OnPathFound(Vector3[] waypoints, bool pathSuccessful)
    {
        if (pathSuccessful)
        {
            
            path = waypoints;
            targetIndex = 0;
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
            onePathSuccess = false;
        }
    }

    //Функция обновления пути
    public IEnumerator UpdatePath()
    {
        if (Time.timeSinceLevelLoad < .3f)
        {
            yield return new WaitForSeconds(.3f);
        }

        Vector3 targetPosOld = Vector3.zero;
        float sqrMoveThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;

        target = getTarget();
        PathRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));
        targetPosOld = target.position;

        while (true)
        {
            yield return new WaitForSeconds(minPathUpdateTime);
            target = getTarget();
            if ((target != null) && (target.position - targetPosOld).sqrMagnitude > sqrMoveThreshold)
            {
                PathRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));
                targetPosOld = target.position;
            }
        }
    }

    //Получить координаты ближайшего противника
    private Transform getTarget()
    {
        if (GameManager.Instance.Target_List.Count < 1 || GameManager.Instance.Target_List.Count > 15)
        {
            throw new System.IndexOutOfRangeException("List length out of range");
        }
        else
        {
            float minDst = 100000.0f;
            Transform targetTransform = null;
            foreach (Transform target in GameManager.Instance.Target_List)
            {
                if (target != null && target.position != transform.position)
                {
                    float currentDst = Vector3.Distance(transform.position, target.position);
                    if (minDst > currentDst)
                    {
                        minDst = currentDst;
                        targetTransform = target;
                    }
                }
            }
            return targetTransform;
        }
    }

    //Следование пути 
    public IEnumerator FollowPath()
    {
        Vector3 currentWaypoint = path[0];
        while (true)
        {
            float currentDst = Vector3.Distance(transform.position, target.position);
            if (currentDst < stoppingDst && fow.visibleTargets.Contains(target.transform))
            {
                yield break;
            }
            if (transform.position == currentWaypoint)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                {
                    yield break;
                }
                currentWaypoint = path[targetIndex];
            }
            transform.position = Vector2.MoveTowards(transform.position, currentWaypoint, speed * Time.deltaTime);
            yield return null;
        }
    }

    //Функция стрельбы
    public void Fire()
    {
        curTimeout += Time.deltaTime;
        if ((curTimeout > fireRate) && (this.GetComponent<PhotonView>().isMine))
        {
            curTimeout = 0;
            Vector3 pos = target.position;
            Quaternion q = Quaternion.FromToRotation(Vector3.right, pos - firePoint.transform.position);
            GameObject obj = PhotonNetwork.Instantiate(bulletPrefab.name,
                new Vector2(firePoint.transform.position.x, firePoint.transform.position.y), q, 0);
            this.GetComponent<PhotonView>().RPC("AddForceBullet", PhotonTargets.All, obj.gameObject.GetPhotonView().viewID);
        }
    }

    [PunRPC]
    private void AddForceBullet(int bul)
    {
        PhotonView.Find(bul).GetComponent<Rigidbody2D>().AddForce(PhotonView.Find(bul).transform.right * speedBul);
    }

    //public void OnDrawGizmos()
    //{
    //    if (path != null)
    //    {
    //        for (int i = targetIndex; i < path.Length; i++)
    //        {
    //            Gizmos.color = Color.black;
    //            Gizmos.DrawCube(path[i], Vector3.one);

    //            if (i == targetIndex)
    //            {
    //                Gizmos.DrawLine(transform.position, path[i]);
    //            }
    //            else
    //            {
    //                Gizmos.DrawLine(path[i - 1], path[i]);
    //            }
    //        }
    //    }
    //}
}