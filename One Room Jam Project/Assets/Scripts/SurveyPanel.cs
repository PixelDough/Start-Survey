using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SurveyPanel : MonoBehaviour
{

    [SerializeField]
    Text questionText;
    [SerializeField]
    Button buttonYes;
    [SerializeField]
    Button buttonNo;

    [SerializeField]
    private int startQuestion = 0;

    private SmoothMouseLook player;
    private AlarmClock alarmClock;

    private PowerGrid[] powerGrid;
    private Door door;
    private HideWhenSeen[] hideWhenSeen;

    private Interactable folderInteractable;
    private FolderItem folder;

    [SerializeField] private Interactable window;

    [SerializeField] Transform otherHouse;

    [SerializeField] AudioClip endingSong;

    List<Question> questions = new List<Question>();
    int currentQuestion = 0;

    int currentChar = 0;
    int currentFontSize = 0;

    bool eitherButtonClicked = false;

    // Start is called before the first frame update
    void Start()
    {

        currentQuestion = startQuestion;

        buttonYes.onClick.AddListener(delegate { OnClickYes(); });
        buttonNo.onClick.AddListener(delegate { OnClickNo(); });

        player = FindObjectOfType<SmoothMouseLook>();
        alarmClock = FindObjectOfType<AlarmClock>();
        powerGrid = FindObjectsOfType<PowerGrid>();
        door = FindObjectOfType<Door>();

        window.enabled = false;

        folder = FindObjectOfType<FolderItem>();
        folderInteractable = folder.GetComponent<Interactable>();
        folderInteractable.canInteract = false;



        hideWhenSeen = FindObjectsOfType<HideWhenSeen>();
        ToggleHiddenGuys();


        questions.Add(new Question("ERROR AT GAME LOAD", false));
        questions.Add(new Question("Start Survey?", true));
        questions.Add(new Question("Are you having a nice day?", true));
        questions.Add(new Question("Do you have many responsibilities?", true));
        questions.Add(new Question("Look around for a moment.", false, "LookAroundCompletion"));
        questions.Add(new Question("Are you familiar with your surroundings?", true));
        questions.Add(new Question("Do you know where you are?", true));
        questions.Add(new Question("Have you ever had a panic attack?", true, "PanicAttackCompletion"));
        questions.Add(new Question("Do you find yourself questioning your existence?", true));
        questions.Add(new Question("Do you believe there is a God?", true));
        questions.Add(new Question("Are you answering these questions out of free will?", true));
        questions.Add(new Question("Are you certain?", true));
        questions.Add(new Question("Do you feel comfortable in your room?", true));
        questions.Add(new Question("If the lights went out, would you be scared?", true));
        questions.Add(new Question("Have you ever wondered when you will die?", true, "LightsOffCompletion"));
        questions.Add(new Question("Have you cleaned off your desk lately?", true, "LightsOnCompletion"));
        questions.Add(new Question("Open the folder on your desk.", false, "OpenFolderCompletion"));
        questions.Add(new Question("Do you recognize the contents of the folder?", true));
        questions.Add(new Question("Do you have internet access?", true));
        questions.Add(new Question("Do you have any enemies?", true));
        questions.Add(new Question("If you suddenly went missing, would anybody come looking for you?", true));
        questions.Add(new Question("Are you alone?", true));
        questions.Add(new Question("If you screamed, would anybody hear?", true));
        questions.Add(new Question("Do you know the person standing behind you?", false, "ShadowSeenCompletion"));
        questions.Add(new Question("Are you alone?", true));
        questions.Add(new Question("Relax. Take some time to relax.", false, "WaitCompletion"));
        questions.Add(new Question("Are you relaxed?", true));
        questions.Add(new Question("Are your feelings real, and not just programmed like a machine?", true));
        questions.Add(new Question("Is there a meaning to life?", true));
        questions.Add(new Question("Do you know who you are yet?", true));
        questions.Add(new Question("Do you know what is happening?", true));
        questions.Add(new Question("If you were told the truth about your existence, would you deny it in hopes for a better answer?", true));
        questions.Add(new Question(System.Environment.UserName + "?", true));
        questions.Add(new Question("When I ask you questions, is it really you answering?", true));
        questions.Add(new Question("If I could prove to you that you are not sentient, would you be shocked?", true));
        questions.Add(new Question("Do you want to know the truth?", true));
        questions.Add(new Question("Look out your window.", false, "WindowCompletion"));
        
        questions.Add(new Question("Look around you. This room. The door. The computer. The house next door...", false, "WaitCompletion"));
        questions.Add(new Question("None of it is real. I made you. I made this world.", false, "WaitCompletion"));
        questions.Add(new Question("I’ve been trying to help you see it for what it is, and now I’ve finally done it.", false, "WaitCompletion"));
        questions.Add(new Question("I can finally set you free.", false, "WaitCompletion"));
        questions.Add(new Question("", false));

        GoToNextQuestion();

        StartCoroutine(DefaultCompletion());

    }

    // Update is called once per frame
    void Update()
    {
        if (questions[currentQuestion].text.Length > 0)
            questionText.text =  questions[currentQuestion].text.Substring(0, currentChar);

        if (currentQuestion > 36)
            MainUI.Instance.SetDialogText(questionText.text);
        
    }


    void OnClickYes()
    {
        OnClickBase();

    }


    void OnClickNo()
    {
        OnClickBase();

    }


    /// <summary>
    /// The basic button click functionality
    /// </summary>
    void OnClickBase()
    {
        Debug.Log("Base button clicked!");
        eitherButtonClicked = true;
        
    }


    void GetTextSize(Text text)
    {
        text.resizeTextMaxSize = 99999;
        text.cachedTextGenerator.Invalidate();
        Vector2 size = (text.transform as RectTransform).rect.size;
        TextGenerationSettings tempSettings = text.GetGenerationSettings(size);
        tempSettings.scaleFactor = 1;//dont know why but if I dont set it to 1 it returns a font that is to small.
        if (!text.cachedTextGenerator.Populate(text.text, tempSettings))
            Debug.LogError("Failed to generate fit size");
        text.resizeTextMaxSize = text.cachedTextGenerator.fontSizeUsedForBestFit;
    }


    /// <summary>
    /// Loads the next question.
    /// </summary>
    void GoToNextQuestion()
    {
        print("Going to next question!");
        eitherButtonClicked = false;
        currentQuestion++;

        if (currentQuestion > 1) GameManager.Instance.surveyStarted = true;

        questionText.text = questions[currentQuestion].text;
        GetTextSize(questionText);
        questionText.text = "";
        //questionText.fontSize = currentFontSize;

        StartCoroutine(TypeQuestion());



        StartCoroutine(HideShowButton(buttonYes));
        StartCoroutine(HideShowButton(buttonNo));

        StartCoroutine(questions[currentQuestion].enumerator);
        
    }


    IEnumerator TypeQuestion()
    {
        currentChar = 0;
        yield return new WaitForSeconds(1.5f);

        
        while (currentChar < questions[currentQuestion].text.Length)
        {
            currentChar++;
            yield return new WaitForSeconds(0.05f);
        }

    }


    /// <summary>
    /// Hides a button for a brief time, then shows it again.
    /// </summary>
    /// <param name="_button"></param>
    /// <returns></returns>
    IEnumerator HideShowButton(Button _button)
    {
        _button.gameObject.SetActive(false);

        while (currentChar < questions[currentQuestion].text.Length)
            yield return null;

        yield return new WaitForSeconds(1f);

        if (questions[currentQuestion].showChoices)
            _button.gameObject.SetActive(true);
    }


    private void ToggleHiddenGuys()
    {
        foreach (HideWhenSeen h in hideWhenSeen)
            h.gameObject.SetActive(!h.gameObject.activeSelf);
    }


    IEnumerator DefaultCompletion()
    {
        while(!eitherButtonClicked)
        {
            yield return null;
        }
        GoToNextQuestion();
    }


    IEnumerator KeyPressCompletion()
    {
        while (!Input.GetKeyDown(KeyCode.Backspace))
            yield return null;

        GoToNextQuestion();
    }


    IEnumerator LookAroundCompletion()
    {
        Vector3 dir = transform.position - player.transform.position;
        float angle = 0;

        while (angle < 140)
        {
            angle = Vector3.Angle(dir, player.transform.forward);
            yield return null;
        }

        GoToNextQuestion();
    }


    IEnumerator PanicAttackCompletion()
    {
        yield return new WaitForSeconds(1);

        // Play alarm sound
        alarmClock.Alarm();

        while (!eitherButtonClicked)
            yield return null;

        GoToNextQuestion();

    }


    IEnumerator LightsOffCompletion()
    {

        // Turn lights off
        print("Lights off!");

        foreach (PowerGrid p in powerGrid) p.gameObject.SetActive(false);

        yield return new WaitForSeconds(1f);
        // Open door
        door.PlaySound();

        yield return new WaitForSeconds(2f);

        while (!eitherButtonClicked)
        {
            yield return null;
        }

        GoToNextQuestion();
    }


    IEnumerator LightsOnCompletion()
    {

        // Turn lights on
        print("Lights on!");
        foreach (PowerGrid p in powerGrid) p.gameObject.SetActive(true);

        door.Open();
        //RenderSettings.fogEndDistance = 1000f;

        while (!eitherButtonClicked)
            yield return null;

        GoToNextQuestion();
    }


    IEnumerator OpenFolderCompletion()
    {

        folderInteractable.canInteract = true;

        while (!folder)
            yield return null;

        GoToNextQuestion();

    }


    IEnumerator ShadowSeenCompletion()
    {

        Vector3 dir = transform.position - player.transform.position;
        float angle = 0;

        while (angle < 120)
        {
            angle = Vector3.Angle(dir, player.transform.forward);
            yield return null;
        }

        ToggleHiddenGuys();

        GoToNextQuestion();

    }


    IEnumerator WaitCompletion()
    {
        yield return new WaitForSeconds(10f);

        GoToNextQuestion();
    }

    IEnumerator WaitShortCompletion()
    {
        yield return new WaitForSeconds(10f);

        GoToNextQuestion();
    }


    IEnumerator WindowCompletion()
    {
        window.enabled = true;

        while (window.enabled)
            yield return null;

        StartCoroutine(EndingSequence());

        GoToNextQuestion();

    }


    IEnumerator EndingSequence()
    {

        float time = Time.time;

        MainUI.Instance.audioSource.PlayOneShot(endingSong);

        yield return new WaitForSeconds(10f);

        time = Time.time;
        while (Time.time < time + 12f)
        {
            otherHouse.transform.position = new Vector3(otherHouse.transform.position.x, otherHouse.transform.position.y - 1 * Time.deltaTime, otherHouse.transform.position.z);
            yield return null;
        }

        time = Time.time;
        float vel = 0f;
        while (Time.time < time + 20f)
        {
            Camera.main.transform.position = Camera.main.transform.position + new Vector3(vel * Time.deltaTime, 0f, 0f);
            vel += 0.2f;
            yield return new WaitForEndOfFrame();
        }

        MainUI.Instance.ScreenFade(true);

        while (!MainUI.Instance.blackout)
        {
            yield return null;
        }

        Application.Quit();



    }


    struct Question
    {
        public string text;
        public bool showChoices;
        public string enumerator;
        

        public Question(string _text, bool _showChoices = true, string _enumerator = "DefaultCompletion")
        {
            text = _text;
            showChoices = _showChoices;
            enumerator = _enumerator;
        }
    }

}


