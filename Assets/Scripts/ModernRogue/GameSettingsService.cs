using UnityEngine;

namespace ModernRogue
{
    public static class GameSettingsService
    {
        private const string MusicVolumeKey = "ModernRogue_MusicVolume";
        private const string SfxVolumeKey = "ModernRogue_SfxVolume";

        public static readonly string[] BindableActions =
        {
            "Attack",
            "Block",
            "Dodge",
            "Inventory",
            "MenuToggle",
            "QuickSlot1",
            "QuickSlot2",
            "QuickSlot3"
        };

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, 0.65f);
            set
            {
                PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 0.85f);
            set
            {
                PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        public static string GetKeyLabel(string actionId)
        {
            return GetKeyDisplayName(actionId);
        }

        public static string GetKeyDisplayName(string actionId)
        {
            KeyCode key = GetKey(actionId);
            if (key == KeyCode.Mouse0)
            {
                return "鼠标左键";
            }

            if (key == KeyCode.Mouse1)
            {
                return "鼠标右键";
            }

            if (key == KeyCode.Mouse2)
            {
                return "鼠标中键";
            }

            if (key == KeyCode.Space)
            {
                return "空格";
            }

            if (key == KeyCode.Escape)
            {
                return "ESC";
            }

            if (key.ToString().StartsWith("Alpha"))
            {
                return key.ToString().Replace("Alpha", "");
            }

            return key.ToString();
        }

        public static string GetActionDisplayName(string actionId)
        {
            switch (actionId)
            {
                case "Attack":
                    return "攻击";
                case "Block":
                    return "格挡 / 副武器";
                case "Dodge":
                    return "闪避";
                case "Inventory":
                    return "打开背包";
                case "MenuToggle":
                    return "开关总面板";
                case "QuickSlot1":
                    return "快捷道具 1";
                case "QuickSlot2":
                    return "快捷道具 2";
                case "QuickSlot3":
                    return "快捷道具 3";
                default:
                    return actionId;
            }
        }

        public static bool WasPressed(string actionId)
        {
            KeyCode key = GetKey(actionId);
            return key != KeyCode.None && Input.GetKeyDown(key);
        }

        public static bool WasReleased(string actionId)
        {
            KeyCode key = GetKey(actionId);
            return key != KeyCode.None && Input.GetKeyUp(key);
        }

        public static bool IsHeld(string actionId)
        {
            KeyCode key = GetKey(actionId);
            return key != KeyCode.None && Input.GetKey(key);
        }

        public static void ResetKeyBindings()
        {
            for (int i = 0; i < BindableActions.Length; i++)
            {
                PlayerPrefs.DeleteKey("ModernRogue_Key_" + BindableActions[i]);
            }

            PlayerPrefs.Save();
        }

        public static KeyCode GetKey(string actionId)
        {
            string saved = PlayerPrefs.GetString("ModernRogue_Key_" + actionId, string.Empty);
            if (!string.IsNullOrEmpty(saved) && System.Enum.TryParse(saved, out KeyCode key))
            {
                return key;
            }

            return GetDefaultKey(actionId);
        }

        public static void SetKey(string actionId, KeyCode key)
        {
            PlayerPrefs.SetString("ModernRogue_Key_" + actionId, key.ToString());
            PlayerPrefs.Save();
        }

        public static KeyCode GetDefaultKey(string actionId)
        {
            switch (actionId)
            {
                case "Attack":
                    return KeyCode.Mouse0;
                case "Block":
                    return KeyCode.Mouse1;
                case "Dodge":
                    return KeyCode.Space;
                case "Inventory":
                    return KeyCode.B;
                case "MenuToggle":
                    return KeyCode.E;
                case "QuickSlot1":
                    return KeyCode.Alpha1;
                case "QuickSlot2":
                    return KeyCode.Alpha2;
                case "QuickSlot3":
                    return KeyCode.Alpha3;
                case "MoveUp":
                    return KeyCode.W;
                case "MoveDown":
                    return KeyCode.S;
                case "MoveLeft":
                    return KeyCode.A;
                case "MoveRight":
                    return KeyCode.D;
                default:
                    return KeyCode.None;
            }
        }
    }
}
