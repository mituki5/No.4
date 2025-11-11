using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class QuizItem
{
    public string label;         // 問題説明（任意）
    public Sprite questionImage; // 問題画像
    public string answer;        // 正答
    public int points;           // 配点（2,4,6,10）
}

public class QuizController : MonoBehaviour
{
    [Header("問題リスト（Inspectorで順番に26個配置）")]
    public List<QuizItem> quizItems = new List<QuizItem>();

    [Header("UI - パネル")]
    public GameObject titlePanel;
    public Text titleText;
    public GameObject introPanel;
    public Text introText;
    public Text countdownText;

    public GameObject quizPanel;
    public Image questionImage;
    public Text questionNumberText;
    public InputField answerInput;
    public Text pointsText;
    public Text timerText;
    public Slider timeSlider;

    [Header("結果表示")]
    public GameObject resultPanel;
    public Text resultScoreText;
    public Image resultStampImage;
    public List<Sprite> stampSprites;
    public GameObject fullScoreFlowerPrefab;

    [Header("ゲーム設定")]
    public float totalTimeSeconds = 480f;
    public KeyCode startKey = KeyCode.Return;
    public KeyCode nextKey = KeyCode.RightArrow;
    public KeyCode prevKey = KeyCode.LeftArrow;

    // 内部状態
    private int currentIndex = 0;
    private string[] userAnswers;
    private float remainingTime;
    private bool isRunning = false;
    private bool isIntro = false;

    void Start()
    {
        if (quizItems == null) quizItems = new List<QuizItem>();
        userAnswers = new string[Mathf.Max(quizItems.Count, 26)];
        remainingTime = totalTimeSeconds;

        titlePanel.SetActive(true);
        introPanel.SetActive(false);
        quizPanel.SetActive(false);
        resultPanel.SetActive(false);

        EventSystem.current?.SetSelectedGameObject(null);
    }

    void Update()
    {
        if (!isRunning && !isIntro)
        {
            if (Input.GetKeyDown(startKey))
            {
                StartCoroutine(ShowIntroAndCountdown());
            }
        }
        else if (isRunning)
        {
            remainingTime -= Time.deltaTime;
            UpdateTimerUI();

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                isRunning = false;
                StartCoroutine(ShowResultAfterDelay(0.2f));
                return;
            }

            if (Input.GetKeyDown(nextKey))
            {
                SaveCurrentAnswer();
                if (currentIndex < quizItems.Count - 1)
                {
                    currentIndex++;
                    ShowCurrentQuestion();
                }
                else
                {
                    isRunning = false;
                    StartCoroutine(ShowResultAfterDelay(0.1f));
                }
            }
            else if (Input.GetKeyDown(prevKey))
            {
                SaveCurrentAnswer();
                if (currentIndex > 0)
                {
                    currentIndex--;
                    ShowCurrentQuestion();
                }
            }
        }
    }

    IEnumerator ShowIntroAndCountdown()
    {
        titlePanel.SetActive(false);
        introPanel.SetActive(true);
        introText.text = "これから漢字クイズを始めます。\n欠けた字や分かれた部首を見て、正しい漢字を入力してください。\n→で次へ、←で前へ。\n全26問、制限時間内に挑戦！";

        isIntro = true;
        int countdown = 5;
        while (countdown > 0)
        {
            countdownText.text = countdown.ToString();
            yield return new WaitForSeconds(1f);
            countdown--;
        }
        countdownText.text = "0";
        yield return new WaitForSeconds(0.2f);
        introPanel.SetActive(false);
        isIntro = false;
        StartQuiz();
    }

    void StartQuiz()
    {
        if (quizItems.Count < 26)
        {
            Debug.LogWarning("QuizController: quizItems.Count < 26. Inspectorで26個セットしてください。");
        }

        remainingTime = totalTimeSeconds;
        isRunning = true;
        currentIndex = 0;
        quizPanel.SetActive(true);
        resultPanel.SetActive(false);

        ShowCurrentQuestion();
    }

    void ShowCurrentQuestion()
    {
        if (currentIndex < 0) currentIndex = 0;
        if (currentIndex >= quizItems.Count) currentIndex = quizItems.Count - 1;

        QuizItem qi = quizItems[currentIndex];
        questionImage.sprite = qi.questionImage;
        questionNumberText.text = $"No. {currentIndex + 1} / {quizItems.Count}";
        pointsText.text = $"配点: {qi.points}点";

        string val = userAnswers[currentIndex] ?? "";
        answerInput.text = val;

        EventSystem.current.SetSelectedGameObject(answerInput.gameObject);
        answerInput.ActivateInputField();

        UpdateTimerUI();
    }

    void SaveCurrentAnswer()
    {
        if (currentIndex >= 0 && currentIndex < userAnswers.Length)
        {
            userAnswers[currentIndex] = answerInput.text.Trim();
        }
    }

    void UpdateTimerUI()
    {
        timerText.text = $"{Mathf.CeilToInt(remainingTime)}s";
        if (timeSlider != null)
        {
            timeSlider.maxValue = totalTimeSeconds;
            timeSlider.value = remainingTime;
        }
    }

    IEnumerator ShowResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SaveCurrentAnswer();
        quizPanel.SetActive(false);
        ShowResults();
    }

    void ShowResults()
    {
        int total = 0;
        int possible = 0;
        for (int i = 0; i < quizItems.Count; i++)
        {
            QuizItem qi = quizItems[i];
            possible += qi.points;
            string u = (i < userAnswers.Length && userAnswers[i] != null) ? userAnswers[i].Trim() : "";
            string a = (qi.answer != null) ? qi.answer.Trim() : "";

            if (!string.IsNullOrEmpty(u) && u == a)
            {
                total += qi.points;
            }
        }

        resultPanel.SetActive(true);
        resultScoreText.text = $"得点: {total} / {possible}";

        Sprite chosenStamp = null;
        if (total >= possible) // 満点
        {
            if (fullScoreFlowerPrefab != null)
            {
                Instantiate(fullScoreFlowerPrefab, resultPanel.transform, false);
            }
            if (stampSprites.Count > 0) chosenStamp = stampSprites[stampSprites.Count - 1];
        }
        else if (total >= possible * 0.9f)
        {
            if (stampSprites.Count >= 2) chosenStamp = stampSprites[stampSprites.Count - 2];
        }
        else if (total >= possible * 0.75f)
        {
            if (stampSprites.Count >= 3) chosenStamp = stampSprites[Mathf.Max(0, stampSprites.Count - 3)];
        }
        else if (total >= possible * 0.5f)
        {
            if (stampSprites.Count >= 4) chosenStamp = stampSprites[Mathf.Max(0, stampSprites.Count - 4)];
        }
        else
        {
            if (stampSprites.Count > 0) chosenStamp = stampSprites[0];
        }

        resultStampImage.sprite = chosenStamp;
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
