namespace com.kykaroo.netcode.Samples.ClientSample.Scripts.Data
{
    public class PlayerData
    {
        public ushort PlayerId { get; }
        public float PosX { get; set; }
        public float PosY { get; set; }

        public PlayerData(ushort playerId)
        {
            PlayerId = playerId;
        }
    }
}