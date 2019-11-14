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
    private HideWhenSeen hideWhenSeen;

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
        //hideWhenSeen = FindObjectOfType<HideWhenSeen>();
        //hideWhenSeen.gameObject.SetActive(false);

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
        questions.Add(new Question("Open the folder on your desk.", false));

        GoToNextQuestion();

        StartCoroutine(DefaultCompletion());

    }

    // Update is called once per frame
    void Update()
    {
        if (questions[currentQuestion].text.Length > 0)
            questionText.text =  questions[currentQuestion].text.Substring(0, currentChar);
        
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


