using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
        if (IsOwner)
            ui.SetActive(true);

        gm = GameObject.FindWithTag("GameplayManager").GetComponent<GameplayManager>();
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
