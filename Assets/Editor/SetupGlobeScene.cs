using UnityEngine;
using UnityEditor;
using ARLab.Geography;

namespace ARLab.EditorTools
{
    public static class SetupGlobeScene
    {
        [MenuItem("AR Lab/1. Posicionar Globo 3D na Cena")]
        public static void CreateOrSetupGlobe()
        {
            // 1. Configurar Câmera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = new GameObject("Main Camera");
                mainCam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            mainCam.transform.position = new Vector3(0, 1f, -10f);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.05f, 0.08f, 0.14f); // Espaço cósmico escuro

            // 2. Procurar ou criar o Globo
            GameObject globeGo = GameObject.Find("Globo_Terrestre");
            if (globeGo == null)
            {
                globeGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                globeGo.name = "Globo_Terrestre";
            }

            globeGo.transform.position = new Vector3(0, 1f, -6f);
            globeGo.transform.rotation = Quaternion.Euler(0, 180f, 0); // Focado na América do Sul
            globeGo.transform.localScale = Vector3.one * 2.2f;

            // 3. Aplicar Material da Terra (URP Lit com Textura Real)
            Material earthMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Geography/Earth_Material.mat");
            if (earthMat == null)
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                earthMat = new Material(urpLit != null ? urpLit : Shader.Find("Standard"));
                earthMat.name = "Earth_Material";
                AssetDatabase.CreateAsset(earthMat, "Assets/Models/Geography/Earth_Material.mat");
            }
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Geography/earth_texture.jpg");
            if (tex != null)
            {
                earthMat.mainTexture = tex;
            }
            var renderer = globeGo.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = earthMat;
            }

            // 4. Adicionar GlobeController
            GlobeController controller = globeGo.GetComponent<GlobeController>();
            if (controller == null)
            {
                controller = globeGo.AddComponent<GlobeController>();
            }
            controller.rotationSpeed = 0.5f;
            controller.damping = 0.94f;
            controller.idleRotationSpeed = 12f;

            // 5. Adicionar Collider
            if (globeGo.GetComponent<Collider>() == null)
            {
                globeGo.AddComponent<SphereCollider>();
            }

            Selection.activeGameObject = globeGo;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            EditorUtility.SetDirty(globeGo);
            Debug.Log("<color=#4CAF50><b>[AR Lab Sucesso]</b></color> Globo Terrestre 3D configurado com textura e rotação! Aperte Play (▶️) para girar no Simulator!");
        }
    }
}
