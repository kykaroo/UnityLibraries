using UnityEngine;

namespace NetcodePackage.Samples.Example.Scripts.Data
{
    public class PlayerInputState
    {
        public ulong Tick { get; set; }
        public Vector2 MoveDir { get; set; }
    }
}