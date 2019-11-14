using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AlarmClock : MonoBehaviour
{

    [SerializeField] TextMeshPro clockText;
    [SerializeField] TextMeshPro AM;
    [SerializeField] TextMeshPro PM;

    [SerializeField] AudioClip buttonSound;

    private AudioSource alarmSource;

    private bool alarmActive = false;

    // Start is called before the first frame update
    void Start()
    {
        alarmSource = GetComponentInChildren<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        bool isAM = System.DateTime.Now.Hour < 12;

        if (isAM)
        {
            AM.enabled = true;
            PM.enabled = false;
        }
        else
        {
            PM.enabled = true;
            AM.enabled = false;
        }
        clockText.text = System.DateTime.Now.ToString("h:mm");

        bool textOn = (Time.realtimeSinceStartup % 1f < 0.5f) || !alarmActive;

        clockText.gameObject.SetActive(textOn);
        AM.gameObject.SetActive(textOn);
        PM.gameObject.SetActive(textOn);
        

    }


    public void Alarm()
    {
        print("Alarm activated!");

        alarmSource.Play();

        alarmActive = true;

    }


    public void ListenButtonPush()
    {
        alarmSource.Stop();
        GetComponentInChildren<Animator>().Play("Click", -1, 0);
        alarmActive = false;
    }

}
