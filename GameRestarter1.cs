using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using YG;

public class GameRestarter1 : MonoBehaviour
{
    [Header("Team Gems Display")]
    [SerializeField] private TMP_Text playerTeamGemsText;
    [SerializeField] private TMP_Text enemyTeamGemsText;
    [SerializeField] private TMP_Text timerText;

    [Header("Timer Settings")]
    [SerializeField] private float gameEndDelay = 15f;
    [SerializeField] private float startCountdownDelay = 2f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject playerWarningSound;
    [SerializeField] private GameObject enemyWarningSound;
    [SerializeField] private AudioClip countdownSound;
    [SerializeField] private AudioClip backgroundMusic;

    [Header("UI Elements")]
    [SerializeField] private GameObject playerTeamWarning;
    [SerializeField] private GameObject enemyTeamWarning;
    [SerializeField] private Image playerTeamHighlight;
    [SerializeField] private Image enemyTeamHighlight;
    [SerializeField] private float highlightIntensity = 1.5f;
    [SerializeField] private float scaleIncrease = 0.1f;
    [SerializeField] private float warningDisplayTime = 1.5f;

    private int playerTeamGems;
    private int enemyTeamGems;
    private bool isTimerRunning;
    private bool gameEnding;
    private Coroutine countdownCoroutine;
    private Coroutine timerCoroutine;
    private Vector3 playerTeamOriginalScale;
    private Vector3 enemyTeamOriginalScale;
    private Color playerTeamOriginalColor;
    private Color enemyTeamOriginalColor;
    private bool currentLeaderIsPlayer;
    private float timerStartTime;
    private int lastDisplayedSecond = -1;
    private float lastTimerCancelTime = -100f;
    private float restartCooldown = 0.5f;
    private bool isCountdownActive = false;
    private bool currentCountdownIsPlayerTeam;
    private bool hasWarningSoundPlayed = false; // Новый флаг для отслеживания воспроизведения звука

    private void Start()
    {
        InitializeUI();
        CacheOriginalValues();
        if (YG2.saves.musicOn)
        {
            StartBackgroundMusic();
        }
    }

    private void Update()
    {
        UpdateGemsCount();
        CheckGameConditions();
        if (isTimerRunning)
        {
            UpdateTimerDisplay();
        }
    }

    private void InitializeUI()
    {
        if (timerText) timerText.gameObject.SetActive(false);
        if (playerTeamWarning) playerTeamWarning.SetActive(false);
        if (enemyTeamWarning) enemyTeamWarning.SetActive(false);
    }

    private void CacheOriginalValues()
    {
        if (playerTeamHighlight)
        {
            playerTeamOriginalScale = playerTeamHighlight.transform.localScale;
            playerTeamOriginalColor = playerTeamHighlight.color;
        }

        if (enemyTeamHighlight)
        {
            enemyTeamOriginalScale = enemyTeamHighlight.transform.localScale;
            enemyTeamOriginalColor = enemyTeamHighlight.color;
        }
    }

    private void StartBackgroundMusic()
    {
        if (audioSource && backgroundMusic && YG2.saves.musicOn)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void UpdateGemsCount()
    {
        int newPlayerGems = CalculateTeamGems(true);
        int newEnemyGems = CalculateTeamGems(false);

        if (newPlayerGems == playerTeamGems && newEnemyGems == enemyTeamGems) return;

        playerTeamGems = newPlayerGems;
        enemyTeamGems = newEnemyGems;
        UpdateGemsUI();
    }

    private int CalculateTeamGems(bool isPlayerTeam)
    {
        int gems = 0;

        if (isPlayerTeam)
        {
            var player = GameObject.FindWithTag("Player");
            if (player && player.TryGetComponent<Player>(out var playerScript))
            {
                gems += playerScript.gems;
            }
        }

        var teamMembers = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var member in teamMembers)
        {
            if (member && member.TryGetComponent<EnemyAI>(out var memberScript) &&
                memberScript.isPlayerTeam == isPlayerTeam)
            {
                gems += memberScript.gems;
            }
        }
        return gems;
    }

    private void UpdateGemsUI()
    {
        if (playerTeamGemsText) playerTeamGemsText.text = playerTeamGems.ToString();
        if (enemyTeamGemsText) enemyTeamGemsText.text = enemyTeamGems.ToString();
    }

