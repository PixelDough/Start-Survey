using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothMouseLook : MonoBehaviour
{
    
    public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
    public RotationAxes axes = RotationAxes.MouseXAndY;
    public float sensitivityX = 15F;
    public float sensitivityY = 15F;

    public float minimumX = -360F;
    public float maximumX = 360F;

    public float minimumY = -60F;
    public float maximumY = 60F;

    float rotationX = 0F;
    float rotationY = 0F;

    Quaternion originalRotation;

    public int zoomFOV = 40;
    private float initialFOV;
    private bool isZoomedIn = false;

    private bool hasFocus = false;


    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
            rb.freezeRotation = true;
        originalRotation = transform.localRotation;

        initialFOV = Camera.main.fieldOfView;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //QualitySettings.vSyncCount = 1;
    }


    void Update()
    {
        if (!hasFocus || Cursor.lockState != CursorLockMode.Locked) return;

        float zoomSpeedMultiplier = 1f;
        if (isZoomedIn) zoomSpeedMultiplier = .5f;

        if (axes == RotationAxes.MouseXAndY)
        {

            rotationY += Input.GetAxis("Mouse Y") * sensitivityY * zoomSpeedMultiplier;
            rotationX += Input.GetAxis("Mouse X") * sensitivityX * zoomSpeedMultiplier;

            rotationY = ClampAngle(rotationY, minimumY, maximumY);
            rotationX = ClampAngle(rotationX, minimumX, maximumX);

            Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);
            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);

            transform.localRotation = originalRotation * xQuaternion * yQuaternion;
        }
        else if (axes == RotationAxes.MouseX)
        {

            rotationX += Input.GetAxis("Mouse X") * sensitivityX * zoomSpeedMultiplier;

            rotationX = ClampAngle(rotationX, minimumX, maximumX);

            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
            transform.localRotation = originalRotation * xQuaternion;
        }
        else
        {
            
            rotationY += Input.GetAxis("Mouse Y") * sensitivityY * zoomSpeedMultiplier;

            rotationY = ClampAngle(rotationY, minimumY, maximumY);

            Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);
            transform.localRotation = originalRotation * yQuaternion;
        }

        isZoomedIn = (Input.GetMouseButton(1));

        float targetZoomValue = initialFOV;
        if (isZoomedIn) targetZoomValue = zoomFOV;
        Camera.main.fieldOfView = Mathf.MoveTowards(Camera.main.fieldOfView, targetZoomValue, 50 * Time.deltaTime);

    }

    public static float ClampAngle(float angle, float min, float max)
    {
        angle = angle % 360;
        if ((angle >= -360F) && (angle <= 360F))
        {
            if (angle < -360F)
            {
                angle += 360F;
            }
            if (angle > 360F)
            {
                angle -= 360F;
            }
        }
        return Mathf.Clamp(angle, min, max);
    }


    private void OnApplicationFocus(bool focus)
    {
        hasFocus = focus;
    }
}