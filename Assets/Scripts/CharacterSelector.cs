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

public class CharacterSelector : NetworkBehaviour
{
    private GameplayManager gm;
    [SerializeField] private GameObject ui;
    [SerializeField] private Button lockInButton;
    [SerializeField] private Image characterPortrait;
    [SerializeField] private Color[] characterColors;
    [SerializeField] private GameObject[] characterInfos;
    private int chosenWizard;

    public override void OnStartClient()
    {
        base.OnStartClient();

        gm = GameObject.FindWithTag("GameplayManager").GetComponent<GameplayManager>();

        if (IsOwner)
        {
            ui.SetActive(true);
            gm.playerName.text = SteamFriends.GetPersonaName();
        }
        else
            SendUserNameServer(SteamFriends.GetPersonaName());
    }

    [ServerRpc]
    private void SendUserNameServer(string name)
    {
        SendUserName(name);
    }

    [ObserversRpc]
    private void SendUserName(string name)
    {
        if (!IsOwner)
            gm.oppoName.text = name;
    }

    public void ChooseWizard(int type)
    {
        characterInfos[chosenWizard].SetActive(false);

        chosenWizard = type;
        characterPortrait.color = characterColors[type];
        characterInfos[type].SetActive(true);
        lockInButton.interactable = true;
    }

    public void LockIn()
    {
        SpawnWizardOnServer(base.Owner, chosenWizard);
        ui.SetActive(false);
    }

    [ServerRpc]
    private void SpawnWizardOnServer(NetworkConnection conn, int type)
    {
        gm.SpawnWizard(conn, type);
    }
}
