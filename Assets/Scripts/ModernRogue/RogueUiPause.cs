using UnityEngine;
using UnityEngine.EventSystems;

namespace ModernRogue
{
    public static class RogueUiPause
    {
        private static int pauseCount;

        public static bool IsPaused => pauseCount > 0;
        public static bool BlocksGameplayInput => IsPaused || Time.timeScale <= 0f || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());

        public static void Push()
        {
            pauseCount++;
            Time.timeScale = 0f;
        }

        public static void Pop()
        {
            pauseCount = Mathf.Max(0, pauseCount - 1);
            if (pauseCount == 0)
            {
                Time.timeScale = 1f;
            }
        }
    }

    public sealed class ModalPauseToken : MonoBehaviour
    {
        private bool active;

        private void OnEnable()
        {
            if (active)
            {
                return;
            }

            active = true;
            RogueUiPause.Push();
        }

        private void OnDisable()
        {
            Release();
        }

        private void OnDestroy()
        {
            Release();
        }

        private void Release()
        {
            if (!active)
            {
                return;
            }

            active = false;
            RogueUiPause.Pop();
        }
    }
}
