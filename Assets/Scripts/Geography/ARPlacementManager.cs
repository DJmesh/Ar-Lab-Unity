using System.Collections.Generic;
using UnityEngine;
#if ENABLE_AR_FOUNDATION
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#endif

namespace ARLab.Geography
{
    /// <summary>
    /// Gerencia a ancoragem espacial do globo terrestre em celulares ou simulador.
    /// Funciona perfeitamente em modo Simulação/Editor e com AR Foundation quando habilitado.
    /// </summary>
    public class ARPlacementManager : MonoBehaviour
    {
        [Header("Prefabs e Referências")]
        [Tooltip("Prefab contendo o modelo 3D do globo terrestre (earth_globe.glb)")]
        public GameObject globePrefab;

        [Tooltip("Indicador visual de posicionamento na mesa (retículo de mira)")]
        public GameObject placementIndicator;

        [Header("Configurações de Escala Inicial")]
        public float defaultGlobeDiameter = 0.35f; // 35 cm ideal para mesa escolar infantil

        private GameObject spawnedGlobe;
        private Pose placementPose;
        private bool placementPoseIsValid = false;

#if ENABLE_AR_FOUNDATION
        private ARRaycastManager arRaycastManager;
        private ARPlaneManager arPlaneManager;
        private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
#endif

        void Awake()
        {
#if ENABLE_AR_FOUNDATION
            arRaycastManager = GetComponent<ARRaycastManager>();
            arPlaneManager = GetComponent<ARPlaneManager>();
#endif
        }

        void Start()
        {
            // Se o globo já estiver na cena como objeto filho ou posicionado, registra
            if (spawnedGlobe == null && globePrefab != null)
            {
                // No Simulator/Editor, se não houver AR ativo, posiciona à frente da câmera automaticamente
                #if UNITY_EDITOR
                PlaceGlobeInFrontOfCamera();
                #endif
            }
        }

        void Update()
        {
            if (spawnedGlobe == null)
            {
                UpdatePlacementPose();
                UpdatePlacementIndicator();

                // Toque na tela para posicionar o globo na mesa
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
                {
                    PlaceGlobe();
                }

                #if UNITY_EDITOR
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    PlaceGlobeInFrontOfCamera();
                }
                #endif
            }
        }

        private void UpdatePlacementPose()
        {
#if ENABLE_AR_FOUNDATION
            if (arRaycastManager != null)
            {
                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);

                placementPoseIsValid = hits.Count > 0;
                if (placementPoseIsValid)
                {
                    placementPose = hits[0].pose;
                    var cameraForward = Camera.main.transform.forward;
                    var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
                    placementPose.rotation = Quaternion.LookRotation(cameraBearing);
                }
                return;
            }
#endif
            // Fallback para modo Editor / Simulator
            if (Camera.main != null)
            {
                placementPose.position = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
                placementPose.rotation = Quaternion.identity;
                placementPoseIsValid = true;
            }
        }

        private void UpdatePlacementIndicator()
        {
            if (placementIndicator == null) return;

            if (placementPoseIsValid && spawnedGlobe == null)
            {
                placementIndicator.SetActive(true);
                placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
            }
            else
            {
                placementIndicator.SetActive(false);
            }
        }

        private void PlaceGlobe()
        {
            if (globePrefab == null) return;

            spawnedGlobe = Instantiate(globePrefab, placementPose.position, placementPose.rotation);
            spawnedGlobe.transform.localScale = Vector3.one * defaultGlobeDiameter;

            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }

            // Notifica telemetria
            TelemetryLogger.Instance?.RegisterSessionStart();
        }

        public void PlaceGlobeInFrontOfCamera()
        {
            if (spawnedGlobe != null || globePrefab == null) return;
            Vector3 spawnPos = Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f : new Vector3(0, 0, 1.5f);
            spawnedGlobe = Instantiate(globePrefab, spawnPos, Quaternion.identity);
            spawnedGlobe.transform.localScale = Vector3.one * defaultGlobeDiameter;
            TelemetryLogger.Instance?.RegisterSessionStart();
        }
    }
}
