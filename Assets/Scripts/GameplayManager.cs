using UnityEngine;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;

public class GameplayManager : NetworkBehaviour
{
    public Transform[] Startingspawns = new Transform[0];
    private int _nextSpawn;
    [SerializeField] private NetworkObject playerPrefab;

    /// <summary>
    /// Currently spawned player objects. Only exist on the server.
    /// </summary>
    private List<NetworkObject> _spawnedPlayerObjects = new List<NetworkObject>();

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log(base.IsOwner);
        if (base.IsOwner)
        {
            Debug.Log("boy");
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        Vector3 position;
        Quaternion rotation;
        SetSpawn(out position, out rotation);

        //Make object and move it to proper scene.
        NetworkObject netIdent;
        netIdent = Instantiate<NetworkObject>(playerPrefab, position, rotation);
        _spawnedPlayerObjects.Add(netIdent);
        
        base.Spawn(netIdent.gameObject, netIdent.Owner);

        //NetworkObject netIdent = conn.identity;            
        netIdent.transform.position = position;
        //RpcTeleport(netIdent, position);
    }

    private void SetSpawn(out Vector3 pos, out Quaternion rot)
    {
        Transform result = Startingspawns[_nextSpawn];
        pos = result.position;
        rot = result.rotation;

        //Increase next spawn and reset if needed.
        _nextSpawn++;
        if (_nextSpawn >= Startingspawns.Length)
            _nextSpawn = 0;
    }
}
