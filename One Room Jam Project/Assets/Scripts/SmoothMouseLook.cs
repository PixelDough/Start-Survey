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


    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
            rb.freezeRotation = true;
        originalRotation = transform.localRotation;

        Cursor.lockState = CursorLockMode.Locked;
    }


    void Update()
    {
        if (axes == RotationAxes.MouseXAndY)
        {

            rotationY += Input.GetAxis("Mouse Y") * sensitivityY * Time.deltaTime;
            rotationX += Input.GetAxis("Mouse X") * sensitivityX * Time.deltaTime;

            rotationY = ClampAngle(rotationY, minimumY, maximumY);
            rotationX = ClampAngle(rotationX, minimumX, maximumX);

            Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);
            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);

            transform.localRotation = originalRotation * xQuaternion * yQuaternion;
        }
        //else if (axes == RotationAxes.MouseX)
        //{
        //    rotAverageX = 0f;

        //    rotationX += Input.GetAxis("Mouse X") * sensitivityX;

        //    rotArrayX.Add(rotationX);

        //    if (rotArrayX.Count >= frameCounter)
        //    {
        //        rotArrayX.RemoveAt(0);
        //    }
        //    for (int i = 0; i < rotArrayX.Count; i++)
        //    {
        //        rotAverageX += rotArrayX[i];
        //    }
        //    rotAverageX /= rotArrayX.Count;

        //    rotAverageX = ClampAngle(rotAverageX, minimumX, maximumX);

        //    Quaternion xQuaternion = Quaternion.AngleAxis(rotAverageX, Vector3.up);
        //    transform.localRotation = originalRotation * xQuaternion;
        //}
        //else
        //{
        //    rotAverageY = 0f;

        //    rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

        //    rotArrayY.Add(rotationY);

        //    if (rotArrayY.Count >= frameCounter)
        //    {
        //        rotArrayY.RemoveAt(0);
        //    }
        //    for (int j = 0; j < rotArrayY.Count; j++)
        //    {
        //        rotAverageY += rotArrayY[j];
        //    }
        //    rotAverageY /= rotArrayY.Count;

        //    rotAverageY = ClampAngle(rotAverageY, minimumY, maximumY);

        //    Quaternion yQuaternion = Quaternion.AngleAxis(rotAverageY, Vector3.left);
        //    transform.localRotation = originalRotation * yQuaternion;
        //}



        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //Cursor.lockState = CursorLockMode.None;
            //Cursor.visible = true;
        }
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
}