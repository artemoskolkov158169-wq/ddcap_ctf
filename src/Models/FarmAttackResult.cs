namespace DDCAP.AltayCTF
{
    // =========================================================================
    // FARM ATTACK RESULT MODEL
    // =========================================================================
    public class FarmAttackResult
    {
        public int TeamNumber { get; set; }
        public string TeamIp { get; set; }
        public int HttpCode { get; set; }
        public long LatencyMs { get; set; }
        public bool FlagFound { get; set; }
        public string CapturedFlag { get; set; }
        public string ResponsePreview { get; set; }
        public string FullResponse { get; set; }
    }
}
