using UnityEngine;

namespace com.kykaroo.netcode.Samples.ClientSample.Scripts.Data
{
    public class PlayerInputState
    {
        public ulong Tick { get; set; }
        public Vector2 MoveDir { get; set; }
    }
}