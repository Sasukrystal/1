using System.Collections;
using UnityEngine;

namespace ModernRogue
{
    [DefaultExecutionOrder(1000)]
    public sealed class ModernRoguePlayModeEntry : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            GameObject host = new GameObject("ModernRoguePlayModeEntry");
            host.hideFlags = HideFlags.HideAndDontSave;
            host.AddComponent<ModernRoguePlayModeEntry>();
        }

        private void Start()
        {
            StartCoroutine(EnterPlayModeRoutine());
        }

        private static IEnumerator EnterPlayModeRoutine()
        {
            ModernRogueBootstrapper.EnsureRuntime();
            ModernRogueLegacyUiSuppressor.DisableConflictingLegacySystems();

            yield return null;
            yield return null;
            yield return null;

            ModernRogueLegacyUiSuppressor.DisableConflictingLegacySystems();

            SoulKnightDirector director = SoulKnightDirector.Instance;
            if (director == null)
            {
                director = Object.FindObjectOfType<SoulKnightDirector>();
            }

            if (director != null)
            {
                director.EnsureStartupMenuVisible();
            }

            Object.Destroy(FindObjectOfType<ModernRoguePlayModeEntry>()?.gameObject);
        }
    }
}
