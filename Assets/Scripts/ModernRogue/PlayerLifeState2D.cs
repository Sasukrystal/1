using System.Collections;
using UnityEngine;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public sealed class PlayerLifeState2D : MonoBehaviour
    {
        public static PlayerLifeState2D Instance { get; private set; }

        private Rigidbody2D body;
        private bool deadOrDying;
        private Coroutine deathRoutine;

        public bool IsDeadOrDying => deadOrDying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            body = GetComponent<Rigidbody2D>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static PlayerLifeState2D Ensure(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return Instance;
            }

            PlayerLifeState2D state = playerTransform.GetComponent<PlayerLifeState2D>();
            if (state == null)
            {
                state = playerTransform.gameObject.AddComponent<PlayerLifeState2D>();
            }

            return state;
        }

        public void ResetForRun()
        {
            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }

            deadOrDying = false;
            ResetVisualPose();
            if (body != null)
            {
                body.simulated = true;
            }
        }

        public void BeginDeathSequence()
        {
            if (deadOrDying)
            {
                return;
            }

            if (RunProgressionSystem.Instance == null || !RunProgressionSystem.Instance.IsRunActive)
            {
                return;
            }

            deadOrDying = true;
            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = false;
            }

            PlayerVisualFeedback2D.PlayDeath(transform);
            GameAudioService.Ensure()?.PlayHeroDeath();
            deathRoutine = StartCoroutine(DeathSequenceRoutine());
        }

        private IEnumerator DeathSequenceRoutine()
        {
            yield return new WaitForSecondsRealtime(0.95f);
            if (SoulKnightDirector.Instance != null)
            {
                SoulKnightDirector.Instance.ShowDeathSettlement();
            }

            deathRoutine = null;
        }

        private void ResetVisualPose()
        {
            PlayerVisualFeedback2D.ResetPose(transform);
        }
    }
}
