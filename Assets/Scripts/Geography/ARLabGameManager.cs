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
    /// Auto-inicialização completa em tempo de execução para Android e Editor,
    /// controle de 7 missões pedagógicas com validação de orientação,
    /// efeito de piscar em Verde (Acerto) ou Vermelho com rotação corretiva suave (Erro),
    /// e placar oficial de acertos 'X de 7'.
    /// </summary>
    public class ARLabGameManager : MonoBehaviour
    {
        public static ARLabGameManager Instance { get; private set; }

        [Header("Referências da Cena")]
        public Transform globeTransform;
        public GlobeController globeController;

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

        [Header("Lista de Continentes (7 Continentes)")]
        public List<ContinentMission> missions = new List<ContinentMission>();

        private int currentMissionIndex = 0;
        private int correctCount = 0;
        private int totalScore = 0;
        private bool isProcessingAction = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoBootstrap()
        {
            if (Instance == null && FindAnyObjectByType<ARLabGameManager>() == null)
            {
                var go = new GameObject("ARLabGameManager");
                go.AddComponent<ARLabGameManager>();
            }
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
            cam.backgroundColor = new Color(0.04f, 0.08f, 0.16f, 1.0f); // 100% opaco para Android
        }

        public void EnsureGlobeAndPins()
        {
            GameObject globe = GameObject.Find("Globo_Terrestre");
            if (globe == null)
            {
                globe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                globe.name = "Globo_Terrestre";
                globe.transform.position = new Vector3(0, 1f, -6f);
                globe.transform.localScale = Vector3.one * 2.2f;
                globe.transform.rotation = Quaternion.Euler(0, 180f, 0);
            }

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

            // Canvas
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

            // GameplayHUD
            Transform hudTr = canvasGo.transform.Find("GameplayHUD");
            if (hudTr == null)
            {
                gameplayHUDPanel = CreatePanel(canvasGo, "GameplayHUD", Color.clear);
                SetRect(gameplayHUDPanel, 0, 0, 1, 1, 0, 0, 0, 0);

                // Top Bar
                GameObject topBar = CreatePanel(gameplayHUDPanel, "TopBar", new Color(0.04f, 0.10f, 0.22f, 0.90f));
                SetRect(topBar, 0.04f, 0.88f, 0.96f, 0.98f, 0, 0, 0, 0);

                missionProgressText = CreateText(topBar, "MissionProgressText", "Missão 1 de 7", 32, new Color(0f, 0.9f, 1f), TextAnchor.MiddleLeft, font);
                SetRect(missionProgressText.gameObject, 0.05f, 0.1f, 0.50f, 0.9f, 0, 0, 0, 0);

                scoreText = CreateText(topBar, "ScoreText", "⭐ Acertos: 0 de 7  (0 pts)", 30, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleRight, font);
                SetRect(scoreText.gameObject, 0.50f, 0.1f, 0.95f, 0.9f, 0, 0, 0, 0);

                // Mission Card
                GameObject missionCard = CreatePanel(gameplayHUDPanel, "MissionCard", new Color(0.05f, 0.12f, 0.26f, 0.92f));
                SetRect(missionCard, 0.05f, 0.74f, 0.95f, 0.87f, 0, 0, 0, 0);

                missionTitleText = CreateText(missionCard, "MissionTitleText", "🐆 Missão: América do Sul", 36, Color.white, TextAnchor.MiddleLeft, font);
                SetRect(missionTitleText.gameObject, 0.06f, 0.55f, 0.94f, 0.92f, 0, 0, 0, 0);

                missionPromptText = CreateText(missionCard, "MissionPromptText", "Gire a Terra para posicionar este continente no centro!", 28, new Color(0.85f, 0.9f, 1f), TextAnchor.MiddleLeft, font);
                SetRect(missionPromptText.gameObject, 0.06f, 0.08f, 0.94f, 0.52f, 0, 0, 0, 0);

                // Botão de Confirmação
                Button btnFound = CreateButton(gameplayHUDPanel, "BtnFoundContinent", "🌍 ESTOU VENDO ESTE CONTINENTE!", new Color(0.05f, 0.75f, 0.38f), font);
                SetRect(btnFound.gameObject, 0.06f, 0.04f, 0.94f, 0.12f, 0, 0, 0, 0);
                btnFound.onClick.AddListener(CheckCurrentOrientation);
            }
            else
            {
                gameplayHUDPanel = hudTr.gameObject;
                BindExistingHUD(gameplayHUDPanel);
            }

            // InfoCard
            Transform infoTr = canvasGo.transform.Find("InfoCard");
            if (infoTr == null)
            {
                infoCardPanel = CreatePanel(canvasGo, "InfoCard", new Color(0.08f, 0.18f, 0.35f, 0.96f));
                SetRect(infoCardPanel, 0.08f, 0.36f, 0.92f, 0.64f, 0, 0, 0, 0);

                infoCardTitleText = CreateText(infoCardPanel, "InfoTitle", "Resultado", 36, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter, font);
                SetRect(infoCardTitleText.gameObject, 0.05f, 0.70f, 0.85f, 0.95f, 0, 0, 0, 0);

                infoCardDescriptionText = CreateText(infoCardPanel, "InfoDesc", "Você encontrou o continente!", 28, Color.white, TextAnchor.MiddleCenter, font);
                SetRect(infoCardDescriptionText.gameObject, 0.05f, 0.15f, 0.95f, 0.70f, 0, 0, 0, 0);

                var btnClose = CreateButton(infoCardPanel, "BtnCloseInfo", "✖", new Color(0.8f, 0.2f, 0.2f), font);
                SetRect(btnClose.gameObject, 0.88f, 0.75f, 0.98f, 0.95f, 0, 0, 0, 0);
                btnClose.onClick.AddListener(HideInfoCard);

                infoCardPanel.SetActive(false);
            }
            else
            {
                infoCardPanel = infoTr.gameObject;
                infoCardTitleText = FindTextInChild(infoCardPanel, "InfoTitle");
                infoCardDescriptionText = FindTextInChild(infoCardPanel, "InfoDesc");
                BindButtonInChild(infoCardPanel, "BtnCloseInfo", HideInfoCard);
            }

            // VictoryPanel
            Transform vicTr = canvasGo.transform.Find("VictoryPanel");
            if (vicTr == null)
            {
                victoryPanel = CreatePanel(canvasGo, "VictoryPanel", new Color(0.04f, 0.1f, 0.22f, 0.96f));
                SetRect(victoryPanel, 0.05f, 0.25f, 0.95f, 0.75f, 0, 0, 0, 0);

                var vicTitle = CreateText(victoryPanel, "VictoryTitle", "🏆 PARABÉNS, EXPLORADOR!", 46, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter, font);
                SetRect(vicTitle.gameObject, 0.05f, 0.65f, 0.95f, 0.92f, 0, 0, 0, 0);

                victoryScoreText = CreateText(victoryPanel, "VictoryScore", "Você finalizou o Desafio dos Continentes!\nAcertos: ⭐ 7 de 7 Continentes!", 30, Color.white, TextAnchor.MiddleCenter, font);
                SetRect(victoryScoreText.gameObject, 0.05f, 0.35f, 0.95f, 0.65f, 0, 0, 0, 0);

                var btnRestart = CreateButton(victoryPanel, "BtnRestart", "🔄 JOGAR NOVAMENTE", new Color(0.2f, 0.75f, 0.4f), font);
                SetRect(btnRestart.gameObject, 0.15f, 0.10f, 0.85f, 0.28f, 0, 0, 0, 0);
                btnRestart.onClick.AddListener(StartDirectExploration);

                victoryPanel.SetActive(false);
            }
            else
            {
                victoryPanel = vicTr.gameObject;
                victoryScoreText = FindTextInChild(victoryPanel, "VictoryScore");
                BindButtonInChild(victoryPanel, "BtnRestart", StartDirectExploration);
            }
        }

        private void BindExistingHUD(GameObject hud)
        {
            if (missionProgressText == null) missionProgressText = FindTextInChild(hud, "MissionProgressText");
            if (scoreText == null) scoreText = FindTextInChild(hud, "ScoreText");
            if (missionTitleText == null) missionTitleText = FindTextInChild(hud, "MissionTitleText");
            if (missionPromptText == null) missionPromptText = FindTextInChild(hud, "MissionPromptText");
            BindButtonInChild(hud, "BtnFoundContinent", CheckCurrentOrientation);
        }

        private Text FindTextInChild(GameObject parent, string name)
        {
            var tr = parent.transform.Find(name);
            if (tr == null)
            {
                foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name) return child.GetComponent<Text>();
                }
                return null;
            }
            return tr.GetComponent<Text>();
        }

        private void BindButtonInChild(GameObject parent, string name, UnityEngine.Events.UnityAction action)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    var btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(action);
                    }
                    return;
                }
            }
        }

        private void Initialize7Continents()
        {
            missions.Clear();

            missions.Add(new ContinentMission
            {
                continentName = "América do Sul",
                animalIcon = "🐆",
                animalName = "Onça-Pintada",
                questionPrompt = "Encontre a América do Sul! Gire a Terra até posicioná-la no centro da tela!",
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
                questionPrompt = "Encontre a grande Ásia, o maior continente! Onde vive o Urso Panda?",
                successFact = "Ásia: o maior continente da Terra, lar do Panda e do Tigre!"
            });

            missions.Add(new ContinentMission
            {
                continentName = "Oceania",
                animalIcon = "🦘",
                animalName = "Canguru",
                questionPrompt = "Onde pula o simpático Canguru? Gire a Terra até as ilhas da Oceania!",
                successFact = "Oceania: Austrália cercada por oceanos e a Grande Barreira de Corais!"
            });

            missions.Add(new ContinentMission
            {
                continentName = "América do Norte",
                animalIcon = "🐻",
                animalName = "Urso Pardo",
                questionPrompt = "Ao norte, encontre a América do Norte, a terra do Urso Pardo e das Águias!",
                successFact = "América do Norte: grandes montanhas, florestas e o Urso Pardo!"
            });

            missions.Add(new ContinentMission
            {
                continentName = "Antártida",
                animalIcon = "🐧",
                animalName = "Pinguim",
                questionPrompt = "Lá no Polo Sul, encontre o gelo da Antártida onde moram os Pinguins!",
                successFact = "Antártida: o continente mais frio da Terra coberto de gelo e pinguins!"
            });
        }

        public void LinkHotspots()
        {
            var hotspots = FindObjectsByType<ContinentHotspot>(FindObjectsInactive.Include);
            foreach (var m in missions)
            {
                foreach (var h in hotspots)
                {
                    if (h.continentName.Trim().Equals(m.continentName.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        m.hotspot = h;
                        break;
                    }
                }
            }
        }

        public void StartDirectExploration()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(true);
            if (infoCardPanel != null) infoCardPanel.SetActive(false);

            currentMissionIndex = 0;
            correctCount = 0;
            totalScore = 0;
            isProcessingAction = false;

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
                missionProgressText.text = $"Missão {currentMissionIndex + 1} de {missions.Count}";
        }

        /// <summary>
        /// Acionado pelo botão 'Estou Vendo Este Continente!'
        /// </summary>
        public void CheckCurrentOrientation()
        {
            if (isProcessingAction || currentMissionIndex >= missions.Count) return;

            var mission = missions[currentMissionIndex];
            bool isFacing = mission.hotspot != null ? mission.hotspot.IsFacingCamera() : true;

            if (isFacing)
            {
                StartCoroutine(ProcessSuccessRoutine(mission));
            }
            else
            {
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
                StartCoroutine(ProcessSuccessRoutine(currentMission));
            }
            else
            {
                ShowInfoCard($"🔍 Você tocou em: {clickedHotspot.continentName}!", $"{clickedHotspot.animalIcon} {clickedHotspot.animalName}: {clickedHotspot.funFact}\n\n👉 A missão atual procura: {currentMission.continentName} {currentMission.animalIcon}!", new Color(0.12f, 0.35f, 0.65f, 0.95f));
            }
        }

        private IEnumerator ProcessSuccessRoutine(ContinentMission mission)
        {
            isProcessingAction = true;
            correctCount++;
            totalScore += 10;
            UpdateScoreDisplay();

            TelemetryLogger.Instance?.RegisterQuizAttempt(mission.continentName, true, 1);

            ShowInfoCard($"🎉 PARABÉNS! ACERTOU!", $"Você posicionou e encontrou {mission.continentName} ({mission.animalIcon} {mission.animalName})!\n{mission.successFact}", new Color(0.1f, 0.68f, 0.28f, 0.96f));

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

            ShowInfoCard($"❌ Ops! Não estava aí...", $"Observe o globo girando! Vamos mostrar onde fica a {mission.continentName} {mission.animalIcon}...", new Color(0.85f, 0.2f, 0.2f, 0.96f));

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
                scoreText.text = $"⭐ Acertos: {correctCount} de {missions.Count}  ({totalScore} pts)";
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
                    victoryScoreText.text = $"Sessão Finalizada!\nVocê acertou ⭐ {correctCount} de {missions.Count} Continentes!\nTotal de Pontos: {totalScore} pts!";
                }
            }

            TelemetryLogger.Instance?.RegisterQuizFinished(correctCount, missions.Count);
        }

        // Helpers de Construção de UI
        private static GameObject CreatePanel(GameObject parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = color;
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

        private static Button CreateButton(GameObject parent, string name, string label, Color color, Font font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            var btn = go.AddComponent<Button>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.AddComponent<Text>();
            t.text = label;
            t.fontSize = 28;
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
