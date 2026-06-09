using UnityEngine;

namespace Bagsys.RogueLike
{
    public static class CombatVfxUtility
    {
        public static void SpawnMeleeArc(Vector3 origin, Vector3 forward, float radius, float arcAngle, Color color, float duration)
        {
            GameObject arcObject = new GameObject("MeleeArcVfx");
            LineRenderer lineRenderer = arcObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = 0.055f;
            lineRenderer.endWidth = 0.018f;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.numCapVertices = 4;
            lineRenderer.material = CreateLineMaterial(color);
            lineRenderer.startColor = new Color(color.r, color.g, color.b, 0.8f);
            lineRenderer.endColor = new Color(color.r, color.g, color.b, 0.05f);

            Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = Vector3.forward;
            }

            Quaternion rotation = Quaternion.LookRotation(flatForward, Vector3.up);
            float halfArc = arcAngle * 0.5f;
            int segments = 14;
            lineRenderer.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.Lerp(-halfArc, halfArc, t);
                Vector3 localPoint = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                lineRenderer.SetPosition(i, origin + rotation * localPoint + Vector3.up * 0.08f);
            }

            Object.Destroy(arcObject, duration > 0f ? duration : 0.12f);
        }

        public static void SpawnImpactBurst(Vector3 position, Color color, int fragmentCount, float forceMagnitude, float lifeTime)
        {
            int count = Mathf.Max(3, fragmentCount);
            for (int i = 0; i < count; i++)
            {
                GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fragment.name = "ImpactFragment";
                Object.Destroy(fragment.GetComponent<Collider>());

                fragment.transform.position = position + Random.insideUnitSphere * 0.06f;
                fragment.transform.localScale = Vector3.one * Random.Range(0.06f, 0.1f);

                Renderer renderer = fragment.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = CreateSolidMaterial(color);
                }

                Rigidbody rigidbody = fragment.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                Vector3 direction = (Random.onUnitSphere + Vector3.up * 0.25f).normalized;
                rigidbody.AddForce(direction * Random.Range(forceMagnitude * 0.65f, forceMagnitude), ForceMode.Impulse);
                rigidbody.AddTorque(Random.onUnitSphere * Random.Range(6f, 16f), ForceMode.Impulse);

                Object.Destroy(fragment, lifeTime > 0f ? lifeTime : 0.25f);
            }
        }

        public static void SpawnProjectileImpact(Vector3 position)
        {
            SpawnImpactBurst(position, new Color(1f, 0.92f, 0.18f, 1f), 5, 3.5f, 0.22f);
        }

        public static void SpawnEnemyDeathBurst(Vector3 position)
        {
            SpawnImpactBurst(position, new Color(0.82f, 0.12f, 0.12f, 1f), 8, 6.5f, 0.45f);
        }

        public static void SpawnHitFlashBurst(Vector3 position)
        {
            SpawnImpactBurst(position, Color.white, 3, 2.5f, 0.18f);
        }

        public static UnityEngine.Material CreateSolidMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            UnityEngine.Material material = new UnityEngine.Material(shader);
            material.color = color;
            return material;
        }

        private static UnityEngine.Material CreateLineMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            UnityEngine.Material material = new UnityEngine.Material(shader);
            material.color = color;
            return material;
        }
    }
}