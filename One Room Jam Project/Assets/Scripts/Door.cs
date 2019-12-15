using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{

    private AudioSource audioSource;
    private Renderer renderer;
    private HideWhenSeen[] hideWhenSeen;
    private bool isOpen = false;

    [SerializeField]
    private Vector3 openRotation = Vector3.zero;


    private void Start()
    {
        audioSource = GetComponentInChildren<AudioSource>();
        renderer = GetComponentInChildren<Renderer>();
        hideWhenSeen = FindObjectsOfType<HideWhenSeen>();
        //ToggleHiddenGuys();
    }


    private void Update()
    {
        
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
        bool visible = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

        if (isOpen && visible && CheckHiddenGuyState(false))
        {
            //ToggleHiddenGuys();
            
        }
    }


    private void ToggleHiddenGuys()
    {
        foreach (HideWhenSeen h in hideWhenSeen)
            h.gameObject.SetActive(!h.gameObject.activeSelf);
    }


    private bool CheckHiddenGuyState(bool state)
    {
        foreach (HideWhenSeen h in hideWhenSeen)
            if (h.gameObject.activeSelf == state) return true;

        return false;
    }


    public void Open()
    {
        print("Door Opened");

        //audioSource.Play();

        transform.rotation = Quaternion.Euler(openRotation);

        isOpen = true;

    }

    
    public void PlaySound()
    {
        audioSource.Play();
    }
}
