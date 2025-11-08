using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuChanger : MonoBehaviour
{
    public GameObject[] menus;
    public GameObject painKillerInjectionLayoutMenu;

    private void Start()
    {
        OpenMenu(2);
    }

    public void OpenMenu(int menuID)
    {
        for (int i = 0; i < menus.Length; i++)
        {
            menus[i].SetActive(menuID == i);
        }
    }

    public void DestroyPainKillerInjectionMenu()
    {
        painKillerInjectionLayoutMenu.SetActive(false);
    }
}
