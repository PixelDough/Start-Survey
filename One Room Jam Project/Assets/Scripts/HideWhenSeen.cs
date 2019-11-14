using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideWhenSeen : MonoBehaviour
{

    [SerializeField]
    private Transform targetSeenPoint;

    private Renderer renderer;
    private bool isSeen = false;
    private AudioSource audioSource;

    private void Start()
    {
        renderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();

        if (!targetSeenPoint) targetSeenPoint = transform;
    }

    private void Update()
    {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(targetSeenPoint.position);
        bool visible = screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

        if (visible && !isSeen)
        {
            StartCoroutine(StartMove());
        }
    }

    private void FixedUpdate()
    {
        if (isSeen)
        {
            transform.position = Vector3.MoveTowards(transform.position, transform.parent.position, 5f * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, transform.parent.rotation, 3f * Time.deltaTime);
        }
    }


    IEnumerator StartMove()
    {
        yield return new WaitForSeconds(0.25f);
        isSeen = true;
        audioSource.Play();
    }

}
