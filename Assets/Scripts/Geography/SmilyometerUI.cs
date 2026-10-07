using UnityEngine;
using UnityEngine.UI;

namespace ARLab.Geography
{
    /// <summary>
    /// Apresenta a escala pictórica visual 'Smilyometer' (Rapti et al., 2023)
    /// com 5 emoticons ilustrados para autorrelato de satisfação infantil sem necessidade de texto.
    /// </summary>
    public class SmilyometerUI : MonoBehaviour
    {
        public static SmilyometerUI Instance { get; private set; }

        [Header("Painel e Componentes Visuais")]
        public GameObject panelRoot;
        public Button[] ratingButtons; // 5 botões correspondendo a 1 (Terrível) até 5 (Adorei!)
        public AudioClip celebrationSound;
        private AudioSource audioSource;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            if (panelRoot != null) panelRoot.SetActive(false);
            SetupButtonListeners();
        }

        private void SetupButtonListeners()
        {
            if (ratingButtons != null)
            {
                for (int i = 0; i < ratingButtons.Length; i++)
                {
                    int ratingValue = i + 1; // 1 a 5
                    ratingButtons[i].onClick.AddListener(() => OnRatingSelected(ratingValue));
                }
            }
        }

        public void ShowSmilyometer()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void OnRatingSelected(int rating)
        {
            Debug.Log($"[Smilyometer] Criança selecionou a nota: {rating} de 5");
            TelemetryLogger.Instance?.RegisterSmilyometerRating(rating);

            if (celebrationSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(celebrationSound);
            }

            // Oculta após escolha
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}
