using UnityEngine;
using UnityEditor;

namespace ARLab.EditorTools
{
    public static class FindContinentCoordinates
    {
        [MenuItem("AR Lab/3. Diagnosticar Direcao da Camera")]
        public static void DiagnoseCameraDirection()
        {
            var globe = GameObject.Find("Globo_Terrestre");
            if (globe == null || Camera.main == null)
            {
                Debug.LogError("Globo_Terrestre ou Camera.main não encontrados!");
                return;
            }

            // O vetor que aponta DO centro do globo PARA a câmera
            Vector3 worldDirToCam = (Camera.main.transform.position - globe.transform.position).normalized;

            // Esse vetor no espaço LOCAL do globo (exatamente onde a câmera está olhando na esfera!)
            Vector3 localFacing = globe.transform.InverseTransformDirection(worldDirToCam).normalized;

            Debug.Log($"<color=#00E5FF><b>[DIAGNÓSTICO AR LAB]</b></color>\n" +
                      $"Vetor LOCAL voltado para a câmera: <b>new Vector3({localFacing.x:F3}f, {localFacing.y:F3}f, {localFacing.z:F3}f)</b>\n" +
                      $"Rotação atual do Globo: {globe.transform.eulerAngles}");
        }
    }
}
