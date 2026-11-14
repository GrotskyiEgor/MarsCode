using UnityEngine;
using UnityEngine.Video;

public class VideoEndSwitcher : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private bool started;

    [SerializeField] private string targetSceneName = "Mars";
    [Tooltip("За сколько секунд до конца видео начинать затухание.")]
    [SerializeField] private float fadeLeadTime = 1.5f;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;
    }

    void Update()
    {
        if (started || videoPlayer == null || !videoPlayer.isPlaying) return;
        if (videoPlayer.length <= 0) return;

        if (videoPlayer.time >= videoPlayer.length - fadeLeadTime)
            StartTransition();
    }

    void OnVideoFinished(VideoPlayer vp) => StartTransition();

    void StartTransition()
    {
        if (started) return;
        started = true;

        Debug.Log("Запускаем плавное затухание на сцену: " + targetSceneName);
        SceneTransition.LoadScene(targetSceneName);
    }
}