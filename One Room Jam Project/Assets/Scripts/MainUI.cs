using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainUI : Singleton<MainUI>
{

    [SerializeField] TextMeshPro interactText;


    private void Awake()
    {
        SetInteractText("", "");
    }

    public void SetInteractText(string action, string name)
    {
        interactText.text = action;
    }

}
