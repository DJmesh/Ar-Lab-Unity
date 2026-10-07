using UnityEngine;
using System.Collections;

namespace ARLab.Geography
{
    public enum WorldRegion
    {
        SouthAmerica,
        NorthAmerica,
        Africa,
        Europe,
        Asia,
        Oceania,
        Antarctica
    }

    /// <summary>
    /// Representa um ponto interativo sobre um continente ou região do globo terrestre.
    /// Exibe a fauna regional, anima ao toque e emite locução sonora sem texto para crianças de 4 a 6 anos.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RegionHotspot : MonoBehaviour
    {
        [Header("Dados da Região")]
        public WorldRegion regionType;
        public string regionName = "América do Sul";
        public string featuredAnimal = "Onça-Pintada";
        public string funFactVoiceover = "Na América do Sul temos a Floresta Amazônica e a bela Onça-Pintada!";

        [Header("Feedback Audiovisual")]
        public AudioClip voiceoverClip;
        public AudioClip animalSoundClip;
        public GameObject animal3DModel;
        public ParticleSystem celebrationParticles;

        private Vector3 originalScale;
        private AudioSource audioSource;
        private bool isSelected = false;

        void Awake()
        {
            originalScale = transform.localScale;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.5f; // Áudio semidirecional suave
            }
        }

        /// <summary>
        /// Invocado quando a criança toca diretamente sobre o continente ou animal
        /// </summary>
        public void OnTouchSelect()
        {
            if (isSelected) return;
            StartCoroutine(AnimateSelectionRoutine());

            // Toca áudio descritivo infantil
            if (voiceoverClip != null && audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = voiceoverClip;
                audioSource.Play();
            }

            // Ativa efeito de partículas lúdicas
            if (celebrationParticles != null)
            {
                celebrationParticles.Play();
            }

            // Notifica telemetria e o gerenciador do jogo
            TelemetryLogger.Instance?.RegisterHotspotClick(regionType.ToString());
            ExplorerQuizManager.Instance?.OnRegionSelected(regionType);
        }

        private IEnumerator AnimateSelectionRoutine()
        {
            isSelected = true;

            // Efeito de salto suave (bounce)
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.35f, Mathf.Sin(progress * Mathf.PI));
                yield return null;
            }

            transform.localScale = originalScale;
            yield return new WaitForSeconds(0.4f);
            isSelected = false;
        }

        void OnMouseDown()
        {
            // Permite teste por clique no Unity Editor
            OnTouchSelect();
        }
    }
}
