using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class UiTutorial : MonoBehaviour
{
    [System.Serializable]
    public class MediaItem
    {
        //public string title;
        public VideoClip videoClip;
        [TextArea(3, 10)]
        public string description;
    }

    [Header("Contenido Multimedia")]
    public MediaItem[] mediaItems;

    [Header("Componentes UI")]
    //public Text titleText; 
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;
    public Text descriptionText;
    public Button previousButton;
    public Button nextButton;
    public Button exitButton;

    private int currentIndex = 0;

    void Start()
    {
        // Configurar VideoPlayer
        videoPlayer.targetTexture = new RenderTexture(
            (int)videoDisplay.rectTransform.rect.width,
            (int)videoDisplay.rectTransform.rect.height, 0);
        videoDisplay.texture = videoPlayer.targetTexture;

        // Asignar eventos
        previousButton.onClick.AddListener(ShowPrevious);
        nextButton.onClick.AddListener(ShowNext);
        exitButton.onClick.AddListener(ExitToMenu);

        ShowCurrentItem();
    }

    void ShowCurrentItem()
    {
        // Actualizar UI con el elemento actual
        //titleText.text = mediaItems[currentIndex].title;
        descriptionText.text = mediaItems[currentIndex].description;
        
        videoPlayer.Stop();
        videoPlayer.clip = mediaItems[currentIndex].videoClip;
        videoPlayer.Play();

        // Actualizar estado de los botones
        previousButton.interactable = (currentIndex > 0);
        nextButton.interactable = (currentIndex < mediaItems.Length - 1);
    }

    void ShowNext()
    {
        if (currentIndex < mediaItems.Length - 1)
        {
            currentIndex++;
            ShowCurrentItem();
        }
    }

    void ShowPrevious()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowCurrentItem();
        }
    }

    void ExitToMenu()
    {
        SceneManager.LoadScene("MenuPrincipal");
    }
}