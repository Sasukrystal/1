using System.Collections.Generic;
using UnityEngine;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public sealed class RuntimeSpriteAnimator2D : MonoBehaviour
    {
        private readonly Dictionary<string, Sprite[]> clips = new Dictionary<string, Sprite[]>();
        private SpriteRenderer spriteRenderer;
        private string currentState = "";
        private float frameRate = 8f;
        private float stateStartTime;
        private bool once;
        private string fallbackState = "Idle";

        public string CurrentState => currentState;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null || !clips.TryGetValue(currentState, out Sprite[] frames) || frames.Length == 0)
            {
                return;
            }

            float elapsed = Mathf.Max(0f, Time.time - stateStartTime);
            int frame = Mathf.FloorToInt(elapsed * frameRate);
            if (once && frame >= frames.Length)
            {
                once = false;
                SetState(fallbackState, frameRate, false, true);
                return;
            }

            ApplyCurrentFrame();
        }

        public void SetClip(string state, Sprite[] frames)
        {
            if (string.IsNullOrEmpty(state) || frames == null || frames.Length == 0)
            {
                return;
            }

            clips[state] = frames;
            if (string.IsNullOrEmpty(currentState))
            {
                SetState(state);
            }
        }

        public void SetState(string state, float fps = 8f, bool playOnce = false, bool forceRestart = false)
        {
            if (string.IsNullOrEmpty(state) || !clips.ContainsKey(state))
            {
                return;
            }

            if (!forceRestart && currentState == state && once == playOnce)
            {
                frameRate = fps;
                return;
            }

            currentState = state;
            frameRate = Mathf.Max(1f, fps);
            once = playOnce;
            stateStartTime = Time.time;
            ApplyCurrentFrame();
        }

        private void ApplyCurrentFrame()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null || !clips.TryGetValue(currentState, out Sprite[] frames) || frames.Length == 0)
            {
                return;
            }

            float elapsed = Mathf.Max(0f, Time.time - stateStartTime);
            int frame = Mathf.FloorToInt(elapsed * frameRate);
            spriteRenderer.sprite = frames[Mathf.Clamp(frame, 0, frames.Length - 1) % frames.Length];
        }

        public void PlayOnce(string state, string fallback = "Idle", float fps = 12f)
        {
            fallbackState = fallback;
            SetState(state, fps, true, true);
        }

        public void ClearClips()
        {
            clips.Clear();
            currentState = string.Empty;
            once = false;
            stateStartTime = 0f;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = null;
            }
        }
    }
}
