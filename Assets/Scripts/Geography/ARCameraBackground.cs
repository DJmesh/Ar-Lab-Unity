using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ARLab.Geography
{
    /// <summary>
    /// Gerencia o Pass-through da Câmera do Smartphone (Realidade Aumentada).
    /// Projeta o feed de vídeo da câmera traseira do celular ao vivo em tela cheia (100% Full Bleed)
    /// atrás do Globo Terrestre 3D, permitindo que a criança visualize o globo flutuando
    /// diretamente sobre a carteira escolar ou sala de aula sem nenhuma borda preta ou azul.
    /// </summary>
    public class ARCameraBackground : MonoBehaviour
    {
        public static ARCameraBackground Instance { get; private set; }

        [Header("Configurações da Câmera")]
        public bool autoStartOnMobile = true;
        public int requestedWidth = 1920;
        public int requestedHeight = 1080;
        public int requestedFPS = 30;

        [Header("Referências")]
        public Camera targetCamera;
        public GameObject backgroundCanvasObject;
        public RawImage cameraRawImage;

        private WebCamTexture webCamTexture;
        private bool isARActive = false;
        private Color defaultBackgroundColor = new Color(0.04f, 0.08f, 0.16f, 1.0f);

        public bool IsARActive => isARActive;
        public System.Action<bool> OnARStateChanged;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            EnsureBackgroundCanvas();
        }

        void Start()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (autoStartOnMobile)
            {
                StartCoroutine(RequestPermissionAndStart());
            }
            #endif
        }

        private void EnsureBackgroundCanvas()
        {
            if (targetCamera == null) targetCamera = Camera.main;

            if (backgroundCanvasObject == null)
            {
                backgroundCanvasObject = GameObject.Find("ARLab_CameraBackgroundCanvas");
            }

            if (backgroundCanvasObject == null)
            {
                backgroundCanvasObject = new GameObject("ARLab_CameraBackgroundCanvas");
                var canvas = backgroundCanvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = targetCamera;
                canvas.planeDistance = 45f; // Atrás do globo 3D (globo fica a Z=-6 da câmera em Z=-10)
                canvas.sortingOrder = -100; // Renderiza atrás de toda a cena e UI

                var scaler = backgroundCanvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                var rawGo = new GameObject("CameraFeed_RawImage");
                rawGo.transform.SetParent(backgroundCanvasObject.transform, false);

                cameraRawImage = rawGo.AddComponent<RawImage>();
                cameraRawImage.color = Color.white;
                cameraRawImage.raycastTarget = false;

                var rt = rawGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(1080, 1920);
            }
            else
            {
                if (cameraRawImage == null)
                {
                    cameraRawImage = backgroundCanvasObject.GetComponentInChildren<RawImage>(true);
                }
            }

            // Remove AspectRatioFitter antigo se existir para evitar conflito de rotação
            if (cameraRawImage != null)
            {
                var oldFitter = cameraRawImage.GetComponent<AspectRatioFitter>();
                if (oldFitter != null)
                {
                    DestroyImmediate(oldFitter);
                }
            }

            backgroundCanvasObject.SetActive(isARActive);
        }

        public void ToggleARCamera()
        {
            if (isARActive)
            {
                DisableARCamera();
            }
            else
            {
                EnableARCamera();
            }
        }

        public void EnableARCamera()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                StartCoroutine(RequestPermissionAndStart());
                return;
            }
            #endif

            StartCoroutine(StartCameraFeed());
        }

        public void DisableARCamera()
        {
            StopAllCoroutines();

            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }

            isARActive = false;

            if (backgroundCanvasObject != null)
            {
                backgroundCanvasObject.SetActive(false);
            }

            if (targetCamera != null)
            {
                targetCamera.clearFlags = CameraClearFlags.SolidColor;
                targetCamera.backgroundColor = defaultBackgroundColor;
            }

            OnARStateChanged?.Invoke(false);
            Debug.Log("<color=#00E5FF><b>[AR Lab]</b></color> Modo Espaço Ativado (Câmera RA Pausada).");
        }

        private IEnumerator RequestPermissionAndStart()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                yield return new WaitForSeconds(0.5f);
            }

            int timeout = 20;
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera) && timeout > 0)
            {
                yield return new WaitForSeconds(0.2f);
                timeout--;
            }

            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                StartCoroutine(StartCameraFeed());
            }
            else
            {
                Debug.LogWarning("[AR Lab] Permissão da câmera não concedida pelo usuário.");
            }
            #else
            yield return StartCameraFeed();
            #endif
        }

        private IEnumerator StartCameraFeed()
        {
            EnsureBackgroundCanvas();

            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogWarning("[AR Lab] Nenhum dispositivo de câmera detectado. Usando modo simulação de fundo.");
                EnableSimulatedBackground();
                yield break;
            }

            // Seleciona preferencialmente a câmera traseira do smartphone
            string selectedDeviceName = WebCamTexture.devices[0].name;
            foreach (var device in WebCamTexture.devices)
            {
                if (!device.isFrontFacing)
                {
                    selectedDeviceName = device.name;
                    break;
                }
            }

            if (webCamTexture == null || webCamTexture.deviceName != selectedDeviceName)
            {
                webCamTexture = new WebCamTexture(selectedDeviceName, requestedWidth, requestedHeight, requestedFPS);
            }

            if (!webCamTexture.isPlaying)
            {
                webCamTexture.Play();
            }

            // Aguarda inicialização real dos frames da câmera
            float timer = 0f;
            while (webCamTexture.width <= 16 && timer < 3f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (cameraRawImage != null)
            {
                cameraRawImage.texture = webCamTexture;
                cameraRawImage.color = Color.white;
                UpdateCameraOrientationAndAspect();
            }

            if (backgroundCanvasObject != null)
            {
                backgroundCanvasObject.SetActive(true);
            }

            if (targetCamera != null)
            {
                targetCamera.clearFlags = CameraClearFlags.SolidColor;
                targetCamera.backgroundColor = Color.black;
            }

            isARActive = true;
            OnARStateChanged?.Invoke(true);
            Debug.Log($"<color=#4CAF50><b>[AR Lab SUCESSO]</b></color> Câmera RA ativada em tela cheia! Dispositivo: {selectedDeviceName} ({webCamTexture.width}x{webCamTexture.height})");
        }

        private void EnableSimulatedBackground()
        {
            EnsureBackgroundCanvas();
            if (backgroundCanvasObject != null)
            {
                backgroundCanvasObject.SetActive(true);
            }

            if (cameraRawImage != null)
            {
                cameraRawImage.color = new Color(0.10f, 0.15f, 0.22f, 1f);
            }

            if (targetCamera != null)
            {
                targetCamera.clearFlags = CameraClearFlags.SolidColor;
                targetCamera.backgroundColor = Color.black;
            }

            isARActive = true;
            OnARStateChanged?.Invoke(true);
        }

        void Update()
        {
            if (isARActive && webCamTexture != null && webCamTexture.isPlaying)
            {
                UpdateCameraOrientationAndAspect();
            }
        }

        private void UpdateCameraOrientationAndAspect()
        {
            if (cameraRawImage == null || webCamTexture == null) return;

            // Remove AspectRatioFitter se ainda estiver presente
            var fitter = cameraRawImage.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                DestroyImmediate(fitter);
            }

            int videoRotation = webCamTexture.videoRotationAngle;
            float texW = webCamTexture.width > 16 ? webCamTexture.width : requestedWidth;
            float texH = webCamTexture.height > 16 ? webCamTexture.height : requestedHeight;

            RectTransform parentRT = backgroundCanvasObject != null ? backgroundCanvasObject.GetComponent<RectTransform>() : null;
            float screenW = (parentRT != null && parentRT.rect.width > 0) ? parentRT.rect.width : Screen.width;
            float screenH = (parentRT != null && parentRT.rect.height > 0) ? parentRT.rect.height : Screen.height;

            RectTransform rt = cameraRawImage.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0, 0, -videoRotation);

            float scaleY = webCamTexture.videoVerticallyMirrored ? -1f : 1f;
            rt.localScale = new Vector3(1f, scaleY, 1f);

            // Garante preenchimento 100% de tela cheia sem NENHUMA borda preta ou azul
            if (videoRotation == 90 || videoRotation == 270)
            {
                // Em 90° ou 270° (Modo retrato na maioria dos celulares Android), 
                // a largura da câmera se alinha com a altura da tela!
                float scale = Mathf.Max(screenH / texW, screenW / texH);
                rt.sizeDelta = new Vector2(texW * scale, texH * scale);
            }
            else
            {
                float scale = Mathf.Max(screenW / texW, screenH / texH);
                rt.sizeDelta = new Vector2(texW * scale, texH * scale);
            }
        }

        void OnDisable()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
        }

        void OnDestroy()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                Destroy(webCamTexture);
            }
        }
    }
}
