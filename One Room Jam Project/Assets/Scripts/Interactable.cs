using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Interactable : MonoBehaviour
{

    private Material startMaterial;
    private Mesh mesh;

    private bool isHeld = false;
    private HoldPoint holdPoint;

    private Rigidbody rb;
    private Collider collider;

    private Transform startParent;

    [SerializeField] AudioClip soundEffect;
    [SerializeField] string name = "NAME";

    private string action = "Use";

    enum InteractionTypes
    {
        Pickup,
        Button,
        Trash,
        Folder
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
        }
    }


    private void LateUpdate()
    {
        if (!GameManager.Instance.surveyStarted) { return; }
        if (isHeld)
        {
            transform.SetParent(holdPoint.transform);
            transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, 5f * Time.deltaTime);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, 5f * Time.deltaTime);
            if (rb) rb.isKinematic = true;
            collider.enabled = false;
            //DrawOutlineMesh();
        }
        else
        {
            transform.SetParent(startParent);
            if (rb) rb.isKinematic = false;
            collider.enabled = true;
        }
    }


    private void DrawOutlineMesh()
    {
        //Graphics.DrawMesh(mesh, Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale), glowMaterial, 0);
    }


    private void OnMouseOver()
    {
        if (!GameManager.Instance.surveyStarted) { return; }
        if ((holdPoint.currentHeldItem != null && interactionType == InteractionTypes.Pickup) ) { return; }
        if (!holdPoint.currentHeldItem && interactionType == InteractionTypes.Trash) { return; }

        //DrawOutlineMesh();
        MainUI.Instance.SetInteractText(action, name);
    }


    private void OnMouseExit()
    {
        if (!GameManager.Instance.surveyStarted) { return; }
        MainUI.Instance.SetInteractText("", "");
    }


    private void OnMouseDown()
    {
        if (!GameManager.Instance.surveyStarted) { return; }
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
                try { gameObject.BroadcastMessage("ListenButtonPush"); } catch { }
                try { gameObject.SendMessageUpwards("ListenButtonPush"); } catch { }
                break;
            case InteractionTypes.Folder:
                gameObject.BroadcastMessage("Open");
                Destroy(gameObject.GetComponent<Interactable>());
                break;
        }
    }


    void PlaySoundEffect()
    {
        AudioSource.PlayClipAtPoint(soundEffect, transform.position);
    }


    //private void OnMouseExit()
    //{
    //    GetComponent<Renderer>().material = startMaterial;
    //}

}
