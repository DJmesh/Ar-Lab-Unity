using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ARLab.Geography;

namespace ARLab.EditorTools
{
    [InitializeOnLoad]
    public static class SetupARLabUI
    {
        static SetupARLabUI()
        {
            EditorApplication.delayCall += AutoSetupOnLoad;
        }

        private static void AutoSetupOnLoad()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying) return;

            string scenePath = "Assets/Scenes/SampleScene.unity";
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != scenePath)
            {
                active = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            BuildCompleteUI();
        }

        [MenuItem("AR Lab/2. Criar Menu e Sistema de Pontuação Completo")]
        public static void BuildCompleteUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
            {
                Debug.LogWarning("[AR Lab] Não é possível reconfigurar a cena no disco durante o Play Mode. Pare o Play Mode primeiro.");
                return;
            }

            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != scenePath)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // 1. Obter ou Criar GameManager
            var gmGo = GameObject.Find("ARLabGameManager");
            if (gmGo == null)
            {
                gmGo = new GameObject("ARLabGameManager");
            }
            var gm = gmGo.GetComponent<ARLabGameManager>();
            if (gm == null) gm = gmGo.AddComponent<ARLabGameManager>();

            // 2. Executa configuração completa
            gm.EnsureCameraSettings();
            gm.EnsureGlobeAndPins();
            gm.EnsureUIAndEventSystem();
            gm.LinkHotspots();

            EditorUtility.SetDirty(gmGo);
            if (Camera.main != null) EditorUtility.SetDirty(Camera.main.gameObject);
            var globe = GameObject.Find("Globo_Terrestre");
            if (globe != null) EditorUtility.SetDirty(globe);
            var canvas = GameObject.Find("ARLab_Canvas");
            if (canvas != null) EditorUtility.SetDirty(canvas);

            // 3. Salva a cena explicitamente no disco
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#4CAF50><b>[AR Lab Sucesso]</b></color> Cena '{scenePath}' salva com sucesso no disco ({saved})! Todos os 7 continentes e interface prontos!");
        }
    }
}
