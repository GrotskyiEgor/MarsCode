using UnityEngine;
using UnityEngine.InputSystem; 

public class MenuButtons : MonoBehaviour
{
    private Camera mainCamera;


    [SerializeField] public GameObject musicUI;
    [SerializeField] public GameObject unMusicUI;
    private bool playMusic = true;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("PlayUI"))
                {
                    Debug.Log("Play");
                }
                else if (hit.collider.CompareTag("QuitUI"))
                {
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                }
                else if (hit.collider.CompareTag("MusicUI"))
                {
                    playMusic = !playMusic;
                    musicUI.SetActive(!musicUI.activeSelf);
                    unMusicUI.SetActive(!musicUI.activeSelf);
                    Debug.Log(playMusic);
                }
                else if (hit.collider.CompareTag("ConfigurationUI"))
                {
                    Debug.Log("Configuration");
                }
            }
        }     
    }
}
