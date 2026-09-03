using System.Collections;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Обробляє "смерть" гравця (напр. акула з'їла) БЕЗ переходу в іншу сцену:
/// - ховає візуальну модель гравця;
/// - вимикає скрипти керування;
/// - перемикає камеру гравця на окрему "камеру смерті";
/// - за командою показує Game Over UI.
///
/// Повісити на persona-об'єкт з PhotonView (у твоїй сцені - на "mainhero").
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PlayerDeathHandler : MonoBehaviourPun
{
    [Header("Що ховати/вимикати")]
    public GameObject playerModel;
    public MonoBehaviour[] scriptsToDisable;
    public Collider[] collidersToDisable;

    [Header("Камери (можна лишити порожнім - знайдуться самі за назвою об'єкта)")]
    [Tooltip("Звичайна ігрова камера. Якщо не задано - шукається дочірній об'єкт з ім'ям \"Main Camera\".")]
    public Camera playerCamera;

    [Tooltip("Камера смерті. Якщо не задано - шукається дочірній об'єкт з ім'ям \"deathcamera\".")]
    public Camera deathCamera;

    [Header("Назви об'єктів для автопошуку камер (якщо поля вище порожні)")]
    [SerializeField] private string playerCameraObjectName = "Main Camera";
    [SerializeField] private string deathCameraObjectName = "deathcamera";

    [Header("UI")]
    public GameObject gameOverUI;

    [Tooltip("Ігровий HUD, який треба сховати одночасно зі смертю (напр. oxygenBarRoot з PlayerBreath, інвентар тощо). Без цього UI лишається на екрані навіть коли керування вже вимкнено і показана камера смерті.")]
    public GameObject[] gameplayUI;

    [Header("Острів (фінальний ефект)")]
    [Tooltip("SharkBiteController зі сцени - через нього гравець(і)-смерть запускає миттєве поглинання острова акулою (з анімацією укусу, а не просто зникнення). Якщо не задано - шукається автоматично через FindObjectOfType.")]
    public SharkBiteController sharkBiteController;

    [Tooltip("Затримка (сек) після смерті гравця, перш ніж острів почне зникати (акула кусати).")]
    public float islandDevourStartDelay = 1f;

    private bool isDead;
    public bool IsDead => isDead;

    void Awake()
    {
        ResolveCamerasIfMissing();

        if (sharkBiteController == null)
            sharkBiteController = FindObjectOfType<SharkBiteController>();
    }

    private void ResolveCamerasIfMissing()
    {
        if (playerCamera != null && deathCamera != null) return;

        Camera[] allCameras = GetComponentsInChildren<Camera>(true);

        foreach (var cam in allCameras)
        {
            if (playerCamera == null && cam.gameObject.name == playerCameraObjectName)
                playerCamera = cam;

            if (deathCamera == null && cam.gameObject.name == deathCameraObjectName)
                deathCamera = cam;
        }

        if (playerCamera == null)
            Debug.LogError($"[PlayerDeathHandler] Не знайдено playerCamera (шукав об'єкт \"{playerCameraObjectName}\") і поле в інспекторі порожнє!");

        if (deathCamera == null)
            Debug.LogError($"[PlayerDeathHandler] Не знайдено deathCamera (шукав об'єкт \"{deathCameraObjectName}\") і поле в інспекторі порожнє!");
    }

    void Start()
    {
        if (!photonView.IsMine) return;

        // deathCamera має бути повністю вимкнена, доки гравець живий:
        // і об'єкт (SetActive), і сам компонент Camera (enabled), і AudioListener,
        // інакше або "2 audio listeners" одночасно, або вона просто лишається
        // Camera.enabled = false назавжди й ніколи не почне рендерити, навіть
        // коли пізніше увімкнемо об'єкт.
        if (deathCamera != null)
        {
            deathCamera.enabled = false;
            if (deathCamera.gameObject.activeSelf)
            {
                Debug.LogWarning("[PlayerDeathHandler] deathCamera була активна на старті - вимикаю.");
                deathCamera.gameObject.SetActive(false);
            }

            var deathListener = deathCamera.GetComponent<AudioListener>();
            if (deathListener != null) deathListener.enabled = false;
        }

        if (gameOverUI != null && gameOverUI.activeSelf)
            gameOverUI.SetActive(false);
    }

    public void Die()
    {
        if (isDead) return;
        if (!photonView.IsMine) return;

        photonView.RPC(nameof(RPC_Die), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_Die()
    {
        if (isDead) return;
        isDead = true;

        foreach (var script in scriptsToDisable)
            if (script != null) script.enabled = false;

        foreach (var col in collidersToDisable)
            if (col != null) col.enabled = false;

        if (playerModel != null)
            playerModel.SetActive(false);

        // Ховаємо ігровий HUD тільки на своєму клієнті (photonView.IsMine) -
        // чужі копії цей UI все одно не показують (він і так вимкнений у
        // PlayerRig для не-власника), тож RPC-виклик на всіх клієнтах
        // достатньо просто пропустити для тих, кому нема що ховати.
        if (photonView.IsMine && gameplayUI != null)
        {
            foreach (var ui in gameplayUI)
                if (ui != null) ui.SetActive(false);
        }

        // Коли гравець (або всі гравці) помирає - острів має зникнути (акула
        // його "з'їдає", з анімацією укусу), але не миттєво в той самий кадр,
        // що й смерть гравця, а з невеликою затримкою (islandDevourStartDelay),
        // щоб ці дві події не зливались візуально в одну. RPC_Die виконується
        // на КОЖНОМУ клієнті, тому цю дію ініціює лише MasterClient
        // (DevourWholeIslandNow() сам ще раз підстраховується перевіркою
        // PhotonNetwork.IsMasterClient) - інакше кожен клієнт спробував би
        // розіслати свій власний RPC.
        if (PhotonNetwork.IsMasterClient && sharkBiteController != null)
            StartCoroutine(DevourWholeIslandDelayedRoutine());

        if (photonView.IsMine)
        {
            ResolveCamerasIfMissing();

            Debug.Log($"[PlayerDeathHandler] RPC_Die: playerCamera={(playerCamera != null ? playerCamera.name : "NULL")}, deathCamera={(deathCamera != null ? deathCamera.name : "NULL")}");

            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                playerCamera.gameObject.SetActive(false);
            }

            if (deathCamera != null)
            {
                // ГОЛОВНИЙ ФІКС: SetActive(true) вмикає ОБ'ЄКТ, але не сам
                // компонент Camera, якщо в нього окремо стояло enabled = false.
                // Вмикаємо обидва явно.
                deathCamera.gameObject.SetActive(true);
                deathCamera.enabled = true;

                var deathListener = deathCamera.GetComponent<AudioListener>();
                if (deathListener != null) deathListener.enabled = true;
            }
            else
            {
                Debug.LogError("[PlayerDeathHandler] deathCamera відсутня - камера смерті НЕ увімкнеться!");
            }
        }
    }

    public void ShowGameOver()
    {
        if (!photonView.IsMine) return;
        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        // ФІКС: без цього курсор лишається заблокованим і невидимим
        // (з FirstPersonCamera.Start()), і гравець фізично не може
        // клікнути по кнопках Game Over UI.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowGameOverDelayed(float delay)
    {
        if (!photonView.IsMine) return;
        StartCoroutine(ShowGameOverRoutine(delay));
    }

    private IEnumerator ShowGameOverRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowGameOver();
    }

    private IEnumerator DevourWholeIslandDelayedRoutine()
    {
        yield return new WaitForSeconds(islandDevourStartDelay);
        sharkBiteController.DevourWholeIslandNow();
    }
}