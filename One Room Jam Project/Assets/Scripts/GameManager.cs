using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class GameManager : Singleton<GameManager>
{

    public bool surveyStarted = false;

    private ColorGrading colorGrading = null;
    private PostProcessVolume ppv;

    private void Start()
    {
        ppv = FindObjectOfType<PostProcessVolume>();
        ppv.profile.TryGetSettings(out colorGrading);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            colorGrading.active = !colorGrading.active;
        }
    }

}
