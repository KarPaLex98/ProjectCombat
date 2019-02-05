using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour {

    public float viewRadius;
    //[Range(0, 360)]
    //public float viewAngle;

    public LayerMask targetMask;
    public LayerMask obstacleMask;

    //public float meshResolution;

    [HideInInspector]
    public List<Transform> visibleTargets = new List<Transform>();

    void Start()
    {
        StartCoroutine("FindTargetsWithDelay", .2f);
    }

    //private void Update()
    //{
    //    DrawFieldOfView();
    //}

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        Collider2D[] targetsInViewRaius = Physics2D.OverlapCircleAll(transform.position, viewRadius, targetMask);
            //Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetsInViewRaius.Length; i++)
        {
            Transform target = targetsInViewRaius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
                //if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
                //{
                    float dstToTarget = Vector3.Distance(transform.position, target.position);
                    if (dstToTarget != 0 && !Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                    {
                        visibleTargets.Add(target);
                    }
                //}
            
        }
    }

    //void DrawFieldOfView()
    //{
    //    int stepCount = Mathf.RoundToInt(meshResolution);
    //    float stepAngleSize = viewAngle / stepCount;

    //    for (int i = 0; i <= stepCount; i++)
    //    {
    //        float angle = transform.eulerAngles.z - viewAngle / 2 + stepAngleSize * i;
    //        Debug.DrawLine(transform.position, transform.position + DirFromAngle(angle, true) * viewRadius, Color.red);
    //    }
    //}

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.z;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), 0);
    }
}
