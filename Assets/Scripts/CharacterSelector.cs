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
    [SerializeField] private GameObject[] characterModels;
    [SerializeField] private GameObject[] characterInfos;
    private int chosenWizard = -1;

    private GameObject wizardDemo;      // Shows the wizard that was chozen
    [SerializeField] private Transform demoLoc;     // where the wizardDemo is created

    public override void OnStartClient()
    {
        base.OnStartClient();

        gm = GameObject.FindWithTag("GameplayManager").GetComponent<GameplayManager>();

        if (IsOwner)
        {
            ui.SetActive(true);
        }
    }

    public void ChooseWizard(int type)
    {
        if (chosenWizard == type)
            return;

        if (wizardDemo != null)
        {
            Destroy(wizardDemo);
            characterInfos[chosenWizard].SetActive(false);
        }
            
        wizardDemo = Instantiate(characterModels[type], demoLoc.position, demoLoc.rotation);
        chosenWizard = type;

        characterInfos[type].SetActive(true);
        lockInButton.interactable = true;
    }

    public void LockIn()
    {
        SpawnWizardOnServer(base.Owner, chosenWizard);
        SetNameServer(SteamFriends.GetPersonaName());
        ui.SetActive(false);
    }

    [ServerRpc]
    private void SetNameServer(string name)
    {
        SetNameObser(name);
    }

    [ObserversRpc]
    private void SetNameObser(string name)
    {
        gm.SetName(IsOwner, name);
    }

    [ServerRpc]
    private void SpawnWizardOnServer(NetworkConnection conn, int type)
    {
        gm.SpawnWizard(conn, type);
    }
}
