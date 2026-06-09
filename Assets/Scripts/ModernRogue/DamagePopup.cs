using UnityEngine;
using UnityEngine.UI;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public class DamagePopup : MonoBehaviour
    {
        private Text text;
        private float age;
        private const float Lifetime = 0.5f;
        private Vector3 drift;

        public static void Spawn(Vector3 worldPosition, int damage)
        {
            GameObject canvasObject = new GameObject("DamagePopupCanvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.position = worldPosition;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(120f, 48f);
            canvasRect.localScale = Vector3.one * 0.014f;

            GameObject textObject = new GameObject("DamageText", typeof(RectTransform), typeof(Text), typeof(DamagePopup));
            textObject.transform.SetParent(canvasObject.transform, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.18f, 0.12f, 1f);
            text.text = "-" + damage;
        }

        private void Awake()
        {
            text = GetComponent<Text>();
            drift = new Vector3(Random.Range(-0.4f, 0.4f), 1.25f, 0f);
        }

        private void Update()
        {
            age += Time.deltaTime;
            Transform canvasTransform = transform.parent != null ? transform.parent : transform;
            canvasTransform.position += drift * Time.deltaTime;

            if (Camera.main != null)
            {
                canvasTransform.rotation = Quaternion.LookRotation(canvasTransform.position - Camera.main.transform.position, Vector3.up);
            }

            float t = Mathf.Clamp01(age / Lifetime);
            transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 0.72f, t);

            if (text != null)
            {
                Color color = text.color;
                color.a = 1f - t;
                text.color = color;
            }

            if (age >= Lifetime)
            {
                Destroy(canvasTransform.gameObject);
            }
        }
    }
}
