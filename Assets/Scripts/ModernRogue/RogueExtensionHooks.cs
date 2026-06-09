using UnityEngine;

namespace ModernRogue
{
    public interface IRoomRewardHandler
    {
        RoomRewardType RewardType { get; }
        void ApplyReward(NewInventorySystem inventory, SoulKnightDirector director);
    }

    public interface ICoreEffectHook
    {
        CoreElement Element { get; }
        void OnCoreEquipped(NewInventorySystem inventory, CoreData core);
        void OnDash(PlayerController2D player);
    }

    public interface IRuntimeArtResolver
    {
        Sprite LoadSprite(string assetName, Sprite fallback);
        RuntimeAnimatorController LoadAnimator(string assetName);
        AudioClip LoadAudio(string assetName);
    }

    public sealed class DefaultRuntimeArtResolver : IRuntimeArtResolver
    {
        public Sprite LoadSprite(string assetName, Sprite fallback)
        {
            Sprite sprite = Resources.Load<Sprite>("Art2D/" + assetName);
            return sprite != null ? sprite : fallback;
        }

        public RuntimeAnimatorController LoadAnimator(string assetName)
        {
            return Resources.Load<RuntimeAnimatorController>("Art2D/" + assetName);
        }

        public AudioClip LoadAudio(string assetName)
        {
            return Resources.Load<AudioClip>("Audio/" + assetName);
        }
    }
}
