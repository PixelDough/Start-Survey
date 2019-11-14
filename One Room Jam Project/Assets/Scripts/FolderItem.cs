using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class FolderItem : MonoBehaviour
{

    [SerializeField] private RawImage image;
    [SerializeField] private Transform topOpener;


    IEnumerator Start()
    {
        string name = System.Environment.UserName;


        string[] FileList = Directory.GetFiles(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "*.png");
        //string[] FileList = Directory.GetFiles(@"C:/Users/owner/AppData/Roaming/Microsoft/Windows/AccountPictures");
        if (FileList.Length <= 0)
            FileList = Directory.GetFiles(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures), "*.png");

        if (FileList.Length > 0)
        {
            FileInfo file = new FileInfo(FileList[0]);

            WWW www = new WWW(FileList[Random.Range(0, FileList.Length)]);
            while (!www.isDone)
                yield return null;
            //image.sprite = Sprite.Create(www.texture, new Rect(0.0f, 0.0f, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f), 100.0f);
            image.texture = www.texture;
            image.texture.filterMode = FilterMode.Point;
            //System.Environment.SpecialFolder.Windows + @"/AccountPictures";
        }
    }


    public void Open()
    {
        StartCoroutine(OpenAnimation());
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
