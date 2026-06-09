using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModernRogue
{
    public static class UiNavigationHelper
    {
        public static bool ShouldBlockEscape => GameSettingsPanelBuilder.IsKeyRebindActive;

        public static bool TryConsumeEscape(UnityAction onBack)
        {
            if (onBack == null || ShouldBlockEscape || !Input.GetKeyDown(KeyCode.Escape))
            {
                return false;
            }

            onBack.Invoke();
            return true;
        }

        public static Button CreateBackButton(Transform parent, UnityAction onBack, Vector2 min, Vector2 max, string label = "返回上一级")
        {
            GameObject obj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            SetRect(obj.GetComponent<RectTransform>(), min, max);
            RuntimeUIVisuals.StyleAccent(obj.GetComponent<Image>(), new Color(0.18f, 0.34f, 0.58f, 1f));
            Text text = CreateText(obj.transform, "Text", label, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one);
            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(onBack);
            return button;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, TextAnchor anchor)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.supportRichText = true;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    [DisallowMultipleComponent]
    public sealed class ModalPanelNavigation : MonoBehaviour
    {
        private GameObject panelRoot;

        public void Initialize(GameObject root)
        {
            panelRoot = root != null ? root : gameObject;
        }

        private void Update()
        {
            UiNavigationHelper.TryConsumeEscape(Close);
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                Destroy(panelRoot);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
