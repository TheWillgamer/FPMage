using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CharacterSelector : NetworkBehaviour
{
    private GameplayManager gm;
    [SerializeField] private GameObject ui;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
            ui.SetActive(true);

        gm = GameObject.FindWithTag("GameplayManager").GetComponent<GameplayManager>();
    }

    public void ChooseWizard(int type)
    {
        SpawnWizardOnServer(base.Owner, type);
        ui.SetActive(false);
    }

    [ServerRpc]
    private void SpawnWizardOnServer(NetworkConnection conn, int type)
    {
        gm.SpawnWizard(conn, type);
    }
}
