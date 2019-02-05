using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyByTime : MonoBehaviour {

    public float DestroyTime = 3f;

    private void OnEnable()
    {
        Destroy(gameObject, DestroyTime);
    }

}
