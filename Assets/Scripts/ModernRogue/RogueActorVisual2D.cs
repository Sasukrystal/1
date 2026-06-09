using UnityEngine;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public sealed class RogueActorVisual2D : MonoBehaviour
    {
        private RuntimeSpriteAnimator2D animator;
        private Rigidbody2D body;
        private float actionUntil;
        private Vector3 lastPosition;

        private void Awake()
        {
            animator = GetComponent<RuntimeSpriteAnimator2D>();
            if (animator == null)
            {
                animator = gameObject.AddComponent<RuntimeSpriteAnimator2D>();
            }

            body = GetComponent<Rigidbody2D>();
            lastPosition = transform.position;
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            if (Time.time < actionUntil)
            {
                return;
            }

            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;
            if (delta.sqrMagnitude > 0.00004f || (body != null && body.velocity.sqrMagnitude > 0.04f))
            {
                animator.SetState("Move", 7f);
            }
            else
            {
                animator.SetState("Idle", 4f);
            }
        }

        public void Configure(string actorId)
        {
            string folder = ResolveFolder(actorId);
            if (string.IsNullOrEmpty(folder) || animator == null)
            {
                return;
            }

            switch (actorId)
            {
                case "Slime":
                    animator.SetClip("Idle", Art2DUtility.LoadSequence(folder, "Slime_Idle_01", "Slime_Idle_02"));
                    animator.SetClip("Move", Art2DUtility.LoadSequence(folder, "Slime_Move_01", "Slime_Move_02"));
                    animator.SetClip("Attack", Art2DUtility.LoadSequence(folder, "Slime_Attack_Windup", "Slime_Attack_Lunge"));
                    animator.SetClip("Hit", Art2DUtility.LoadSequence(folder, "Slime_Hit_01"));
                    break;
                case "EliteSlime":
                    animator.SetClip("Idle", Art2DUtility.LoadSequence(folder, "EliteSlime_Idle_01", "EliteSlime_Idle_02"));
                    animator.SetClip("Move", Art2DUtility.LoadSequence(folder, "EliteSlime_Move_01", "EliteSlime_Move_01"));
                    animator.SetClip("Attack", Art2DUtility.LoadSequence(folder, "EliteSlime_Attack_Windup", "EliteSlime_Attack_Lunge"));
                    animator.SetClip("Hit", Art2DUtility.LoadSequence(folder, "EliteSlime_Hit_01"));
                    break;
                case "SkeletonArcher":
                    animator.SetClip("Idle", Art2DUtility.LoadSequence(folder, "SkeletonArcher_Idle_01", "SkeletonArcher_Idle_02"));
                    animator.SetClip("Move", Art2DUtility.LoadSequence(folder, "SkeletonArcher_Move_01", "SkeletonArcher_Move_02"));
                    animator.SetClip("Attack", Art2DUtility.LoadSequence(folder, "SkeletonArcher_Aim_01", "SkeletonArcher_Aim_02", "SkeletonArcher_Shoot_01", "SkeletonArcher_Recover_01"));
                    animator.SetClip("Hit", Art2DUtility.LoadSequence(folder, "SkeletonArcher_Hit_01"));
                    break;
                case "Titan":
                    ConfigureBoss(folder, "Titan", "Slam_Windup", "Slam_Impact", "Phase2_Idle", "Phase2_Rage");
                    break;
                case "EmberMage":
                    ConfigureBoss(folder, "EmberMage", "Cast_Windup", "Cast_Fireball", "Cast_FlameCircle", "Phase2_Rage");
                    break;
                case "StormGuard":
                    ConfigureBoss(folder, "StormGuard", "Charge_Windup", "Charge_Impact", "LightningCast_Windup", "Phase2_Overload");
                    break;
                case "BroodQueen":
                    ConfigureBoss(folder, "BroodQueen", "Summon_Windup", "Summon_Release", "PoisonSpit_Windup", "Phase2_SwarmRage");
                    break;
            }

            animator.SetState("Idle", 4f, false, true);
        }

        public void PlayAction(float duration = 0.7f)
        {
            if (animator == null)
            {
                return;
            }

            actionUntil = Time.time + Mathf.Max(0.05f, duration);
            animator.SetState("Attack", 10f, false, true);
        }

        public void PlayHit()
        {
            if (animator == null)
            {
                return;
            }

            actionUntil = Time.time + 0.12f;
            animator.SetState("Hit", 12f, false, true);
        }

        private void ConfigureBoss(string folder, string prefix, string attackA, string attackB, string phaseA, string phaseB)
        {
            animator.SetClip("Idle", Art2DUtility.LoadSequence(folder, prefix + "_Idle_01", prefix + "_Idle_02"));
            animator.SetClip("Move", Art2DUtility.LoadSequence(folder, prefix + "_Move_01", prefix + "_Move_02"));
            animator.SetClip("Attack", Art2DUtility.LoadSequence(folder, prefix + "_" + attackA, prefix + "_" + attackB, prefix + "_" + phaseA, prefix + "_" + phaseB));
            animator.SetClip("Hit", Art2DUtility.LoadSequence(folder, prefix + "_Hit_01"));
        }

        private static string ResolveFolder(string actorId)
        {
            switch (actorId)
            {
                case "Slime":
                    return "AnimationSprites/Enemies/Slime";
                case "EliteSlime":
                    return "AnimationSprites/Enemies/EliteSlime";
                case "SkeletonArcher":
                    return "AnimationSprites/Enemies/SkeletonArcher";
                case "Titan":
                    return "AnimationSprites/Bosses/Titan";
                case "EmberMage":
                    return "AnimationSprites/Bosses/EmberMage";
                case "StormGuard":
                    return "AnimationSprites/Bosses/StormGuard";
                case "BroodQueen":
                    return "AnimationSprites/Bosses/BroodQueen";
                default:
                    return "";
            }
        }
    }
}
