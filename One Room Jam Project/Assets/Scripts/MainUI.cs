using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainUI : Singleton<MainUI>
{

    [SerializeField] TextMeshPro interactText;
    [SerializeField] public TextMeshPro dialogText;

    [SerializeField] public Animator animator;
    [SerializeField] public CanvasGroup blackout;
    [HideInInspector] public bool screenIsBlack = false;

    [SerializeField] public AudioSource audioSource;

    private bool visible = false;


    private void Awake()
    {
        SetInteractText("", "");
        SetDialogText("");

        
    }


    private void Start()
    {
        animator = GetComponent<Animator>();
    }


    public void SetInteractText(string action, string name)
    {
        interactText.text = action;
    }

    public void SetDialogText(string text)
    {
        dialogText.text = text;
    }

    public void SetInteractVisible(bool isVisible)
    {
        if (isVisible == visible) { return; }

        if (isVisible)
            animator.CrossFadeInFixedTime("In", .1f, -1, 0);
        else
            animator.CrossFadeInFixedTime("Out", .1f, -1, 0);

        visible = isVisible;
    }


    public void ScreenFade(bool blackout)
    {
        StopCoroutine(FadeScreenUntil(blackout ? 1 : 0));
        StartCoroutine(FadeScreenUntil(blackout ? 1 : 0));

    }


    private IEnumerator FadeScreenUntil(int target)
    {
        while (blackout.alpha != target)
        {
            blackout.alpha = Mathf.MoveTowards(blackout.alpha, target, 2f * Time.deltaTime);
            yield return null;
        }

        screenIsBlack = false;
        if (blackout.alpha == 1f) screenIsBlack = true;

        yield return null;
    }
}
