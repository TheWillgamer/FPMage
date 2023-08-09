using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageAOE : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.tag);
    }
}