    private void CheckGameConditions()
    {
        if (gameEnding) return;

        bool playerWinning = playerTeamGems > enemyTeamGems && playerTeamGems >= 10;
        bool enemyWinning = enemyTeamGems > playerTeamGems && enemyTeamGems >= 10;
        bool scoresEqualAndHigh = playerTeamGems == enemyTeamGems && playerTeamGems >= 10;
        bool leaderChanged = (playerWinning && !currentLeaderIsPlayer) || (enemyWinning && currentLeaderIsPlayer);
        bool someoneWinning = playerWinning || enemyWinning;

        if (someoneWinning && !isTimerRunning && (Time.time - lastTimerCancelTime > restartCooldown))
        {
            currentLeaderIsPlayer = playerWinning;
            StartCountdown(currentLeaderIsPlayer);
        }
        else if (someoneWinning && leaderChanged)
        {
            currentLeaderIsPlayer = playerWinning;
            ResetTimer();
            StartCountdown(currentLeaderIsPlayer);
        }
        else if ((!someoneWinning || scoresEqualAndHigh) && isTimerRunning)
        {
            ResetTimer(); // Сброс при утрате условий победы
        }
    }

    private void StartCountdown(bool isPlayerTeam)
    {
        if (isCountdownActive && currentCountdownIsPlayerTeam == isPlayerTeam)
            return;

        // Сброс, если активен другой таймер
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        ResetTimer();

        currentCountdownIsPlayerTeam = isPlayerTeam;
        isCountdownActive = true;

        countdownCoroutine = StartCoroutine(CountdownSequence(isPlayerTeam));

        if (timerText)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = "15";
        }

        lastDisplayedSecond = 15;
    }

    private IEnumerator CountdownSequence(bool isPlayerTeam)
    {
        GameObject warningPrefab = isPlayerTeam ? playerWarningSound : enemyWarningSound;
        var highlight = isPlayerTeam ? playerTeamHighlight : enemyTeamHighlight;
        GameObject teamWarning = isPlayerTeam ? playerTeamWarning : enemyTeamWarning;

        if (warningPrefab != null && YG2.saves.soundsOn)
        {
            GameObject spawnedWarning = Instantiate(warningPrefab, transform);
            Destroy(spawnedWarning, 2f);
        }

        if (teamWarning != null)
        {
            teamWarning.SetActive(YG2.saves.soundsOn); // Активируем только если звуки включены
            StartCoroutine(HideWarningAfterDelay(teamWarning, warningDisplayTime));
        }

        HighlightTeam(highlight, true);

        yield return new WaitForSeconds(3f);

        if (!IsWinningConditionStillValid(isPlayerTeam))
        {
            ResetTimer();
            yield break;
        }

        timerStartTime = Time.time;
        isTimerRunning = true;

        if (YG2.saves.soundsOn)
        {
            PlaySound(countdownSound);
        }
    }

    private IEnumerator HideWarningAfterDelay(GameObject warning, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (warning != null)
            warning.SetActive(false);
    }

    private bool IsWinningConditionStillValid(bool isPlayerTeam)
    {
        int playerGems = playerTeamGems;
        int enemyGems = enemyTeamGems;

        if (playerGems == enemyGems && playerGems >= 10)
            return false;

        if (isPlayerTeam)
            return playerGems > enemyGems && playerGems >= 10;
        else
            return enemyGems > playerGems && enemyGems >= 10;
    }

    private void UpdateTimerDisplay()
    {
        if (!isTimerRunning) return;

        float elapsed = Time.time - timerStartTime;
        float remainingTime = Mathf.Clamp(gameEndDelay - elapsed, 0f, gameEndDelay);
        int currentSecond = Mathf.CeilToInt(remainingTime);

        if (currentSecond != lastDisplayedSecond)
        {
            lastDisplayedSecond = currentSecond;

            if (timerText)
            {
                timerText.text = currentSecond.ToString();
            }

            if (currentSecond == 0)
            {
                EndGame();
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null || !YG2.saves.soundsOn) return;
        if (audioSource.isPlaying && audioSource.clip == clip) return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void ResetTimer()
    {
        isTimerRunning = false;
        isCountdownActive = false;
        hasWarningSoundPlayed = false;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (timerText)
            timerText.gameObject.SetActive(false);

        if (playerTeamWarning) playerTeamWarning.SetActive(false);
        if (enemyTeamWarning) enemyTeamWarning.SetActive(false);

        HighlightTeam(playerTeamHighlight, false);
        HighlightTeam(enemyTeamHighlight, false);

        StartBackgroundMusic();
    }

    private void HighlightTeam(Image teamHighlight, bool highlight)
    {
        if (!teamHighlight) return;

        Vector3 originalScale = teamHighlight == playerTeamHighlight ? playerTeamOriginalScale : enemyTeamOriginalScale;

        teamHighlight.transform.localScale = highlight ? originalScale * 1.1f : originalScale;
    }

    private void EndGame()
    {
        isTimerRunning = false;
        gameEnding = true;
        int result = playerTeamGems > enemyTeamGems ? 0 : 1;
        BrawlManager brawlManager = FindObjectOfType<BrawlManager>();
        if (brawlManager != null)
        {
            brawlManager.HandleGameResult(result);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}