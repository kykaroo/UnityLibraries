using com.kykaroo.netcode.Samples.ClientSample.Scripts.Data;
using UnityEngine;

namespace com.kykaroo.netcode.Samples.ClientSample.Scripts.Objects
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer = default!;

        public PlayerData PlayerData;

        public SpriteRenderer SpriteRenderer => spriteRenderer;
    }
}