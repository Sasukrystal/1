using UnityEngine;

namespace ModernRogue
{
    [DisallowMultipleComponent]
    public class LevelPortal : MonoBehaviour
    {
        private DungeonLoopDirector director;
        private bool victoryPortal;
        private bool triggered;

        public void Initialize(DungeonLoopDirector owner, bool isVictoryPortal)
        {
            director = owner;
            victoryPortal = isVictoryPortal;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered || other == null || !other.CompareTag("Player"))
            {
                return;
            }

            if (director == null)
            {
                director = DungeonLoopDirector.Instance;
            }

            if (director == null)
            {
                return;
            }

            triggered = true;
            director.GoToNextRoom();
        }

        private void Update()
        {
            float speed = victoryPortal ? 90f : 55f;
            transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.World);
        }
    }
}
