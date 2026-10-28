using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

namespace ARLab.Geography
{
    [Serializable]
    public class ContinentMission
    {
        public string continentName;
        public string animalIcon;
        public string animalName;
        public string questionPrompt;
        public string successFact;
        public ContinentHotspot hotspot;
    }

    /// <summary>
    /// Gerenciador Central do AR Lab:
    /// Interface sofisticada em Glassmorphism com pass-through de Câmera RA em tela cheia (100% Full-Bleed),
    /// Globo Terrestre em proporção perfeita (escala 1.35x), Botão profissional de alternância de Câmera RA com estados
    /// visuais explícitos (Ativar/Desativar), Botão interativo lúdico com olhos animados observando,
    /// sistema animado de Toast Notifications, e 7 missões pedagógicas com validação tridimensional.
    /// </summary>
    public class ARLabGameManager : MonoBehaviour
    {
        public static ARLabGameManager Instance { get; private set; }

        [Header("Referências da Cena")]
        public Transform globeTransform;
        public GlobeController globeController;
        public ARCameraBackground arCameraBackground;

        [Header("Painéis da Interface")]
        public GameObject gameplayHUDPanel;
        public GameObject infoCardPanel;
        public GameObject victoryPanel;

        [Header("Textos da Interface")]
        public Text scoreText;
        public Text missionProgressText;
        public Text missionTitleText;
        public Text missionPromptText;
        public Text infoCardTitleText;
        public Text infoCardDescriptionText;
        public Text victoryScoreText;

        [Header("Botão Profissional de Câmera RA")]
        public Button btnToggleARCamera;
        public Text btnToggleARCameraText;
        public Image btnToggleARCameraImage;

        [Header("Botão Lúdico com Olhos Animados")]
        public Button btnFoundContinent;
        public Text btnFoundEyesText;
        public Text btnFoundLabelText;
        private Coroutine eyesAnimationCoroutine;

        [Header("Toast Notification")]
        private GameObject toastObject;
        private Text toastText;
        private CanvasGroup toastCanvasGroup;
        private Coroutine toastCoroutine;

        [Header("Progresso do Jogo")]
        public List<ContinentMission> missions = new List<ContinentMission>();
        public int currentMissionIndex = 0;
        public int correctCount = 0;
        public int totalScore = 0;

        private bool isProcessingAction = false;

        // Sprites procedurais arredondados cacheados
        private static Sprite _roundedPillSprite;
        private static Sprite _circleSprite;
        private static Sprite _glassCardSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoBootstrap()
        {
            if (Instance == null)
            {
                var existing = FindAnyObjectByType<ARLabGameManager>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    var go = new GameObject("ARLabGameManager");
                    Instance = go.AddComponent<ARLabGameManager>();
                }
            }

            Instance.EnsureCameraSettings();
            Instance.EnsureGlobeAndPins();
            Instance.EnsureUIAndEventSystem();
            Instance.LinkHotspots();
        }

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

