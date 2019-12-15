using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Networking;

public class FolderItem : MonoBehaviour
{

    [SerializeField] private Renderer image;
    [SerializeField] private Transform topOpener;

    [HideInInspector] public bool isOpen = false;

    [HideInInspector] public bool hasImage = true;


    private void Start()
    {
        StartCoroutine(StartItemLoad());
    }


    private void Update()
    {
        if (image != null)
        {
            hasImage = false;
        }
    }


    IEnumerator StartItemLoad()
    {
        string name = System.Environment.UserName;

        string[] FileList = Directory.GetFiles(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "*.png");
        //string[] FileList = Directory.GetFiles(@"C:/Users/owner/AppData/Roaming/Microsoft/Windows/AccountPictures");
        if (FileList.Length <= 0)
            FileList = Directory.GetFiles(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures), "*.png");
        if (FileList.Length > 0)
        {
            byte[] bytes = File.ReadAllBytes(FileList[Random.Range(0, FileList.Length)]);
            Texture2D tex = new Texture2D(1,1);
            tex.LoadImage(bytes);
            image.material.mainTexture = tex;
            image.material.mainTexture.filterMode = FilterMode.Point;
            yield return null;

            //UnityWebRequest www = new UnityWebRequest(FileList[Random.Range(0, FileList.Length)]);
            
            //while (!www.isDone)
            //    yield return null;
            ////image.sprite = Sprite.Create(www.texture, new Rect(0.0f, 0.0f, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f), 100.0f);
            //image.texture.LoadImage(www.texture.GetRawTextureData());
            //image.texture.filterMode = FilterMode.Point;
            ////System.Environment.SpecialFolder.Windows + @"/AccountPictures";
        }
    }


    public void Open()
    {
        StartCoroutine(OpenAnimation());
        isOpen = true;
    }


    private IEnumerator OpenAnimation()
    {

        while(transform.rotation.eulerAngles.z != 100)
        {
            topOpener.localRotation = Quaternion.Slerp(topOpener.localRotation, Quaternion.Euler(topOpener.localRotation.eulerAngles.x, -145f, topOpener.localRotation.eulerAngles.z), 5f * Time.deltaTime);
            yield return null;
        }

    }


}
