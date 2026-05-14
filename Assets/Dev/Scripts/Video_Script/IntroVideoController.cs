using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RenderTexture videoRenderTexture;

    private VisualElement videoOverlay;
    private VisualElement videoSurface;
    private Button showIntroVideoButton;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("IntroVideoController: UIDocument is missing.");
            return;
        }

        if (videoPlayer == null)
        {
            Debug.LogError("IntroVideoController: VideoPlayer is missing.");
            return;
        }

        if (videoRenderTexture == null)
        {
            Debug.LogError("IntroVideoController: RenderTexture is missing.");
            return;
        }

        var root = uiDocument.rootVisualElement;

        videoOverlay = root.Q<VisualElement>("video_overlay");
        videoSurface = root.Q<VisualElement>("video_surface");
        showIntroVideoButton = root.Q<Button>("show_intro_video_btn");

        if (videoOverlay == null)
        {
            Debug.LogError("Could not find 'video-overlay' in UXML.");
            return;
        }

        if (videoSurface == null)
        {
            Debug.LogError("Could not find 'video-surface' in UXML.");
            return;
        }

        if (showIntroVideoButton == null)
        {
            Debug.LogError("Could not find 'show_intro_video_btn' in UXML.");
            return;
        }

        videoSurface.style.backgroundImage =
            new StyleBackground(Background.FromRenderTexture(videoRenderTexture));

        videoPlayer.targetTexture = videoRenderTexture;

        videoOverlay.style.display = DisplayStyle.None;

        showIntroVideoButton.clicked += OnShowIntroVideoClicked;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnDisable()
    {
        if (showIntroVideoButton != null)
            showIntroVideoButton.clicked -= OnShowIntroVideoClicked;

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    public void ShowIntroVideo()
    {
        videoOverlay.style.display = DisplayStyle.Flex;

        if (videoPlayer.isPrepared)
            videoPlayer.Play();
        else
            videoPlayer.Prepare();
    }

    private void OnShowIntroVideoClicked()
    {
        videoOverlay.style.display = DisplayStyle.Flex;

        if (videoPlayer.isPrepared)
        {
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.Prepare();
        }
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        source.Play();
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        source.Stop();
        videoOverlay.style.display = DisplayStyle.None;
    }
}