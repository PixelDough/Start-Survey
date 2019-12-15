using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Interactable : MonoBehaviour
{

    private Material startMaterial;
    private Mesh mesh;

    private bool isHeld = false;
    [HideInInspector] public bool canInteract = true;
    private HoldPoint holdPoint;

    private float heldRotation = 0f;

    private Rigidbody rb;
    private Collider collider;

    private Transform startParent;

    private bool isOpen = false;

    [SerializeField] AudioClip soundEffect;
    [SerializeField] string name = "NAME";
    [SerializeField] Transform lookPoint;

    [HideInInspector] public bool isHoveredOver = false;

    private string action = "Use";

    enum InteractionTypes
    {
        Pickup,
        Button,
        Trash,
        Folder,
        OpenClose,
        Look
    }
    [SerializeField] InteractionTypes interactionType = InteractionTypes.Pickup;

    private void Start()
    {
        //startMaterial = GetComponentInChildren<Renderer>().material;
        //mesh = GetComponentInChildren<MeshFilter>().mesh;

        startParent = gameObject.transform.parent;

        holdPoint = FindObjectOfType<HoldPoint>();

        try
        {
            rb = GetComponentInChildren<Rigidbody>();
        }
        catch (Exception e)
        {
            print("No Rigidbody.");
        }
        collider = GetComponentInChildren<Collider>();

        switch(interactionType)
        {
            case InteractionTypes.Pickup:
                action = "Pick Up";
                break;
            case InteractionTypes.Button:
                action = "Press";
                break;
            case InteractionTypes.Trash:
                action = "Trash";
                break;
            case InteractionTypes.Folder:
                action = "Open";
                break;
            case InteractionTypes.OpenClose:
                action = "Toggle";
                break;
            case InteractionTypes.Look:
                action = "Look";
                break;
        }
    }


    private void Update()
    {

        //if (isHeld && Input.GetMouseButtonDown(2))
        //{

        //    isHeld = false;
        //    transform.SetParent(startParent);
        //    if (rb) rb.isKinematic = false;
        //    collider.enabled = true;
        //    rb.velocity = Camera.main.transform.forward * 1000f * Time.deltaTime;
        //    holdPoint.currentHeldItem = null;

        //}
    }


    private void LateUpdate()
    {
        if (!GameManager.Instance.surveyStarted || !canInteract) { return; }

        if (isHeld)
        {
            transform.SetParent(holdPoint.transform);
            transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, 5f * Time.deltaTime);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(new Vector3(0, heldRotation, 0)), 5f * Time.deltaTime);
            if (rb) rb.isKinematic = true;
            collider.enabled = false;
            //DrawOutlineMesh();
            heldRotation += Input.mouseScrollDelta.y * 2000f * Time.deltaTime;

            
        }

        else if (interactionType != InteractionTypes.Pickup)
        {
            
            transform.SetParent(startParent);
            if (rb) rb.isKinematic = false;
            collider.enabled = true;

            heldRotation = 0f;
        }
    }


    private void DrawOutlineMesh()
    {
        //Graphics.DrawMesh(mesh, Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale), glowMaterial, 0);
    }


    //private void OnMouseEnter()
    //{
    //    if (!this.enabled) { return; }
    //    if (!GameManager.Instance.surveyStarted || !canInteract) { return; }
    //    if ((holdPoint.currentHeldItem != null && interactionType == InteractionTypes.Pickup)) { return; }
    //    if (!holdPoint.currentHeldItem && interactionType == InteractionTypes.Trash) { return; }

    //    MainUI.Instance.SetInteractText(action, name);
    //    MainUI.Instance.SetInteractVisible(true);
    //}


    public void HoverLook()
    {
        if (!this.enabled) { return; }
        if (!GameManager.Instance.surveyStarted || !canInteract) { return; }
        if ((holdPoint.currentHeldItem != null && interactionType == InteractionTypes.Pickup) ) { return; }
        if (!holdPoint.currentHeldItem && interactionType == InteractionTypes.Trash) { return; }

        MainUI.Instance.SetInteractText(action, name);
        MainUI.Instance.SetInteractVisible(true);

        
    }


    public void ExitLook()
    {
        if (!this.enabled) { return; }
        if (!GameManager.Instance.surveyStarted || !canInteract) { return; }
        if ((holdPoint.currentHeldItem != null && interactionType == InteractionTypes.Pickup)) { return; }
        if (!holdPoint.currentHeldItem && interactionType == InteractionTypes.Trash) { return; }

        MainUI.Instance.SetInteractVisible(false);
    }


    public void Clicked()
    {
        if (!this.enabled) { return; }
        if (!GameManager.Instance.surveyStarted || !canInteract) { return; }
        
        switch (interactionType)
        {
            case InteractionTypes.Pickup:
                if (holdPoint.currentHeldItem == null)
                {
                    isHeld = true;
                    holdPoint.currentHeldItem = this;
                }
                break;
            case InteractionTypes.Trash:
                if (holdPoint.currentHeldItem != null)
                {
                    Destroy(holdPoint.currentHeldItem.gameObject);
                    holdPoint.currentHeldItem = null;

                    GetComponent<Animator>().Play("Bounce");
                }
                break;
            case InteractionTypes.Button:
                try { gameObject.BroadcastMessage("ListenButtonPush"); } catch(Exception e) { }
                try { gameObject.SendMessageUpwards("ListenButtonPush"); } catch(Exception e) { }
                break;
            case InteractionTypes.Folder:
                gameObject.BroadcastMessage("Open");
                Destroy(gameObject.GetComponent<Interactable>());
                break;
            case InteractionTypes.OpenClose:
                if (isOpen)
                    GetComponent<Animator>().CrossFade("Close", 0.5f, -1, 0);
                else
                    GetComponent<Animator>().CrossFade("Open", 0.5f, -1, 0);
                isOpen = !isOpen;
                break;
            case InteractionTypes.Look:
                this.enabled = false;
                StartCoroutine(SendPlayerToPoint());

                break;
        }
        MainUI.Instance.SetInteractVisible(false);

    }


    void PlaySoundEffect()
    {
        AudioSource.PlayClipAtPoint(soundEffect, transform.position);
    }


    private IEnumerator SendPlayerToPoint()
    {
        MainUI.Instance.ScreenFade(true);

        while (!MainUI.Instance.screenIsBlack)
            yield return null;

        Camera.main.gameObject.transform.position = lookPoint.transform.position;

        MainUI.Instance.ScreenFade(false);

    }


    //private void OnMouseExit()
    //{
    //    GetComponent<Renderer>().material = startMaterial;
    //}

}
