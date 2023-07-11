using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class SpellManager : NetworkBehaviour
{
    [SerializeField] GameObject shop;
    private bool shopOpen;
    private AbilityManager am;

    public void Update()
    {
        if (Input.GetButtonDown("OpenShop") && IsOwner)
        {
            if (shopOpen)
            {
                CloseShop();
                shopOpen = false;
            }
            else
            {
                OpenShop();
                shopOpen = true;
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            OpenShop();
            shopOpen = true;
        }
        am = GetComponentInChildren<AbilityManager>();
    }

    public void OpenShop()
    {
        if (base.IsOwner)
        {
            shop.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void CloseShop()
    {
        if (base.IsOwner)
        {
            shop.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // unequips spell at i
    public void Unequip(int i)
    {
        am.equipped[i] = 0;
    }

    public void Equip(int i, int spellNum)
    {
        am.equipped[i] = spellNum;
    }

    public void SpellPressed(int spellNum)
    {
        //Primary
        if (spellNum < 9)
        {
            if (am.equipped[0] != 0)
                Equip(0, spellNum);
            else if (am.equipped[1] != 0)
                Equip(1, spellNum);
        }
        //Utility
        else if (spellNum > 12)
        {
            if (am.equipped[3] != 0)
                Equip(3, spellNum);
            else if (am.equipped[4] != 0)
                Equip(4, spellNum);
        }
        //Mobility
        else
        {
            if (am.equipped[2] != 0)
                Equip(2, spellNum);
        }
    }

    public void SwitchPrimary()
    {
        int temp = am.equipped[0];
        am.equipped[0] = am.equipped[1];
        am.equipped[1] = temp;
    }

    // Switches utility
    public void SwitchUtility()
    {
        int temp = am.equipped[3];
        am.equipped[3] = am.equipped[4];
        am.equipped[4] = temp;
    }
}
