using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ARLab.Geography
{
    /// <summary>
    /// Marcador interativo 3D (Pin) de cada continente.
    /// Suporta animação de pulsação, clique direto e efeitos visuais
    /// de piscar em Verde (Acerto) ou Vermelho (Erro com correção).
    /// </summary>
    public class ContinentHotspot : MonoBehaviour
    {
        [Header("Dados do Continente")]
        public string continentName = "América do Sul";
        public string animalIcon = "🐆";
        public string animalName = "Onça-Pintada";
        public string funFact = "Floresta Amazônica e animais incríveis!";

        [Header("Cores e Visual")]
        public Color baseColor = Color.yellow;
        private Vector3 originalScale;
        private MeshRenderer meshRenderer;
        private Material instanceMaterial;
        private bool isFlashing = false;

        void Awake()
        {
            originalScale = transform.localScale;
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                if (meshRenderer.sharedMaterial != null)
                {
                    instanceMaterial = new Material(meshRenderer.sharedMaterial);
                }
                else
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Lit");
                    if (s == null) s = Shader.Find("Standard");
                    instanceMaterial = new Material(s);
                }
                instanceMaterial.color = baseColor;
                meshRenderer.material = instanceMaterial;
            }
        }

        void Update()
        {
            if (isFlashing) return;

            // Pulsação suave contínua
            float pulse = 1f + 0.16f * Mathf.Sin(Time.time * 4f);
            transform.localScale = originalScale * pulse;

            // Toque/Clique direto
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                CheckRaycast(mouse.position.ReadValue());
            }

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                CheckRaycast(touch.primaryTouch.position.ReadValue());
            }
        }

        private void CheckRaycast(Vector2 screenPos)
        {
            if (Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    ARLabGameManager.Instance?.OnHotspotClicked(this);
                }
            }
        }

        /// <summary>
        /// Verifica se este continente está voltado de frente para a Câmera
        /// </summary>
        public bool IsFacingCamera()
        {
            if (Camera.main == null) return false;
            var globe = transform.parent;
            Vector3 center = globe != null ? globe.position : Vector3.zero;
            Vector3 dirFromCenter = (transform.position - center).normalized;
            Vector3 dirToCam = (Camera.main.transform.position - center).normalized;

            // Dot > 0.30f = de frente para a tela do usuário
            return Vector3.Dot(dirFromCenter, dirToCam) > 0.30f;
        }

        /// <summary>
        /// Pisca em VERDE brilhante com aumento de escala (Acerto!)
        /// </summary>
        public void FlashSuccess(Action onComplete)
        {
            GetComponentInChildren<Animal3DRotator>()?.PlayHappyJump();
            StartCoroutine(FlashRoutine(new Color(0.1f, 1f, 0.3f), 4, true, onComplete));
        }

        /// <summary>
        /// Pisca em VERMELHO indicando a posição correta após o erro
        /// </summary>
        public void FlashFail(Action onComplete)
        {
            StartCoroutine(FlashRoutine(new Color(1f, 0.15f, 0.15f), 4, false, onComplete));
        }

        private IEnumerator FlashRoutine(Color flashColor, int flashCount, bool success, Action onComplete)
        {
            isFlashing = true;
            Color defaultColor = baseColor;

            for (int i = 0; i < flashCount; i++)
            {
                if (instanceMaterial != null) instanceMaterial.color = flashColor;
                transform.localScale = originalScale * (success ? 1.7f : 1.4f);
                yield return new WaitForSeconds(0.18f);

                if (instanceMaterial != null) instanceMaterial.color = Color.white;
                transform.localScale = originalScale * 0.9f;
                yield return new WaitForSeconds(0.14f);
            }

            if (instanceMaterial != null) instanceMaterial.color = defaultColor;
            transform.localScale = originalScale;
            isFlashing = false;

            onComplete?.Invoke();
        }
    }
}
