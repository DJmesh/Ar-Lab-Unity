using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARLab.Geography
{
    [Serializable]
    public class QuizQuestion
    {
        public string promptText = "Onde mora o Pinguim?";
        public WorldRegion targetRegion = WorldRegion.Antarctica;
        public AudioClip promptAudioClip;
        public AudioClip successAudioClip;
    }

    /// <summary>
    /// Gerencia o minijogo gamificado 'Desafio do Explorador' para crianças pré-escolares.
    /// Propõe desafios de identificação de fauna/regiões e registra acertos e tentativas na telemetria.
    /// </summary>
    public class ExplorerQuizManager : MonoBehaviour
    {
        public static ExplorerQuizManager Instance { get; private set; }

        [Header("Lista de Desafios Geográficos")]
        public List<QuizQuestion> questions = new List<QuizQuestion>();

        [Header("Áudio e Feedback")]
        public AudioSource audioSource;
        public AudioClip defaultSuccessFanfare;
        public AudioClip gentleTryAgainClip;

        [Header("Interface Opcional")]
        public GameObject quizPanel;
        public Text questionPromptText;

        private int currentQuestionIndex = 0;
        private int attemptsOnCurrentQuestion = 0;
        private int totalCorrectAnswers = 0;
        private bool isQuizActive = false;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        void Start()
        {
            InitializeDefaultQuestions();
        }

        private void InitializeDefaultQuestions()
        {
            if (questions.Count == 0)
            {
                questions.Add(new QuizQuestion { promptText = "Onde mora o Pinguim? Gire o globo e encontre o gelo!", targetRegion = WorldRegion.Antarctica });
                questions.Add(new QuizQuestion { promptText = "Onde mora a Onça-Pintada e a Floresta Amazônica?", targetRegion = WorldRegion.SouthAmerica });
                questions.Add(new QuizQuestion { promptText = "Onde vive o grande Leão nas savanas?", targetRegion = WorldRegion.Africa });
                questions.Add(new QuizQuestion { promptText = "Onde pula o Canguru?", targetRegion = WorldRegion.Oceania });
            }
        }

        public void StartQuiz()
        {
            isQuizActive = true;
            currentQuestionIndex = 0;
            totalCorrectAnswers = 0;
            attemptsOnCurrentQuestion = 0;

            if (quizPanel != null) quizPanel.SetActive(true);
            PlayCurrentQuestion();
        }

        private void PlayCurrentQuestion()
        {
            if (currentQuestionIndex >= questions.Count)
            {
                EndQuiz();
                return;
            }

            var q = questions[currentQuestionIndex];
            attemptsOnCurrentQuestion = 0;

            if (questionPromptText != null)
            {
                questionPromptText.text = q.promptText;
            }

            if (q.promptAudioClip != null && audioSource != null)
            {
                audioSource.clip = q.promptAudioClip;
                audioSource.Play();
            }
        }

        public void OnRegionSelected(WorldRegion selectedRegion)
        {
            if (!isQuizActive || currentQuestionIndex >= questions.Count) return;

            attemptsOnCurrentQuestion++;
            var q = questions[currentQuestionIndex];

            if (selectedRegion == q.targetRegion)
            {
                // Acerto!
                totalCorrectAnswers++;
                TelemetryLogger.Instance?.RegisterQuizAttempt(q.targetRegion.ToString(), true, attemptsOnCurrentQuestion);

                if (audioSource != null && defaultSuccessFanfare != null)
                {
                    audioSource.PlayOneShot(defaultSuccessFanfare);
                }

                StartCoroutine(NextQuestionDelayRoutine());
            }
            else
            {
                // Erro com mediação suave
                TelemetryLogger.Instance?.RegisterQuizAttempt(selectedRegion.ToString(), false, attemptsOnCurrentQuestion);

                if (audioSource != null && gentleTryAgainClip != null)
                {
                    audioSource.PlayOneShot(gentleTryAgainClip);
                }
            }
        }

        private IEnumerator NextQuestionDelayRoutine()
        {
            yield return new WaitForSeconds(2.0f);
            currentQuestionIndex++;
            PlayCurrentQuestion();
        }

        private void EndQuiz()
        {
            isQuizActive = false;
            if (quizPanel != null) quizPanel.SetActive(false);

            // Notifica telemetria do resultado final
            TelemetryLogger.Instance?.RegisterQuizFinished(totalCorrectAnswers, questions.Count);

            // Abre a tela de avaliação pictórica (Smilyometer)
            SmilyometerUI.Instance?.ShowSmilyometer();
        }
    }
}
