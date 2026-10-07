using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ARLab.Geography
{
    [Serializable]
    public class TelemetrySessionData
    {
        public string participantId = "P01";
        public string timestamp = "";
        public float sessionDurationSeconds = 0f;
        public int fullRotationsCount = 0;
        public int hotspotClicksCount = 0;
        public List<string> clickedHotspots = new List<string>();
        public int quizScore = 0;
        public int quizTotal = 0;
        public int smilyometerRating = 0; // 1 a 5
    }

    /// <summary>
    /// Registra telemetria assíncrona local para sustentar a pesquisa experimental.
    /// Funciona 100% offline em conformidade com a LGPD (dados anonimizados).
    /// </summary>
    public class TelemetryLogger : MonoBehaviour
    {
        public static TelemetryLogger Instance { get; private set; }

        public string currentParticipantId = "P01";
        private TelemetrySessionData currentSession;
        private float sessionStartTime;
        private string filePath;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            filePath = Path.Combine(Application.persistentDataPath, "ar_globo_telemetry.json");
        }

        public void RegisterSessionStart()
        {
            currentSession = new TelemetrySessionData
            {
                participantId = currentParticipantId,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            sessionStartTime = Time.time;
            Debug.Log($"[Telemetry] Sessão iniciada para {currentParticipantId}");
        }

        public void RegisterRotationCompleted()
        {
            if (currentSession != null)
            {
                currentSession.fullRotationsCount++;
            }
        }

        public void RegisterHotspotClick(string regionName)
        {
            if (currentSession != null)
            {
                currentSession.hotspotClicksCount++;
                currentSession.clickedHotspots.Add(regionName);
            }
        }

        public void RegisterQuizAttempt(string region, bool success, int attemptNumber)
        {
            Debug.Log($"[Telemetry] Quiz Tentativa: Região={region}, Sucesso={success}, Tentativa={attemptNumber}");
        }

        public void RegisterQuizFinished(int score, int total)
        {
            if (currentSession != null)
            {
                currentSession.quizScore = score;
                currentSession.quizTotal = total;
            }
        }

        public void RegisterSmilyometerRating(int rating)
        {
            if (currentSession != null)
            {
                currentSession.smilyometerRating = rating;
                currentSession.sessionDurationSeconds = Time.time - sessionStartTime;
                SaveTelemetryToFile();
            }
        }

        private void SaveTelemetryToFile()
        {
            try
            {
                string json = JsonUtility.ToJson(currentSession, true);
                File.AppendAllText(filePath, json + "\n---\n");
                Debug.Log($"[Telemetry] Dados salvos com sucesso em: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Telemetry] Erro ao salvar log: {ex.Message}");
            }
        }
    }
}
