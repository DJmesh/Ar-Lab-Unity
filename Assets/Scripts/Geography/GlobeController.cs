using UnityEngine;
using UnityEngine.InputSystem;

namespace ARLab.Geography
{
    /// <summary>
    /// Gerencia a manipulação tátil intuitiva do globo terrestre (rotação 3D e zoom),
    /// compatível com o New Input System da Unity, Mobile Touchscreen e Mouse no Editor/Simulator.
    /// </summary>
    public class GlobeController : MonoBehaviour
    {
        [Header("Configurações de Rotação")]
        [Tooltip("Sensibilidade ao arrasto")]
        public float rotationSpeed = 0.4f;

        [Tooltip("Suavização da inércia após soltar (0 a 1)")]
        [Range(0.8f, 0.98f)]
        public float damping = 0.94f;

        [Tooltip("Velocidade de rotação suave ociosa quando ninguém está tocando")]
        public float idleRotationSpeed = 10f;

        [Header("Configurações de Escala")]
        public float minScale = 0.5f;
        public float maxScale = 5.0f;
        public float zoomSpeed = 0.005f;

        private Vector2 angularVelocity;
        private bool isInteracting = false;
        private float idleTimer = 0f;

        void Update()
        {
            HandleInput();
            ApplyRotation();
        }

        private void HandleInput()
        {
            bool hadInput = false;

            // 1. Telas Sensíveis ao Toque (Mobile e Simulator Touch)
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            {
                hadInput = true;
                isInteracting = true;
                idleTimer = 0f;

                Vector2 delta = touchscreen.primaryTouch.delta.ReadValue();
                if (delta.sqrMagnitude > 0.01f)
                {
                    angularVelocity = new Vector2(-delta.y, -delta.x) * (rotationSpeed * 0.4f);
                }
            }

            // 2. Mouse (Device Simulator e Unity Editor)
            var mouse = Mouse.current;
            if (!hadInput && mouse != null)
            {
                if (mouse.leftButton.isPressed)
                {
                    hadInput = true;
                    isInteracting = true;
                    idleTimer = 0f;

                    Vector2 delta = mouse.delta.ReadValue();
                    if (delta.sqrMagnitude > 0.01f)
                    {
                        angularVelocity = new Vector2(-delta.y, -delta.x) * (rotationSpeed * 0.4f);
                    }
                }
                else if (mouse.leftButton.wasReleasedThisFrame)
                {
                    isInteracting = false;
                }

                // Zoom pelo Scroll do Mouse
                Vector2 scroll = mouse.scroll.ReadValue();
                if (Mathf.Abs(scroll.y) > 0.01f)
                {
                    float newScale = Mathf.Clamp(transform.localScale.x + (scroll.y * 0.002f), minScale, maxScale);
                    transform.localScale = Vector3.one * newScale;
                    idleTimer = 0f;
                }
            }

            if (!hadInput)
            {
                isInteracting = false;
                idleTimer += Time.deltaTime;
            }
        }

        private void ApplyRotation()
        {
            if (isInteracting || angularVelocity.sqrMagnitude > 0.001f)
            {
                // Rotação livre tridimensional com inércia física
                transform.Rotate(Vector3.up, angularVelocity.y * Time.deltaTime * 60f, Space.World);
                transform.Rotate(Vector3.right, angularVelocity.x * Time.deltaTime * 60f, Space.World);
                angularVelocity *= damping;
            }
            else if (idleTimer > 1.5f)
            {
                // Rotação contínua atrativa da Terra
                transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }
}
