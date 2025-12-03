using NetcodePackage.Samples.Example.Scripts.Data;
using UnityEngine;

namespace NetcodePackage.Samples.Example.Scripts.Objects
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer = default!;

        public PlayerData PlayerData;

        public SpriteRenderer SpriteRenderer => spriteRenderer;
    }
}