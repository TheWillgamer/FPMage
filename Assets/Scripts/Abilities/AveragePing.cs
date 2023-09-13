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
        ping = Mathf.Min(60f, (float)tm.RoundTripTime / 2f);
    }
}
