using System;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // RECENT REQUEST ITEM FOR CORRELATION
    // =========================================================================
    public class RecentRequestItem
    {
        public string Method { get; set; }
        public string Uri { get; set; }
        public DateTime Timestamp { get; set; }
        public string Body { get; set; }
        public bool IsUploadOrStager { get; set; }
        public string UploadedFilename { get; set; }
    }
}
