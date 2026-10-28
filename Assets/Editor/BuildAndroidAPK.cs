using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using System.IO;

namespace ARLab.EditorTools
{
    public static class BuildAndroidAPK
    {
        [MenuItem("AR Lab/5. Gerar APK para Celular (Android)")]
        public static void BuildAPK()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
            {
                Debug.Log("<color=#00E5FF><b>[AR Lab]</b></color> Encerrando Modo Play para iniciar a compilação do APK automaticamente...");
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                EditorApplication.isPlaying = false;
                return;
            }

            ExecuteBuild();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.delayCall += ExecuteBuild;
            }
        }

        private static void ExecuteBuild()
        {
            // 1. Configurar APIs Gráficas Universais (Vulkan + OpenGLES 3 automático para 100% dos celulares Android)
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, true);

            // 2. Arquitetura 64-bit moderna
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // 3. Garante que a cena SampleScene está aberta, gerada e salva no disco
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != scenePath)
            {
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            SetupARLabUI.BuildCompleteUI();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene, scenePath);
            AssetDatabase.SaveAssets();

            // 4. Diretório de saída
            string buildFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds"));
            if (!Directory.Exists(buildFolder))
            {
                Directory.CreateDirectory(buildFolder);
            }

            string apkPath = Path.Combine(buildFolder, "ARLab_GloboTerrestre.apk");
            string[] scenes = new string[] { scenePath };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log("<color=#00E5FF><b>[AR Lab]</b></color> Iniciando compilação da APK com cena salva e gráficos 100% compatíveis...");

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                long sizeBytes = new FileInfo(apkPath).Length;
                long sizeMb = sizeBytes / (1024 * 1024);
                Debug.Log($"<color=#4CAF50><b>[AR Lab SUCESSO]</b></color> Nova APK gerada com sucesso!\n" +
                          $"<b>Local do arquivo:</b> {apkPath}\n" +
                          $"<b>Tamanho real da APK:</b> {sizeMb} MB");
                EditorUtility.RevealInFinder(apkPath);
            }
            else
            {
                Debug.LogError($"[AR Lab Falha] Não foi possível compilar a APK. Resultado: {report.summary.result}");
            }
        }
    }
}
