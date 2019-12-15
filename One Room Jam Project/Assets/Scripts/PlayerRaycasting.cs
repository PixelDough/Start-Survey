using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRaycasting : MonoBehaviour
{

    private float distanceToSee = 10f;

    RaycastHit hit;
    Interactable currentInteractable;
    Interactable lastInteractable;


    private void Update()
    {
        
        Debug.DrawRay(this.transform.position, this.transform.forward * distanceToSee, Color.magenta);

        if (Physics.Raycast(this.transform.position, this.transform.forward, out hit, distanceToSee))
        {

            // Interactable
            if (hit.transform.TryGetComponent(out currentInteractable))
            {
                currentInteractable.HoverLook();
                print("Hit Interactable");
                if (Input.GetMouseButtonDown(0))
                {
                    currentInteractable.Clicked();
                }
                lastInteractable = currentInteractable;

                return;
            }
            else
            {
                if (lastInteractable != currentInteractable && currentInteractable == null)
                {
                    lastInteractable.ExitLook();
                    lastInteractable = null;

                    return;
                }
            }

            // UI Button
            if (hit.transform.TryGetComponent(out Button button))
            {
                print("Hit Button");
                if (Input.GetMouseButtonDown(0))
                {
                    button.SendMessageUpwards("OnClickBase");
                }
                return;
            }



        }

    }

}
