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
    private int chosenWizard = 1;

    public override void OnStartClient()
    {
        base.OnStartClient();

        gm = GameObject.FindWithTag("GameplayManager").GetComponent<GameplayManager>();
        SetWizardOnServer(base.Owner, chosenWizard);
        SetNameServer(base.Owner, SteamFriends.GetPersonaName());
    }

    [ServerRpc]
    private void SetNameServer(NetworkConnection conn, string name)
    {
        SetNameObser(conn, name);
    }

    [ObserversRpc]
    private void SetNameObser(NetworkConnection conn, string name)
    {
        gm.SetName(conn, name);
    }

    [ServerRpc]
    private void SetWizardOnServer(NetworkConnection conn, int type)
    {
        gm.SetWizard(conn, type);
    }
}
