using System.Collections;
using UnityEngine;

namespace ModernRogue
{
    public static class PlayerVisualFeedback2D
    {
        public static void PlayHit(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return;
            }

            HeroClass heroClass = RunProgressionSystem.Instance != null
                ? RunProgressionSystem.Instance.CurrentClass
                : HeroClass.Warrior;
            if (heroClass == HeroClass.Warrior)
            {
                TinySwordsPlayerVisual2D warriorVisual = playerTransform.GetComponent<TinySwordsPlayerVisual2D>();
                if (warriorVisual != null)
                {
                    warriorVisual.PlayHitFlash();
                }
            }
            else
            {
                SKCharacterVisual2D skVisual = playerTransform.GetComponent<SKCharacterVisual2D>();
                if (skVisual != null)
                {
                    skVisual.PlayHitFlash();
                }
            }
        }

        public static void PlayDeath(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return;
            }

            SKCharacterVisual2D skVisual = playerTransform.GetComponent<SKCharacterVisual2D>();
            if (skVisual != null)
            {
                skVisual.PlayDeathPose();
            }

            TinySwordsPlayerVisual2D warriorVisual = playerTransform.GetComponent<TinySwordsPlayerVisual2D>();
            if (warriorVisual != null)
            {
                warriorVisual.PlayDeathPose();
            }
        }

        public static void ResetPose(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return;
            }

            SKCharacterVisual2D skVisual = playerTransform.GetComponent<SKCharacterVisual2D>();
            if (skVisual != null)
            {
                skVisual.ResetDeathPose();
            }

            TinySwordsPlayerVisual2D warriorVisual = playerTransform.GetComponent<TinySwordsPlayerVisual2D>();
            if (warriorVisual != null)
            {
                warriorVisual.ResetDeathPose();
            }
        }

        public static void PlayReviveBurst(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return;
            }

            ResetPose(playerTransform);
            MonoBehaviour host = playerTransform.GetComponent<MonoBehaviour>();
            if (host != null)
            {
                host.StartCoroutine(ReviveFlashRoutine(playerTransform));
            }
        }

        private static IEnumerator ReviveFlashRoutine(Transform playerTransform)
        {
            SpriteRenderer[] renderers = playerTransform.GetComponentsInChildren<SpriteRenderer>(true);
            Color[] original = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                original[i] = renderers[i] != null ? renderers[i].color : Color.white;
            }

            for (int pulse = 0; pulse < 2; pulse++)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].color = new Color(0.75f, 1f, 0.85f, original[i].a);
                    }
                }

                yield return new WaitForSeconds(0.08f);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].color = original[i];
                    }
                }

                yield return new WaitForSeconds(0.08f);
            }
        }
    }
}
