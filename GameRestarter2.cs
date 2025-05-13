using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using YG;
using Cinemachine;

public class GameRestarter2 : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] playerPrefabs;
    public GameObject[] allyPrefabs;
    public GameObject[] enemyPrefabs;

    [Header("Spawns")]
    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;

    [Header("UI")]
    public Button[] playerButtons;
    public Button[] enemyButtons;
    public Image[] gameResultImages;

    [Header("Joystick & Camera")]
    public Joystick moveJoystick;
    public Joystick attackJoystick;
    public bool isMobileControl;
    public CinemachineVirtualCamera virtualCamera;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip playerDeathSound;
    public AudioClip enemyDeathSound;
    public AudioClip playerRespawnSound;
    public AudioClip backgroundMusic;

    [Header("Environment")]
    public PoisonCloudSpawner poisonSpawner;

    private List<GameObject> playerTeam = new();
    private List<GameObject> enemyTeam = new();

    private int roundsPlayed = 0;
    private int playerWins = 0;
    private int enemyWins = 0;
    private bool monitoringTeams = true;

    private void Start()
    {
        if (YG2.saves.musicOn && backgroundMusic != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        SpawnTeams();
    }

    private void Update()
    {
        if (monitoringTeams)
        {
            CheckTeamStatus();
        }
    }

    public void RegisterPlayerDeath(GameObject player)
    {
        Vector3 spawnPoint = GetSpawnPointForPlayer(player);
        player.transform.position = spawnPoint;
        player.SetActive(true);
    }

    private Vector3 GetSpawnPointForPlayer(GameObject player)
    {
        return new Vector3(0, 0, 0);
    }

    private void SpawnTeams()
    {
        if (poisonSpawner != null)
        {
            poisonSpawner.ResetClouds();
        }

        foreach (var player in playerTeam)
        {
            if (player != null)
            {
                var playerComponent = player.GetComponent<Player>();
                if (playerComponent != null)
                {
                    playerComponent.UpdateBullets(0);
                }
                Destroy(player);
            }
        }
        foreach (var enemy in enemyTeam)
        {
            if (enemy != null) Destroy(enemy);
        }

        playerTeam.Clear();
        enemyTeam.Clear();

        SpawnPlayer();
        SpawnAllies(2);
        SpawnEnemies(3);
    }

    private void ClearTeams()
    {
        foreach (var player in playerTeam)
        {
            if (player != null)
            {
                var playerComponent = player.GetComponent<Player>();
                if (playerComponent != null)
                {
                    playerComponent.UpdateBullets(0);
                }
                Destroy(player);
            }
        }
        foreach (var enemy in enemyTeam)
        {
            if (enemy != null) Destroy(enemy);
        }

        playerTeam.Clear();
        enemyTeam.Clear();
    }

    private void SpawnPlayer()
    {
        int playerIndex = Mathf.Clamp(YG2.saves.currentBrawler, 0, playerPrefabs.Length - 1);
        GameObject playerObj = Instantiate(playerPrefabs[playerIndex], playerSpawnPoints[0].position, Quaternion.identity);

        Player playerComponent = playerObj.GetComponent<Player>();
        ShellyGun gun = playerObj.GetComponentInChildren<ShellyGun>();

        if (gun != null && playerComponent != null)
        {
            gun.Initialize(playerComponent, moveJoystick, attackJoystick, isMobileControl);
            playerComponent.SetJoysticks(moveJoystick, attackJoystick, isMobileControl);
            playerComponent.ForceResetBullets();
        }

        Knockout knockout = playerObj.AddComponent<Knockout>();
        knockout.Initialize(this, true, 0);
        knockout.SetCamera(virtualCamera);

        playerTeam.Add(playerObj);
        if (playerButtons.Length > 0) playerButtons[0].interactable = true;

        foreach (var enemy in enemyTeam)
        {
            if (enemy != null)
            {
                EnemyAI ai = enemy.GetComponent<EnemyAI>();
                if (ai != null) ai.SetPlayerTarget(playerObj.transform);
            }
        }
    }

    private void SpawnAllies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (allyPrefabs.Length == 0)
                continue;

            int prefabIndex = Random.Range(0, allyPrefabs.Length);
            GameObject prefab = allyPrefabs[prefabIndex];
            Transform spawn = playerSpawnPoints[i + 1];

            GameObject ally = Instantiate(prefab, spawn.position, spawn.rotation);
            Knockout k = ally.AddComponent<Knockout>();
            k.Initialize(this, true, i + 1);
            playerTeam.Add(ally);

            EnemyAI ai = ally.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.isPlayerTeam = true;
                ai.attackOtherEnemies = true;
                ai.friendlyFire = false;
            }

            if (playerButtons.Length > i + 1)
                playerButtons[i + 1].interactable = true;
        }
    }

    private void SpawnEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int prefabIndex = Random.Range(0, enemyPrefabs.Length);
            GameObject prefab = enemyPrefabs[prefabIndex];
            Transform spawn = enemySpawnPoints[i];

            GameObject enemy = Instantiate(prefab, spawn.position, Quaternion.identity);
            Knockout k = enemy.AddComponent<Knockout>();
            k.Initialize(this, false, i);
            enemyTeam.Add(enemy);

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.InitializeAI();
            }

            if (enemyButtons.Length > i)
                enemyButtons[i].interactable = true;
        }
    }

    public void OnCharacterKnockedOut(bool isPlayerTeam, int index)
    {
        playerTeam.RemoveAll(p => p == null || !p.activeInHierarchy);
        enemyTeam.RemoveAll(e => e == null || !e.activeInHierarchy);
        CheckTeamStatus();
    }

    private void CheckTeamStatus()
    {
        playerTeam.RemoveAll(p => p == null || !p.activeInHierarchy);
        enemyTeam.RemoveAll(e => e == null || !e.activeInHierarchy);

        int playerAlive = playerTeam.Count;
        int enemyAlive = enemyTeam.Count;

        // Обновляем состояние кнопок на основе оставшихся персонажей
        for (int i = 0; i < playerButtons.Length; i++)
        {
            bool hasAlivePlayer = playerTeam.Count > i;
            playerButtons[i].interactable = hasAlivePlayer;
        }

        for (int i = 0; i < enemyButtons.Length; i++)
        {
            bool hasAliveEnemy = enemyTeam.Count > i;
            enemyButtons[i].interactable = hasAliveEnemy;
        }

        if (playerAlive == 0 || enemyAlive == 0)
        {
            monitoringTeams = false;

            bool playerWon = playerAlive > enemyAlive;

            if (YG2.saves.soundsOn && playerRespawnSound != null)
            {
                audioSource.PlayOneShot(playerRespawnSound);
            }

            EndRound(playerWon);
        }
    }

    private void EndRound(bool playerWon)
    {
        foreach (var playerObj in playerTeam.ToArray())
        {
            if (playerObj != null)
            {
                Player player = playerObj.GetComponent<Player>();
                if (player != null)
                {
                    player.ForceResetBullets();
                    ShellyGun gun = playerObj.GetComponentInChildren<ShellyGun>(true);
                    if (gun != null)
                    {
                        gun.FullReset();
                    }
                }
                Destroy(playerObj);
            }
        }
        playerTeam.Clear();

        foreach (var enemy in enemyTeam.ToArray())
        {
            if (enemy != null) Destroy(enemy);
        }
        enemyTeam.Clear();

        if (playerWon)
        {
            gameResultImages[roundsPlayed].color = Color.blue;
            playerWins++;
        }
        else
        {
            gameResultImages[roundsPlayed].color = Color.red;
            enemyWins++;
        }

        roundsPlayed++;

        if (playerWins >= 2 || enemyWins >= 2)
        {
            int result = playerWins >= 2 ? 0 : 1;
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
        else
        {
            SpawnTeams();
            monitoringTeams = true;
        }
    }
}
