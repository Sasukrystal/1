using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public class SmoothWorldHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.85f, 0f);
        [SerializeField] private Color playerColor = new Color(0.2f, 0.55f, 1f, 1f);
        [SerializeField] private Color enemyColor = new Color(0.9f, 0.12f, 0.12f, 1f);

        private Canvas canvas;
        private Image fill;
        private Bagsys.RogueLike.CharacterStats stats;
        private float displayedRatio = 1f;
        private int lastHp = -1;
        private float shake;

        private void Awake()
        {
            stats = GetComponent<Bagsys.RogueLike.CharacterStats>();
            Build();
            DisableOldFloatingBar();
        }

        private void LateUpdate()
        {
            if (stats == null)
            {
                return;
            }

            if (canvas == null || fill == null)
            {
                Build();
            }

            int currentHp = stats.CurrentHP;
            int maxHp = Mathf.Max(1, stats.MaxHP);
            if (lastHp >= 0 && currentHp < lastHp)
            {
                DamagePopup.Spawn(transform.position + offset + Vector3.up * 0.35f, lastHp - currentHp);
                shake = 0.18f;
            }

            lastHp = currentHp;
            float targetRatio = Mathf.Clamp01(currentHp / (float)maxHp);
            displayedRatio = Mathf.Lerp(displayedRatio, targetRatio, Time.deltaTime * 10f);
            fill.fillAmount = displayedRatio;

            Vector3 shakeOffset = shake > 0f ? Random.insideUnitSphere * shake * 0.08f : Vector3.zero;
            shake = Mathf.Max(0f, shake - Time.deltaTime);

            canvas.transform.position = transform.position + offset + shakeOffset;
            if (Camera.main != null)
            {
                canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - Camera.main.transform.position, Vector3.up);
            }
        }

        private void Build()
        {
            Transform existing = transform.Find("SmoothHealthBar");
            if (existing != null)
            {
                canvas = existing.GetComponent<Canvas>();
                fill = existing.Find("Frame/Fill")?.GetComponent<Image>();
                return;
            }

            GameObject canvasObject = new GameObject("SmoothHealthBar", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 55;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(140f, 26f);
            canvasRect.localScale = Vector3.one * 0.012f;

            GameObject frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(canvasObject.transform, false);
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            frame.GetComponent<Image>().color = new Color(0.02f, 0.025f, 0.035f, 0.9f);

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(frame.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            fill = fillObject.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = CompareTag("Player") ? playerColor : enemyColor;
        }

        private void DisableOldFloatingBar()
        {
            Bagsys.RogueLike.FloatingHealthBar old = GetComponent<Bagsys.RogueLike.FloatingHealthBar>();
            if (old != null)
            {
                old.enabled = false;
            }
        }
    }
}
