using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Steamworks;

public class CharacterSpawner : NetworkBehaviour
{
    private GameplayManager gm;

    public override void OnStartClient()
    {
        base.OnStartClient();

        gm = GameObject.FindWithTag("GameplayManager").GetComponent<GameplayManager>();
        int type = PlayerPrefs.GetInt("Character", 0);
        SetWizardOnServer(base.Owner, type);
    }

    [ServerRpc]
    private void SetWizardOnServer(NetworkConnection conn, int type)
    {
        gm.SetWizard(conn, type);
    }
}