            EnsureCameraSettings();
            EnsureGlobeAndPins();
            EnsureUIAndEventSystem();
        }

        void Start()
        {
            Initialize7Continents();
            LinkHotspots();
            StartDirectExploration();
            StartEyesAnimation();
        }

        void Update()
        {
            // Animação sutil de respiração (pulsing) no botão de confirmação lúdico
            if (btnFoundContinent != null && btnFoundContinent.gameObject.activeInHierarchy)
            {
                float pulse = 1.0f + 0.025f * Mathf.Sin(Time.time * 3.5f);
                btnFoundContinent.transform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        public void EnsureCameraSettings()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            cam.transform.position = new Vector3(0, 1f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.08f, 0.16f, 1.0f);

            // Garante o componente de Câmera de Fundo AR (Realidade Aumentada)
            arCameraBackground = cam.GetComponent<ARCameraBackground>();
            if (arCameraBackground == null)
            {
                arCameraBackground = cam.gameObject.AddComponent<ARCameraBackground>();
            }
            arCameraBackground.targetCamera = cam;
            arCameraBackground.OnARStateChanged = OnARStateChanged;
        }

        public void EnsureGlobeAndPins()
        {
            GameObject globe = GameObject.Find("Globo_Terrestre");
            if (globe == null)
            {
                globe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                globe.name = "Globo_Terrestre";
                globe.transform.rotation = Quaternion.Euler(0, 180f, 0);
            }

            // Proporção harmônica elegante (1.35x) para visualização espaçosa e realista em RA
            globe.transform.position = new Vector3(0, 0.95f, -6f);
            globe.transform.localScale = Vector3.one * 1.35f;

            globeTransform = globe.transform;

            globeController = globe.GetComponent<GlobeController>();
            if (globeController == null)
            {
                globeController = globe.AddComponent<GlobeController>();
            }

            // Garante os 7 pinos com as coordenadas matematicamente corretas
            EnsurePin(globe, "Pin_AmericaDoSul", "América do Sul", "🐆", "Onça-Pintada", "Floresta Amazônica e o Brasil!", new Vector3(0.293f, -0.096f, -0.431f), new Color(1f, 0.85f, 0.1f));
            EnsurePin(globe, "Pin_Africa", "África", "🦁", "Leão", "Grandes savanas com leões, zebras e girafas!", new Vector3(0.490f, 0.000f, 0.203f), new Color(1f, 0.48f, 0.08f));
            EnsurePin(globe, "Pin_Europa", "Europa", "🦊", "Raposa", "Ao norte da África, com florestas temperadas e castelos!", new Vector3(0.293f, 0.431f, 0.096f), new Color(0.18f, 0.55f, 1f));
            EnsurePin(globe, "Pin_Asia", "Ásia", "🐼", "Urso Panda", "O maior continente da Terra, lar do Panda e do Tigre!", new Vector3(0.000f, 0.297f, 0.439f), new Color(0.95f, 0.2f, 0.35f));
            EnsurePin(globe, "Pin_Oceania", "Oceania", "🦘", "Canguru", "Austrália e ilhas cercadas por corais coloridos!", new Vector3(-0.328f, -0.255f, 0.328f), new Color(0.15f, 0.9f, 0.35f));
            EnsurePin(globe, "Pin_AmericaDoNorte", "América do Norte", "🐻", "Urso Pardo", "Florestas de pinheiros, grandes montanhas e o Urso Pardo!", new Vector3(-0.094f, 0.368f, -0.369f), new Color(0.7f, 0.25f, 0.95f));
            EnsurePin(globe, "Pin_Antartida", "Antártida", "🐧", "Pinguim", "O polo sul congelado coberto de gelo e pinguins!", new Vector3(0.107f, -0.519f, 0.000f), new Color(0.85f, 0.95f, 1f));
        }

        private void EnsurePin(GameObject globe, string goName, string continent, string icon, string animal, string fact, Vector3 localPos, Color color)
        {
            Transform existing = globe.transform.Find(goName);
            GameObject pinGo = existing != null ? existing.gameObject : null;

            if (pinGo == null)
            {
                pinGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pinGo.name = goName;
                pinGo.transform.SetParent(globe.transform, false);
            }

            pinGo.transform.localPosition = localPos.normalized * 0.53f;
            pinGo.transform.localScale = Vector3.one * 0.12f;

            var mr = pinGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Standard");
                var mat = new Material(s);
                mat.color = color;
                mr.material = mat;
            }

            var hotspot = pinGo.GetComponent<ContinentHotspot>();
            if (hotspot == null) hotspot = pinGo.AddComponent<ContinentHotspot>();
            hotspot.continentName = continent;
            hotspot.animalIcon = icon;
            hotspot.animalName = animal;
            hotspot.funFact = fact;
            hotspot.baseColor = color;
        }

        public void EnsureUIAndEventSystem()
        {
            // EventSystem com New Input System
            var es = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                es = esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                if (es.GetComponent<InputSystemUIInputModule>() == null)
                {
                    var oldMod = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    if (oldMod != null) Destroy(oldMod);
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }

            // Canvas Principal
            var canvasGo = GameObject.Find("ARLab_Canvas");
            Canvas canvas = canvasGo != null ? canvasGo.GetComponent<Canvas>() : null;
            if (canvas == null)
            {
                canvasGo = new GameObject("ARLab_Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Recria ou limpa a HUD antiga para aplicar o design Glassmorphism moderno sem blocos quadrados
            Transform hudTr = canvasGo.transform.Find("GameplayHUD");
            if (hudTr != null)
            {
                DestroyImmediate(hudTr.gameObject);
            }

            gameplayHUDPanel = CreatePanel(canvasGo, "GameplayHUD", Color.clear);
            SetRect(gameplayHUDPanel, 0, 0, 1, 1, 0, 0, 0, 0);

            // 1. Top HUD Card (Design Glassmorphic translúcido com cantos arredondados)
            GameObject topCard = CreatePanel(gameplayHUDPanel, "GlassMissionHUD", new Color(0.04f, 0.09f, 0.18f, 0.70f), GetGlassCardSprite());
            SetRect(topCard, 0.04f, 0.845f, 0.67f, 0.975f, 0, 0, 0, 0);

            // Linha superior: Progresso + Pontos
            missionProgressText = CreateText(topCard, "MissionProgressText", "🎯 Missão 1 de 7", 25, new Color(0f, 0.92f, 1f), TextAnchor.MiddleLeft, font);
            SetRect(missionProgressText.gameObject, 0.06f, 0.65f, 0.56f, 0.95f, 0, 0, 0, 0);

            scoreText = CreateText(topCard, "ScoreText", "⭐ 0 pts", 25, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleRight, font);
            SetRect(scoreText.gameObject, 0.56f, 0.65f, 0.94f, 0.95f, 0, 0, 0, 0);

            // Linha central: Nome do Continente e Animal
            missionTitleText = CreateText(topCard, "MissionTitleText", "🐆 América do Sul", 32, Color.white, TextAnchor.MiddleLeft, font);
            missionTitleText.fontStyle = FontStyle.Bold;
            SetRect(missionTitleText.gameObject, 0.06f, 0.32f, 0.94f, 0.65f, 0, 0, 0, 0);

            // Linha inferior: Instrução lúdica amigável
            missionPromptText = CreateText(topCard, "MissionPromptText", "Gire a Terra para posicionar no centro!", 20, new Color(0.82f, 0.90f, 0.98f), TextAnchor.MiddleLeft, font);
            SetRect(missionPromptText.gameObject, 0.06f, 0.05f, 0.94f, 0.32f, 0, 0, 0, 0);

            // 2. Botão Profissional de Câmera RA com Ícone e Estado Explícito (Ativar / Desativar)
            btnToggleARCamera = CreateRoundedButton(gameplayHUDPanel, "BtnToggleARCamera", "📷 ATIVAR RA", new Color(0.06f, 0.55f, 0.45f, 0.92f), font, GetRoundedPillSprite(), 20);
            SetRect(btnToggleARCamera.gameObject, 0.69f, 0.865f, 0.96f, 0.965f, 0, 0, 0, 0);
            btnToggleARCamera.onClick.AddListener(ToggleARMode);
            btnToggleARCameraText = btnToggleARCamera.GetComponentInChildren<Text>();
            btnToggleARCameraText.fontStyle = FontStyle.Bold;
            btnToggleARCameraImage = btnToggleARCamera.GetComponent<Image>();

            // 3. Botão Flutuante Inferior Lúdico com Olhos Animados e Brilho Aurora Teal
            btnFoundContinent = CreateRoundedButton(gameplayHUDPanel, "BtnFoundContinent", "", new Color(0.02f, 0.78f, 0.65f, 0.96f), font, GetRoundedPillSprite(), 26);
            SetRect(btnFoundContinent.gameObject, 0.06f, 0.035f, 0.94f, 0.11f, 0, 0, 0, 0);
            btnFoundContinent.onClick.AddListener(CheckCurrentOrientation);

            // Remove o texto centrado padrão para acomodar os Olhos Animados + Rótulo Lúdico
            var defaultBtnText = btnFoundContinent.transform.Find("Text");
            if (defaultBtnText != null) DestroyImmediate(defaultBtnText.gameObject);

            // Olhos observando animados (lado esquerdo)
            btnFoundEyesText = CreateText(btnFoundContinent.gameObject, "EyesIcon", "👀", 36, Color.white, TextAnchor.MiddleCenter, font);
            SetRect(btnFoundEyesText.gameObject, 0.02f, 0.05f, 0.20f, 0.95f, 0, 0, 0, 0);

            // Rótulo de confirmação com visual vibrante (centro-direita)
            btnFoundLabelText = CreateText(btnFoundContinent.gameObject, "MainLabel", "ESTOU VENDO ESTE CONTINENTE!", 25, Color.white, TextAnchor.MiddleLeft, font);
            btnFoundLabelText.fontStyle = FontStyle.Bold;
            SetRect(btnFoundLabelText.gameObject, 0.20f, 0.05f, 0.98f, 0.95f, 0, 0, 0, 0);

            // 4. Toast Notification Flutuante
            EnsureToastUI(canvasGo, font);

            // 5. InfoCard (Feedback e Curiosidades com Glassmorphism)
            Transform infoTr = canvasGo.transform.Find("InfoCard");
            if (infoTr != null) DestroyImmediate(infoTr.gameObject);

            infoCardPanel = CreatePanel(canvasGo, "InfoCard", new Color(0.05f, 0.10f, 0.22f, 0.94f), GetGlassCardSprite());
            SetRect(infoCardPanel, 0.07f, 0.32f, 0.93f, 0.68f, 0, 0, 0, 0);

            infoCardTitleText = CreateText(infoCardPanel, "InfoTitle", "Resultado", 34, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter, font);
            infoCardTitleText.fontStyle = FontStyle.Bold;
            SetRect(infoCardTitleText.gameObject, 0.06f, 0.72f, 0.84f, 0.95f, 0, 0, 0, 0);

            infoCardDescriptionText = CreateText(infoCardPanel, "InfoDesc", "Você encontrou o continente!", 26, Color.white, TextAnchor.MiddleCenter, font);
            SetRect(infoCardDescriptionText.gameObject, 0.06f, 0.22f, 0.94f, 0.72f, 0, 0, 0, 0);

            var btnClose = CreateRoundedButton(infoCardPanel, "BtnCloseInfo", "✖", new Color(0.85f, 0.25f, 0.25f, 0.9f), font, GetCircleSprite(), 28);
            SetRect(btnClose.gameObject, 0.86f, 0.80f, 0.96f, 0.94f, 0, 0, 0, 0);
            btnClose.onClick.AddListener(HideInfoCard);

            infoCardPanel.SetActive(false);

            // 6. Painel de Vitória Final
            Transform vicTr = canvasGo.transform.Find("VictoryPanel");
            if (vicTr != null) DestroyImmediate(vicTr.gameObject);

            victoryPanel = CreatePanel(canvasGo, "VictoryPanel", new Color(0.04f, 0.08f, 0.18f, 0.96f), GetGlassCardSprite());
            SetRect(victoryPanel, 0.08f, 0.28f, 0.92f, 0.72f, 0, 0, 0, 0);

            var vicTitle = CreateText(victoryPanel, "VictoryTitle", "🎉 MISSÃO CONCLUÍDA! 🌍", 36, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter, font);
            vicTitle.fontStyle = FontStyle.Bold;
            SetRect(vicTitle.gameObject, 0.05f, 0.75f, 0.95f, 0.95f, 0, 0, 0, 0);

            victoryScoreText = CreateText(victoryPanel, "VictoryScore", "Você conheceu os 7 Continentes!", 28, Color.white, TextAnchor.MiddleCenter, font);
            SetRect(victoryScoreText.gameObject, 0.06f, 0.32f, 0.94f, 0.75f, 0, 0, 0, 0);

            var btnRestart = CreateRoundedButton(victoryPanel, "BtnRestart", "🔄 JOGAR NOVAMENTE", new Color(0.06f, 0.82f, 0.45f, 0.95f), font, GetRoundedPillSprite(), 28);
            btnRestart.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            SetRect(btnRestart.gameObject, 0.12f, 0.10f, 0.88f, 0.24f, 0, 0, 0, 0);
            btnRestart.onClick.AddListener(StartDirectExploration);

            victoryPanel.SetActive(false);

            // Atualiza o estado inicial do botão da câmera RA
            bool isARCurrentlyActive = (arCameraBackground != null && arCameraBackground.IsARActive);
            UpdateARButtonUI(isARCurrentlyActive);

            StartEyesAnimation();
        }

        private void StartEyesAnimation()
        {
            if (eyesAnimationCoroutine != null) StopCoroutine(eyesAnimationCoroutine);
            eyesAnimationCoroutine = StartCoroutine(EyesAnimationRoutine());
        }

        private IEnumerator EyesAnimationRoutine()
        {
            // Ciclo lúdico de olhos observando, piscando e procurando continentes
            string[] eyeFrames = new string[]
            {
                "👀",      // Olhos atentos olhando a Terra
                "👀",      // Observando
                "😌",      // Piscadela rápida e amigável
                "👀",      // Reabertos
                "🔎👀",    // Procurando à esquerda
                "👀🔍",    // Procurando à direita
                "✨👀✨",  // Olhos brilhando curiosos
                "🤩"       // Encantado com a descoberta
            };

            float[] durations = new float[]
            {
                1.5f,
                1.2f,
                0.18f,
                1.0f,
                1.3f,
                1.3f,
                1.5f,
                1.2f
            };

            int index = 0;
            while (true)
            {
                if (btnFoundEyesText != null)
                {
                    btnFoundEyesText.text = eyeFrames[index];
                }

                yield return new WaitForSeconds(durations[index]);

                index = (index + 1) % eyeFrames.Length;
            }
        }

        private void EnsureToastUI(GameObject canvasGo, Font font)
        {
            Transform existingToast = canvasGo.transform.Find("ToastNotification");
            if (existingToast != null) DestroyImmediate(existingToast.gameObject);

            toastObject = CreatePanel(canvasGo, "ToastNotification", new Color(0.04f, 0.08f, 0.16f, 0.94f), GetRoundedPillSprite());
            SetRect(toastObject, 0.10f, 0.75f, 0.90f, 0.82f, 0, 0, 0, 0);

            toastCanvasGroup = toastObject.AddComponent<CanvasGroup>();
            toastCanvasGroup.alpha = 0f;
            toastCanvasGroup.interactable = false;
            toastCanvasGroup.blocksRaycasts = false;

            toastText = CreateText(toastObject, "ToastText", "", 24, Color.white, TextAnchor.MiddleCenter, font);
            toastText.fontStyle = FontStyle.Bold;
            SetRect(toastText.gameObject, 0.05f, 0.05f, 0.95f, 0.95f, 0, 0, 0, 0);

            toastObject.SetActive(false);
        }

        public void ShowToast(string message, Color? accentColor = null)
        {
            if (toastObject == null) return;
            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(AnimateToast(message, accentColor ?? new Color(0f, 0.92f, 1f)));
        }

        private IEnumerator AnimateToast(string message, Color accentColor)
        {
            toastObject.SetActive(true);
            if (toastText != null)
            {
                toastText.text = message;
                toastText.color = accentColor;
            }

            // Fade in suave
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                toastCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.2f);
                yield return null;
            }
            toastCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(2.0f);

            // Fade out suave
            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                toastCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / 0.3f));
                yield return null;
            }
            toastCanvasGroup.alpha = 0f;
            toastObject.SetActive(false);
        }

        private void Initialize7Continents()
        {
            missions.Clear();

            missions.Add(new ContinentMission
            {
                continentName = "América do Sul",
                animalIcon = "🐆",
                animalName = "Onça-Pintada",
                questionPrompt = "Encontre a América do Sul! Gire a Terra até posicioná-la no centro!",
                successFact = "América do Sul: Floresta Amazônica, Onça-Pintada e o Brasil!"
            });

            missions.Add(new ContinentMission
            {
                continentName = "África",
                animalIcon = "🦁",
                animalName = "Leão",
                questionPrompt = "Onde vive o grande Leão nas savanas? Gire o globo até a África!",
                successFact = "África: terra de leões, zebras e girafas sob o sol da savana!"
            });

            missions.Add(new ContinentMission
            {
                continentName = "Europa",
                animalIcon = "🦊",
                animalName = "Raposa",
                questionPrompt = "Logo acima da África, encontre a Europa! Gire até ela aparecer no centro!",
                successFact = "Europa: ao norte da África, com florestas temperadas e belos castelos!"
            });

            missions.Add(new ContinentMission
            {
                continentName = "Ásia",
                animalIcon = "🐼",
                animalName = "Urso Panda",
                questionPrompt = "Procure o maior continente da Terra onde vive o Urso Panda! Encontre a Ásia!",
                successFact = "Ásia: o maior continente do planeta, lar dos Montes Himalaias e do Panda!"
            });

            missions.Add(new ContinentMission
            {
                continentName = "Oceania",
                animalIcon = "🦘",
                animalName = "Canguru",
                questionPrompt = "Onde os cangurus saltam em ilhas no oceano azul? Gire até a Oceania!",
                successFact = "Oceania: Austrália, ilhas paradisíacas e recifes de corais brilhantes!"
            });

            missions.Add(new ContinentMission
            {
                continentName = "América do Norte",
                animalIcon = "🐻",
                animalName = "Urso Pardo",
                questionPrompt = "Encontre as terras altas do Urso Pardo e montanhas nevadas na América do Norte!",
                successFact = "América do Norte: grandes montanhas rochosas, lagos gigantes e florestas de pinheiros!"
            });

            missions.Add(new ContinentMission
            {
                continentName = "Antártida",
                animalIcon = "🐧",
                animalName = "Pinguim",
                questionPrompt = "Gire o globo para ver o polo sul gelado onde vivem os pinguins na Antártida!",
                successFact = "Antártida: o continente mais frio do mundo, todo coberto de gelo e pinguins!"
            });
        }

        public void LinkHotspots()
        {
            var hotspots = FindObjectsByType<ContinentHotspot>(FindObjectsSortMode.None);
            foreach (var h in hotspots)
            {
                foreach (var m in missions)
                {
                    if (m.continentName.Trim().Equals(h.continentName.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        m.hotspot = h;
                        break;
                    }
                }
            }
        }

        public void StartDirectExploration()
        {
            currentMissionIndex = 0;
            correctCount = 0;
            totalScore = 0;
            isProcessingAction = false;

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (infoCardPanel != null) infoCardPanel.SetActive(false);
            if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(true);

            UpdateScoreDisplay();
            StartCurrentMission();

            TelemetryLogger.Instance?.RegisterSessionStart();
        }

        private void StartCurrentMission()
        {
            if (currentMissionIndex >= missions.Count)
            {
                EndGameSession();
                return;
            }

            isProcessingAction = false;
            var mission = missions[currentMissionIndex];

            if (missionTitleText != null)
                missionTitleText.text = $"{mission.animalIcon} Missão: {mission.continentName}";

            if (missionPromptText != null)
                missionPromptText.text = mission.questionPrompt;

            if (missionProgressText != null)
                missionProgressText.text = $"🎯 Missão {currentMissionIndex + 1} de {missions.Count}";
        }

        /// <summary>
        /// Acionado pelo botão lúdico 'ESTOU VENDO ESTE CONTINENTE!'
        /// </summary>
        public void CheckCurrentOrientation()
        {
            if (isProcessingAction || currentMissionIndex >= missions.Count) return;

            var mission = missions[currentMissionIndex];
            bool isFacing = mission.hotspot != null ? mission.hotspot.IsFacingCamera() : true;

            if (isFacing)
            {
                if (btnFoundEyesText != null) btnFoundEyesText.text = "🎉🤩🎉";
                StartCoroutine(ProcessSuccessRoutine(mission));
            }
            else
            {
                if (btnFoundEyesText != null) btnFoundEyesText.text = "👀❓";
                StartCoroutine(ProcessFailAndCorrectRoutine(mission));
            }
        }

        /// <summary>
        /// Acionado por toque direto no pino 3D
        /// </summary>
        public void OnHotspotClicked(ContinentHotspot clickedHotspot)
        {
            if (isProcessingAction || currentMissionIndex >= missions.Count) return;

            var currentMission = missions[currentMissionIndex];

            if (clickedHotspot.continentName.Trim().Equals(currentMission.continentName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                if (btnFoundEyesText != null) btnFoundEyesText.text = "🎉🤩🎉";
                StartCoroutine(ProcessSuccessRoutine(currentMission));
            }
            else
            {
                ShowInfoCard($"🔍 Você tocou em: {clickedHotspot.continentName}!", $"{clickedHotspot.animalIcon} {clickedHotspot.animalName}: {clickedHotspot.funFact}\n\n👉 A missão atual procura: {currentMission.continentName} {currentMission.animalIcon}!", new Color(0.08f, 0.22f, 0.45f, 0.95f));
            }
        }

        private IEnumerator ProcessSuccessRoutine(ContinentMission mission)
        {
            isProcessingAction = true;
            correctCount++;
            totalScore += 10;
            UpdateScoreDisplay();

            TelemetryLogger.Instance?.RegisterQuizAttempt(mission.continentName, true, 1);

            ShowInfoCard($"🎉 PARABÉNS! ACERTOU!", $"Você posicionou e encontrou {mission.continentName} ({mission.animalIcon} {mission.animalName})!\n{mission.successFact}", new Color(0.06f, 0.65f, 0.32f, 0.96f));

            if (mission.hotspot != null)
            {
                mission.hotspot.FlashSuccess(null);
            }

            yield return new WaitForSeconds(2.2f);

            HideInfoCard();
            currentMissionIndex++;
            StartCurrentMission();
        }

        private IEnumerator ProcessFailAndCorrectRoutine(ContinentMission mission)
        {
            isProcessingAction = true;
            TelemetryLogger.Instance?.RegisterQuizAttempt(mission.continentName, false, 1);

            ShowInfoCard($"❌ Ops! Não estava aí...", $"Observe o globo girando! Vamos mostrar onde fica a {mission.continentName} {mission.animalIcon}...", new Color(0.75f, 0.18f, 0.18f, 0.96f));

            if (globeController != null) globeController.enabled = false;

            if (globeTransform != null && mission.hotspot != null && Camera.main != null)
            {
                Vector3 localDir = mission.hotspot.transform.localPosition.normalized;
                Vector3 dirToCam = (Camera.main.transform.position - globeTransform.position).normalized;

                Quaternion startRot = globeTransform.rotation;
                Vector3 curWorldDir = globeTransform.TransformDirection(localDir);
                Quaternion deltaRot = Quaternion.FromToRotation(curWorldDir, dirToCam);
                Quaternion targetRot = deltaRot * globeTransform.rotation;

                float elapsed = 0f;
                float duration = 1.3f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    globeTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                    yield return null;
                }
                globeTransform.rotation = targetRot;

                mission.hotspot.FlashFail(null);

                ShowInfoCard($"👀 Olhe aqui!", $"A {mission.continentName} ({mission.animalIcon} {mission.animalName}) fica nesta posição da Terra!\nObserve bem para lembrar!", new Color(0.85f, 0.45f, 0.1f, 0.96f));
            }

            yield return new WaitForSeconds(2.8f);

            if (globeController != null) globeController.enabled = true;

            HideInfoCard();
            currentMissionIndex++;
            StartCurrentMission();
        }

        private void ShowInfoCard(string title, string desc, Color bgColor)
        {
            if (infoCardPanel != null)
            {
                infoCardPanel.SetActive(true);
                var img = infoCardPanel.GetComponent<Image>();
                if (img != null) img.color = bgColor;
            }

            if (infoCardTitleText != null) infoCardTitleText.text = title;
            if (infoCardDescriptionText != null) infoCardDescriptionText.text = desc;
        }

        public void HideInfoCard()
        {
            if (infoCardPanel != null) infoCardPanel.SetActive(false);
        }

        private void UpdateScoreDisplay()
        {
            if (scoreText != null)
            {
                scoreText.text = $"⭐ {totalScore} pts ({correctCount}/{missions.Count})";
            }
        }

        private void EndGameSession()
        {
            isProcessingAction = true;
            if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(false);
            if (infoCardPanel != null) infoCardPanel.SetActive(false);

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                if (victoryScoreText != null)
                {
                    victoryScoreText.text = $"Parabéns Explorador!\nVocê acertou ⭐ {correctCount} de {missions.Count} Continentes!\nPontuação Total: {totalScore} pts!";
                }
            }

            TelemetryLogger.Instance?.RegisterQuizFinished(correctCount, missions.Count);
        }

        public void ToggleARMode()
        {
            if (arCameraBackground == null)
            {
                EnsureCameraSettings();
            }
            if (arCameraBackground != null)
            {
                arCameraBackground.ToggleARCamera();
            }
        }

        private void OnARStateChanged(bool isARActive)
        {
            UpdateARButtonUI(isARActive);

            if (isARActive)
            {
                ShowToast("✨ Câmera RA Ativada! Aponte para sua mesa ou chão.", new Color(0.1f, 0.95f, 0.55f));
            }
            else
            {
                ShowToast("🪐 Modo Espaço Ativado! Fundo cósmico.", new Color(0.4f, 0.75f, 1f));
            }
        }

        public void UpdateARButtonUI(bool isARActive)
        {
            if (btnToggleARCameraText != null)
            {
                btnToggleARCameraText.text = isARActive ? "✕ DESATIVAR RA" : "📷 ATIVAR RA";
            }
            if (btnToggleARCameraImage != null)
            {
                btnToggleARCameraImage.color = isARActive 
                    ? new Color(0.78f, 0.18f, 0.28f, 0.92f) 
                    : new Color(0.06f, 0.55f, 0.45f, 0.92f);
            }
        }

        // --- SISTEMA PROCEDURAL DE SPRITES ARREDONDADOS E GLASSMORPHISM ---

        public static Sprite GetRoundedPillSprite()
        {
            if (_roundedPillSprite == null)
            {
                _roundedPillSprite = CreateRoundedRectSprite(128, 64, 30);
            }
            return _roundedPillSprite;
        }

        public static Sprite GetGlassCardSprite()
        {
            if (_glassCardSprite == null)
            {
                _glassCardSprite = CreateRoundedRectSprite(128, 128, 24);
            }
            return _glassCardSprite;
        }

        public static Sprite GetCircleSprite()
        {
            if (_circleSprite == null)
            {
                _circleSprite = CreateCircleSprite(128);
            }
            return _circleSprite;
        }

        private static Sprite CreateRoundedRectSprite(int width, int height, int radius)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] cols = new Color32[width * height];
            float r = radius;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0;
                    if (x < r) dx = r - x;
                    else if (x > width - 1 - r) dx = x - (width - 1 - r);

                    float dy = 0;
                    if (y < r) dy = r - y;
                    else if (y > height - 1 - r) dy = y - (height - 1 - r);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 1f;
                    if (dist > r)
                    {
                        alpha = Mathf.Clamp01(1f - (dist - r));
                    }
                    cols[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels32(cols);
            tex.Apply();
            Vector4 border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static Sprite CreateCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] cols = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(r - dist + 0.5f);
                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // Helpers de Construção de UI
        private static GameObject CreatePanel(GameObject parent, string name, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = (sprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            }
            img.raycastTarget = true;
            return go;
        }

        private static Text CreateText(GameObject parent, string name, string text, int fontSize, Color color, TextAnchor alignment, Font font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = alignment;
            t.font = font;
            t.raycastTarget = false;
            return t;
        }

        private static Button CreateRoundedButton(GameObject parent, string name, string label, Color bgColor, Font font, Sprite sprite = null, int fontSize = 28)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = (sprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            }
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.2f);
            btn.colors = colors;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.AddComponent<Text>();
            t.text = label;
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = font;
            t.raycastTarget = false;
            SetRect(textGo, 0, 0, 1, 1, 0, 0, 0, 0);

            return btn;
        }

        private static void SetRect(GameObject go, float minX, float minY, float maxX, float maxY, float left, float bottom, float right, float top)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
