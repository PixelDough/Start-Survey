using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SurveyPanel : MonoBehaviour
{

    Button buttonYes;
    Button buttonNo;

    // Start is called before the first frame update
    void Start()
    {


        buttonYes.onClick.AddListener(OnClickYes);
        buttonNo.onClick.AddListener(OnClickNo);

    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void OnClickYes()
    {
        OnClickBase();

    }


    void OnClickNo()
    {
        OnClickBase();

    }


    /// The basic button click functionality.
    void OnClickBase()
    {

    }
}
