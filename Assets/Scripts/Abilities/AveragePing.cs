using FishNet;
using FishNet.Managing.Timing;
using UnityEngine;

public class AveragePing : MonoBehaviour
{
    private TimeManager tm;
    public float ping;
    
    void Start()
    {
        tm = InstanceFinder.TimeManager;
    }

    void Update()
    {
        Debug.Log(Mathf.Min(200f, tm.RoundTripTime / 2));
    }
}
