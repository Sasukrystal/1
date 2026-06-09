using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Bagsys.RogueLike
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats))]
    public class FloatingHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.7f, 0f);
        [SerializeField] private Vector2 barSize = new Vector2(1.4f, 0.16f);
        [SerializeField] private Color fillColor = new Color(0.85f, 0.1f, 0.12f, 1f);
        [SerializeField] private Color playerFillColor = new Color(0.12f, 0.52f, 1f, 1f);

        private CharacterStats stats;
        private Canvas canvas;
        private Image fillImage;
        private int lastHp;
        private readonly List<Text> activePopups = new List<Text>();

        private void Awake()
        {
            stats = GetComponent<CharacterStats>();
            lastHp = stats != null ? stats.CurrentHP : 0;
            BuildCanvas();
        }

        private void LateUpdate()
        {
            if (stats == null)
            {
                return;
            }

            if (canvas == null || fillImage == null)
            {
                BuildCanvas();
            }

            if (canvas != null)
            {
                canvas.transform.position = transform.position + worldOffset;
                if (Camera.main != null)
                {
                    canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - Camera.main.transform.position, Vector3.up);
                }
            }

            float ratio = stats.MaxHP > 0 ? Mathf.Clamp01(stats.CurrentHP / (float)stats.MaxHP) : 0f;
            fillImage.fillAmount = ratio;

            if (stats.CurrentHP < lastHp)
            {
                SpawnDamagePopup(lastHp - stats.CurrentHP);
            }

            lastHp = stats.CurrentHP;
            TickPopups();
        }

        private void BuildCanvas()
        {
            Transform existing = transform.Find("FloatingHealthBar");
            if (existing != null)
            {
                canvas = existing.GetComponent<Canvas>();
                fillImage = existing.Find("Frame/Fill")?.GetComponent<Image>();
                return;
            }

            GameObject canvasObject = new GameObject("FloatingHealthBar", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 40;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(160f, 36f);
            canvasRect.localScale = Vector3.one * 0.012f;

            GameObject frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(canvasObject.transform, false);
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.sizeDelta = barSize * 100f;
            frame.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.025f, 0.88f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(frame.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            fillImage = fill.GetComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.color = CompareTag("Player") ? playerFillColor : fillColor;
        }

        private void SpawnDamagePopup(int damage)
        {
            if (canvas == null || damage <= 0)
            {
                return;
            }

            GameObject popupObject = new GameObject("DamagePopup", typeof(RectTransform), typeof(Text));
            popupObject.transform.SetParent(canvas.transform, false);
            RectTransform popupRect = popupObject.GetComponent<RectTransform>();
            popupRect.anchoredPosition = new Vector2(Random.Range(-18f, 18f), 30f);
            popupRect.sizeDelta = new Vector2(90f, 32f);

            Text text = popupObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.18f, 0.12f, 1f);
            text.text = "-" + damage;
            activePopups.Add(text);
        }

        private void TickPopups()
        {
            for (int i = activePopups.Count - 1; i >= 0; i--)
            {
                Text popup = activePopups[i];
                if (popup == null)
                {
                    activePopups.RemoveAt(i);
                    continue;
                }

                RectTransform rect = popup.rectTransform;
                rect.anchoredPosition += Vector2.up * 34f * Time.deltaTime;
                Color color = popup.color;
                color.a -= Time.deltaTime * 1.8f;
                popup.color = color;

                if (color.a <= 0f)
                {
                    Destroy(popup.gameObject);
                    activePopups.RemoveAt(i);
                }
            }
        }
    }
}
