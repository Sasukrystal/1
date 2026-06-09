using UnityEngine;
using System.Collections;

namespace Bagsys.RogueLike
{
    public class CharacterStats : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] protected int maxHP = 100;
        [SerializeField] protected int currentHP = 100;
        [SerializeField] protected int atk = 10;
        [SerializeField] protected int def = 5;

        protected bool isDead;
        private Coroutine hitFlashRoutine;

        public int MaxHP => maxHP;
        public int CurrentHP => currentHP;
        public int ATK => atk;
        public int DEF => def;
        public bool IsDead => isDead;

        protected virtual void Awake()
        {
            if (maxHP < 1)
            {
                maxHP = 1;
            }

            if (currentHP <= 0)
            {
                currentHP = maxHP;
            }

            currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        }

        public virtual void TakeDamage(int damage)
        {
            if (isDead || damage <= 0)
            {
                return;
            }

            int finalDamage = Mathf.Max(1, damage - def);
            currentHP = Mathf.Max(0, currentHP - finalDamage);
            PlaySpriteHitFlash();

            Debug.Log($"{name} took {finalDamage} damage. HP: {currentHP}/{maxHP}");

            if (currentHP <= 0)
            {
                Die();
            }
        }

        public virtual void Heal(int amount)
        {
            if (isDead || amount <= 0)
            {
                return;
            }

            currentHP = Mathf.Min(maxHP, currentHP + amount);
        }

        protected virtual void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            Debug.Log($"{name} died.");
        }

        private void PlaySpriteHitFlash()
        {
            SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                return;
            }

            if (hitFlashRoutine != null)
            {
                StopCoroutine(hitFlashRoutine);
            }

            hitFlashRoutine = StartCoroutine(SpriteHitFlashRoutine(spriteRenderers));
        }

        private IEnumerator SpriteHitFlashRoutine(SpriteRenderer[] spriteRenderers)
        {
            Color[] originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null)
                {
                    continue;
                }

                originalColors[i] = spriteRenderers[i].color;
                if (originalColors[i].a > 0.05f)
                {
                    spriteRenderers[i].color = Color.white;
                }
            }

            yield return new WaitForSeconds(0.1f);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = originalColors[i];
                }
            }

            hitFlashRoutine = null;
        }
    }
}
