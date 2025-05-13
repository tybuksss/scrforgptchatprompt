using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using YG;

public class GameRestarter : MonoBehaviour
{
    [Header("One vs One Settings")]
    [SerializeField] private GameObject oneVsOneObject;
    [SerializeField] private float scaleTime = 0.25f;
    [SerializeField] private float displayTime = 5f;
    [SerializeField] private float finalScale = 2.5f;

    [Header("Entity Counter")]
    [SerializeField] private TMP_Text entityCounterText;

    [Header("Music Settings")]
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip oneVsOneMusic;
    [SerializeField] private float musicFadeTime = 1f;

    [Header("Sound Effects")]
    [SerializeField] private AudioSource soundEffectsSource;
    [SerializeField] private AudioClip enemyDefeatedSound;

    private bool isOneVsOne = false;
    private float currentScaleTime = 0f;
    private float displayTimer = 0f;
    private bool isDisplayingOneVsOne = false;
    private bool isScalingDown = false;
    private float musicTransitionTimer = 0f;
    private bool isTransitioningMusic = false;
    private bool targetMusicIsOneVsOne = false;
    private int previousEnemyCount = 0;

    private void Start()
    {
        if (oneVsOneObject != null)
        {
            oneVsOneObject.SetActive(false);
            oneVsOneObject.transform.localScale = Vector3.zero;
        }

        if (backgroundMusic != null && normalMusic != null)
        {
            backgroundMusic.clip = normalMusic;
            backgroundMusic.loop = true;
            if (YG2.saves.musicOn)
            {
                backgroundMusic.Play();
            }
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        previousEnemyCount = enemies.Length;

        UpdateEntityCounter();
    }

    private void Update()
    {
        CheckPlayerAndEnemies();
        HandleOneVsOneAnimation();
        HandleOneVsOneDisplayTimer();
        UpdateEntityCounter();
        HandleMusicTransition();
        CheckEnemyCountChange();
    }

    private void CheckEnemyCountChange()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int currentEnemyCount = enemies.Length;

        if (currentEnemyCount < previousEnemyCount && enemyDefeatedSound != null && soundEffectsSource != null && YG2.saves.soundsOn)
        {
            soundEffectsSource.PlayOneShot(enemyDefeatedSound);
        }

        previousEnemyCount = currentEnemyCount;
    }

    private void CheckPlayerAndEnemies()
    {
        GameObject player = GameObject.FindWithTag("Player");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (player == null || enemies.Length == 0)
        {
            RestartScene();
            return;
        }

        bool nowOneVsOne = enemies.Length == 1;

        if (nowOneVsOne && !isOneVsOne)
        {
            StartOneVsOne();
            StartMusicTransition(true);
        }
        else if (!nowOneVsOne && isOneVsOne && !isDisplayingOneVsOne)
        {
            EndOneVsOne();
            StartMusicTransition(false);
        }

        isOneVsOne = nowOneVsOne;
    }

    private void StartMusicTransition(bool toOneVsOneMusic)
    {
        if (backgroundMusic == null || normalMusic == null || oneVsOneMusic == null) return;

        targetMusicIsOneVsOne = toOneVsOneMusic;
        musicTransitionTimer = 0f;
        isTransitioningMusic = true;
    }

    private void HandleMusicTransition()
    {
        if (!isTransitioningMusic || backgroundMusic == null) return;

        musicTransitionTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(musicTransitionTimer / musicFadeTime);

        if (progress < 1f)
        {
            backgroundMusic.volume = 1f - progress;
        }
        else
        {
            backgroundMusic.volume = 0f;
            backgroundMusic.clip = targetMusicIsOneVsOne ? oneVsOneMusic : normalMusic;
            if (YG2.saves.musicOn)
            {
                backgroundMusic.Play();
            }
            isTransitioningMusic = false;

            StartCoroutine(FadeInMusic());
        }
    }

    private System.Collections.IEnumerator FadeInMusic()
    {
        float fadeTimer = 0f;
        while (fadeTimer < musicFadeTime)
        {
            fadeTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(fadeTimer / musicFadeTime);
            backgroundMusic.volume = progress;
            yield return null;
        }
        backgroundMusic.volume = YG2.saves.musicOn ? 1f : 0f;
    }

    private void StartOneVsOne()
    {
        if (oneVsOneObject != null)
        {
            oneVsOneObject.SetActive(true);
            currentScaleTime = 0f;
            displayTimer = 0f;
            isDisplayingOneVsOne = true;
            isScalingDown = false;
        }
    }

    private void EndOneVsOne()
    {
        if (oneVsOneObject != null)
        {
            oneVsOneObject.SetActive(false);
            oneVsOneObject.transform.localScale = Vector3.zero;
            isDisplayingOneVsOne = false;
        }
    }

    private void HandleOneVsOneAnimation()
    {
        if (!isDisplayingOneVsOne || oneVsOneObject == null) return;

        if (!isScalingDown)
        {
            if (currentScaleTime < scaleTime)
            {
                currentScaleTime += Time.deltaTime;
                float progress = Mathf.Clamp01(currentScaleTime / scaleTime);
                float scale = Mathf.Lerp(0.01f, finalScale, progress);
                oneVsOneObject.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
        else
        {
            if (currentScaleTime > 0f)
            {
                currentScaleTime -= Time.deltaTime;
                float progress = Mathf.Clamp01(currentScaleTime / scaleTime);
                float scale = Mathf.Lerp(0.01f, finalScale, progress);
                oneVsOneObject.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                EndOneVsOne();
            }
        }
    }

    private void HandleOneVsOneDisplayTimer()
    {
        if (!isDisplayingOneVsOne || isScalingDown) return;

        displayTimer += Time.deltaTime;

        if (displayTimer >= displayTime - scaleTime)
        {
            isScalingDown = true;
            currentScaleTime = scaleTime;
        }
    }

    private void UpdateEntityCounter()
    {
        if (entityCounterText == null) return;

        GameObject player = GameObject.FindWithTag("Player");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        int playerCount = player != null ? 1 : 0;
        int totalEntities = playerCount + enemies.Length;

        entityCounterText.text = totalEntities.ToString();
    }

    private void RestartScene()
    {
        int place = 10;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            place = 0;
        }

        BrawlManager brawlManager = FindObjectOfType<BrawlManager>();
        if (brawlManager != null)
        {
            brawlManager.HandleGameResult(place);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}