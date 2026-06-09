using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    public class SKHealthBar2D : MonoBehaviour
    {
        private Bagsys.RogueLike.CharacterStats stats;
        private GameObject barRoot;
        private CanvasGroup group;
        private RectTransform fillRect;
        private int lastHp;
        private float visibleUntil;
        private float displayedRatio = 1f;

        private void Awake()
        {
            RemoveLegacyBars();
            stats = GetComponent<Bagsys.RogueLike.CharacterStats>();
            BuildBar();
            lastHp = stats != null ? stats.CurrentHP : 0;
            SetVisible(false, true);
        }

        private void LateUpdate()
        {
            if (stats == null || fillRect == null || group == null)
            {
                return;
            }

            float target = stats.MaxHP > 0 ? stats.CurrentHP / (float)stats.MaxHP : 0f;
            if (stats.CurrentHP < lastHp)
            {
                ShowForSeconds(3f);
            }

            lastHp = stats.CurrentHP;
            displayedRatio = Mathf.Lerp(displayedRatio, target, Time.deltaTime * 14f);
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(displayedRatio), 1f);
            SetVisible(Time.time < visibleUntil && stats.CurrentHP > 0, false);
        }

        public void ShowForSeconds(float seconds)
        {
            if (stats == null || stats.CurrentHP <= 0)
            {
                return;
            }

            visibleUntil = Time.time + Mathf.Max(0.1f, seconds);
            SetVisible(true, true);
        }

        private void BuildBar()
        {
            GameObject canvasObject = new GameObject("SKHealthBar2D", typeof(RectTransform), typeof(Canvas));
            barRoot = canvasObject;
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = new Vector3(0f, 0.82f, 0f);
            canvasObject.transform.localScale = Vector3.one * 0.012f;
            group = canvasObject.AddComponent<CanvasGroup>();

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 30;

            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(82f, 12f);

            GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(canvasObject.transform, false);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.04f, 0.9f);

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(canvasObject.transform, false);
            fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            Image fill = fillObject.GetComponent<Image>();
            fill.color = new Color(0.95f, 0.15f, 0.12f, 1f);
        }

        private void RemoveLegacyBars()
        {
            SmoothWorldHealthBar smooth = GetComponent<SmoothWorldHealthBar>();
            if (smooth != null)
            {
                Destroy(smooth);
            }

            Bagsys.RogueLike.FloatingHealthBar floating = GetComponent<Bagsys.RogueLike.FloatingHealthBar>();
            if (floating != null)
            {
                Destroy(floating);
            }

            Transform smoothChild = transform.Find("SmoothHealthBar");
            if (smoothChild != null)
            {
                Destroy(smoothChild.gameObject);
            }

            Transform floatingChild = transform.Find("FloatingHealthBar");
            if (floatingChild != null)
            {
                Destroy(floatingChild.gameObject);
            }
        }

        private void SetVisible(bool visible, bool instant)
        {
            if (group == null)
            {
                return;
            }

            if (barRoot != null && barRoot.activeSelf != visible)
            {
                barRoot.SetActive(visible);
            }

            group.alpha = visible ? 1f : 0f;
        }
    }

    public class DamagePopup2D : MonoBehaviour
    {
        private TextMesh text;
        private Color color;
        private float age;

        public static void Spawn(Vector3 position, int damage)
        {
            GameObject obj = new GameObject("DamagePopup2D", typeof(TextMesh), typeof(DamagePopup2D));
            obj.transform.position = position + new Vector3(Random.Range(-0.15f, 0.15f), 0.8f, 0f);
            TextMesh text = obj.GetComponent<TextMesh>();
            text.text = damage.ToString();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 42;
            text.characterSize = 0.04f;
            text.color = new Color(1f, 0.16f, 0.12f, 1f);
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 40;
            }
        }

        private void Awake()
        {
            text = GetComponent<TextMesh>();
            color = text != null ? text.color : Color.red;
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.position += new Vector3(0.15f, 1.4f, 0f) * Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.75f, age / 0.55f);
            if (text != null)
            {
                color.a = Mathf.Lerp(1f, 0f, age / 0.55f);
                text.color = color;
            }

            if (age >= 0.55f)
            {
                Destroy(gameObject);
            }
        }
    }

    public static class HitFlash2D
    {
        public static void Flash(MonoBehaviour owner, SpriteRenderer renderer, Color restoreColor)
        {
            if (owner != null && renderer != null)
            {
                owner.StartCoroutine(FlashRoutine(renderer, restoreColor));
            }
        }

        private static IEnumerator FlashRoutine(SpriteRenderer renderer, Color restoreColor)
        {
            renderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            if (renderer != null)
            {
                renderer.color = restoreColor;
            }
        }
    }
}
