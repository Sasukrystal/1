using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    public static class ModernRogueLegacyUiSuppressor
    {
        public static void ApplyStartupPresentation(bool menuVisible)
        {
            SetRunHudVisible(!menuVisible);
            SetSoulKnightHudVisible(!menuVisible);

            if (menuVisible)
            {
                Time.timeScale = 0f;
            }
            else if (GameStartMenuPanel.IsVisible)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        public static void SetRunHudVisible(bool visible)
        {
            GameObject runHud = GameObject.Find("RunHud");
            if (runHud != null)
            {
                runHud.SetActive(visible);
            }
        }

        public static void SetSoulKnightHudVisible(bool visible)
        {
            if (SoulKnightDirector.Instance != null)
            {
                SoulKnightDirector.Instance.SetHudVisible(visible);
            }
            else
            {
                GameObject overlay = GameObject.Find("SoulKnightOverlayCanvas");
                if (overlay != null)
                {
                    overlay.SetActive(visible);
                }
            }
        }

        public static void DisableConflictingLegacySystems()
        {
            Bagsys.RogueLike.GameFeelDirector feelDirector = Object.FindObjectOfType<Bagsys.RogueLike.GameFeelDirector>();
            if (feelDirector != null)
            {
                feelDirector.enabled = false;
            }

            DungeonLoopDirector loopDirector = Object.FindObjectOfType<DungeonLoopDirector>();
            if (loopDirector != null)
            {
                Object.Destroy(loopDirector.gameObject);
            }

            SetRunHudVisible(false);
        }
    }
}
