using UnityEngine;

namespace ARLab.Geography
{
    /// <summary>
    /// Animação lúdica suave para os animais 3D nos continentes:
    /// - Rotação suave contínua em torno do próprio eixo (360° para visualização completa).
    /// - Micro-flutuação/respiração suave (bobbing).
    /// - Efeito de salto alegre ao ser clicado/encontrado.
    /// </summary>
    public class Animal3DRotator : MonoBehaviour
    {
        [Tooltip("Velocidade de rotação suave em graus por segundo")]
        public float rotationSpeed = 24f;

        [Tooltip("Amplitude da flutuação suave")]
        public float bobbingAmplitude = 0.004f;

        private Vector3 initialLocalPos;
        private bool isJumping = false;

        void Start()
        {
            initialLocalPos = transform.localPosition;
        }

        void Update()
        {
            if (isJumping) return;

            // Rotação contínua charmosa para que a criança veja o animal de todos os ângulos
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);

            // Flutuação lúdica de respiração
            float bob = Mathf.Sin(Time.time * 2.8f) * bobbingAmplitude;
            transform.localPosition = initialLocalPos + new Vector3(0, bob, 0);
        }

        /// <summary>
        /// Salto alegre de comemoração quando a criança acerta a missão
        /// </summary>
        public void PlayHappyJump()
        {
            StartCoroutine(HappyJumpRoutine());
        }

        private System.Collections.IEnumerator HappyJumpRoutine()
        {
            isJumping = true;
            Vector3 startPos = transform.localPosition;
            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float height = Mathf.Sin(t * Mathf.PI) * 0.04f;
                transform.localPosition = startPos + new Vector3(0, height, 0);
                transform.Rotate(Vector3.up, 360f * (Time.deltaTime / duration), Space.Self);
                yield return null;
            }

            transform.localPosition = startPos;
            isJumping = false;
        }
    }
}
