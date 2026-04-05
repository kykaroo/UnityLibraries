using UnityEngine;

namespace ClientSample
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer = default!;

        public PlayerData PlayerData;

        public SpriteRenderer SpriteRenderer => spriteRenderer;
    }
}