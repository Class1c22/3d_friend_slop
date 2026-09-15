using UnityEngine;

public class QuitGameButton : MonoBehaviour
{
    // Підключити до imageButton -> On Click ()
    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}