using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ARLab.Geography
{
    /// <summary>
    /// Gerencia a manipulação tátil intuitiva do globo terrestre:
    /// - Rotação livre 3D com inércia física (1 dedo).
    /// - Zoom suave por movimento de pinça (Pinch-to-Zoom com 2 dedos).
    /// - Zoom por scroll do mouse no Editor e Simulator.
    /// - Rotação suave ociosa quando ninguém está interagindo.
    /// </summary>
    public class GlobeController : MonoBehaviour
    {
        [Header("Configurações de Rotação")]
        [Tooltip("Sensibilidade ao arrasto com 1 dedo")]
        public float rotationSpeed = 0.45f;

        [Tooltip("Suavização da inércia após soltar (0 a 1)")]
        [Range(0.8f, 0.98f)]
        public float damping = 0.94f;

        [Tooltip("Velocidade de rotação suave ociosa")]
        public float idleRotationSpeed = 10f;

        [Header("Configurações de Escala e Pinça (Pinch)")]
        public float minScale = 0.75f;
        public float maxScale = 2.10f;
        [Tooltip("Sensibilidade do movimento de pinça para zoom")]
        public float pinchZoomSpeed = 0.0035f;
        public float mouseZoomSpeed = 0.002f;

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

            // 1. Telas Sensíveis ao Toque via New Input System (Android e Device Simulator)
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touches = touchscreen.touches;
                TouchControl t0 = null;
                TouchControl t1 = null;

                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].press.isPressed)
                    {
                        if (t0 == null) t0 = touches[i];
                        else if (t1 == null) { t1 = touches[i]; break; }
                    }
                }

                // DETECÇÃO DE PINÇA: 2 dedos na tela simultaneamente
                if (t0 != null && t1 != null)
                {
                    hadInput = true;
                    isInteracting = true;
                    idleTimer = 0f;

                    Vector2 pos0 = t0.position.ReadValue();
                    Vector2 pos1 = t1.position.ReadValue();
                    Vector2 prevPos0 = pos0 - t0.delta.ReadValue();
                    Vector2 prevPos1 = pos1 - t1.delta.ReadValue();

                    float curDist = Vector2.Distance(pos0, pos1);
                    float prevDist = Vector2.Distance(prevPos0, prevPos1);
                    float deltaDist = curDist - prevDist;

                    if (Mathf.Abs(deltaDist) > 0.05f)
                    {
                        float newScale = Mathf.Clamp(transform.localScale.x + (deltaDist * pinchZoomSpeed), minScale, maxScale);
                        transform.localScale = Vector3.one * newScale;
                    }

                    // Zera inércia rotacional durante pinça para evitar rotações indesejadas
                    angularVelocity = Vector2.zero;
                }
                // ROTAÇÃO: 1 dedo na tela
                else if (t0 != null)
                {
                    hadInput = true;
                    isInteracting = true;
                    idleTimer = 0f;

                    Vector2 delta = t0.delta.ReadValue();
                    if (delta.sqrMagnitude > 0.01f)
                    {
                        angularVelocity = new Vector2(-delta.y, -delta.x) * (rotationSpeed * 0.4f);
                    }
                }
            }

            // 2. Fallback para Input Clássico (compatibilidade total com todos os drivers Android)
            if (!hadInput && Input.touchCount >= 2)
            {
                UnityEngine.Touch touchZero = Input.GetTouch(0);
                UnityEngine.Touch touchOne = Input.GetTouch(1);

                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
                float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

                if (Mathf.Abs(deltaMagnitudeDiff) > 0.05f)
                {
                    hadInput = true;
                    isInteracting = true;
                    idleTimer = 0f;

                    float newScale = Mathf.Clamp(transform.localScale.x + (deltaMagnitudeDiff * pinchZoomSpeed), minScale, maxScale);
                    transform.localScale = Vector3.one * newScale;
                }
                angularVelocity = Vector2.zero;
            }
            else if (!hadInput && Input.touchCount == 1)
            {
                UnityEngine.Touch touch = Input.GetTouch(0);
                if (touch.phase == UnityEngine.TouchPhase.Moved)
                {
                    hadInput = true;
                    isInteracting = true;
                    idleTimer = 0f;

                    Vector2 delta = touch.deltaPosition;
                    if (delta.sqrMagnitude > 0.01f)
                    {
                        angularVelocity = new Vector2(-delta.y, -delta.x) * (rotationSpeed * 0.4f);
                    }
                }
            }

            // 3. Mouse (Unity Editor e Simulator)
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
                    float newScale = Mathf.Clamp(transform.localScale.x + (scroll.y * mouseZoomSpeed), minScale, maxScale);
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
                // Rotação tridimensional livre com amortecimento suave
                transform.Rotate(Vector3.up, angularVelocity.y * Time.deltaTime * 60f, Space.World);
                transform.Rotate(Vector3.right, angularVelocity.x * Time.deltaTime * 60f, Space.World);
                angularVelocity *= damping;
            }
            else if (idleTimer > 1.5f)
            {
                // Rotação suave contínua quando ocioso
                transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime, Space.World);
            }
        }

        /// <summary>
        /// Aumenta suavemente o tamanho do globo terrestre dentro dos limites seguros
        /// </summary>
        public void ZoomIn(float step = 0.15f)
        {
            float newScale = Mathf.Clamp(transform.localScale.x + step, minScale, maxScale);
            transform.localScale = Vector3.one * newScale;
            idleTimer = 0f;
        }

        /// <summary>
        /// Diminui suavemente o tamanho do globo terrestre dentro dos limites seguros
        /// </summary>
        public void ZoomOut(float step = 0.15f)
        {
            float newScale = Mathf.Clamp(transform.localScale.x - step, minScale, maxScale);
            transform.localScale = Vector3.one * newScale;
            idleTimer = 0f;
        }
    }

}
