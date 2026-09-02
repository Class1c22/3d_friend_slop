using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine;

// Повісити на КОРІНЬ префабу гравця (mainhero_animated), поруч з PhotonView.
// Один раз, при спавні, вимикає всі локальні системи керування (рух, камеру,
// підбір предметів, дихання, UI інвентаря) на копіях, що належать ІНШИМ гравцям.
//
// Без цього кожна заспавнена копія (включно з чужими) читає локальний Input
// цього клієнта - тобто WASD/миша/E керують ВСІМА аватарами в сцені одночасно,
// а UI кисню показувався б навіть за чужий кисень.
//
// ДОДАНО: захист від самопрослуховування голосу (voice self-echo). На СВОЄМУ
// (IsMine) аватарі примусово вимикаємо Recorder.DebugEchoMode і глушимо будь-який
// Speaker/AudioSource, які могли б програвати тобі назад твій же мікрофон -
// незалежно від того, чи забули вимкнути "Debug Echo" вручну в інспекторі.
[RequireComponent(typeof(PhotonView))]
public class PlayerRig : MonoBehaviourPun
{
    [Header("Скрипти, що мають працювати ЛИШЕ на своєму (IsMine) аватарі")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private FirstPersonCamera firstPersonCamera;
    [SerializeField] private PlayerPickup playerPickup;
    [SerializeField] private PlayerBreath playerBreath;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    [Header("UI, який мають бачити тільки ми самі (напр. Canvas інвентаря, бар кисню)")]
    [SerializeField] private GameObject[] localOnlyUI;

    [Header("Голос (захист від самопрослуховування)")]
    [Tooltip("Recorder цього гравця. Якщо не задано - буде знайдений автоматично через GetComponentInChildren.")]
    [SerializeField] private Recorder voiceRecorder;

    void Awake()
    {
        bool mine = photonView.IsMine;

        if (playerController != null) playerController.enabled = mine;
        if (firstPersonCamera != null) firstPersonCamera.enabled = mine;
        if (playerPickup != null) playerPickup.enabled = mine;
        if (playerBreath != null) playerBreath.enabled = mine;
        if (playerCamera != null) playerCamera.gameObject.SetActive(mine);
        if (audioListener != null) audioListener.enabled = mine;

        if (localOnlyUI != null)
        {
            foreach (var ui in localOnlyUI)
                if (ui != null) ui.SetActive(mine);
        }

        // HandAnimatorController НЕ вимикаємо повністю - руки чужих гравців
        // мають рухатись (їх параметри Animator прийдуть по мережі через
        // Photon Animator View). Але свій локальний Input у ньому теж
        // треба заглушити на чужих копіях - це зроблено всередині самого
        // HandAnimatorController.cs через перевірку photonView.IsMine.

        if (mine)
            FixSelfVoiceEcho();
    }

    /// <summary>
    /// Гарантує, що СВІЙ ЖЕ мікрофон ніколи не програється тобі назад:
    /// - примусово вимикає Recorder.DebugEchoMode (класична причина
    ///   "чую сам себе" при тестуванні голосового чату);
    /// - глушить (mute) будь-який Speaker/AudioSource, знайдений на власному
    ///   аватарі - Speaker призначений відтворювати голос ІНШИХ гравців,
    ///   а не свій власний.
    /// </summary>
    private void FixSelfVoiceEcho()
    {
        if (voiceRecorder == null)
            voiceRecorder = GetComponentInChildren<Recorder>();

        if (voiceRecorder != null && voiceRecorder.DebugEchoMode)
        {
            Debug.LogWarning("[PlayerRig] Recorder.DebugEchoMode був увімкнений на своєму аватарі - вимикаю, щоб не чути власний голос.");
            voiceRecorder.DebugEchoMode = false;
        }

        // Speaker на СВОЄМУ ж аватарі (не на чужих!) - зайве джерело self-echo.
        // Speaker потрібен лише на копіях ІНШИХ гравців, щоб чути ЇХНІЙ голос.
        Speaker[] speakersOnSelf = GetComponentsInChildren<Speaker>(true);
        foreach (var speaker in speakersOnSelf)
        {
            var src = speaker.GetComponent<AudioSource>();
            if (src != null)
            {
                src.mute = true;
                src.volume = 0f;
                Debug.LogWarning($"[PlayerRig] Знайдено Speaker '{speaker.name}' на власному аватарі - заглушено, щоб не чути себе.");
            }
        }
    }
}