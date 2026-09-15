using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButtonSequence : MonoBehaviour
{
    [SerializeField] private Animator jawAnimator;
    [SerializeField] private float delayBeforeClose = 2f;
    [SerializeField] private string closeTrigger = "Close";
    [SerializeField] private string nextSceneName = "Game";

    private bool alreadyStarted;

    // Підключити в inspector до imageButton -> onClick
    public void OnPlayClicked()
    {
        if (alreadyStarted) return; // захист від подвійного кліку
        alreadyStarted = true;

        Invoke(nameof(PlayCloseAnimation), delayBeforeClose);
    }

    private void PlayCloseAnimation()
    {
        jawAnimator.SetTrigger(closeTrigger);
    }

    // Викликається Animation Event-ом в кінці кліпу "loading 1"
    public void OnJawCloseFinished()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}