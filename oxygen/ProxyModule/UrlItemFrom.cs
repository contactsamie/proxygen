using System;

namespace Proxygen
{
    public class UrlItemFrom : UrlItem
    {

        /// <summary>
        /// no matter what the situation is , the response will be OK and will contain system diagnostic message
        /// </summary>
        public bool OverrideReturnWithSystemMessages { set; get; }

        public Guid? RequestID { set; get; }

        public string LogDestination { set; get; }

        /// <summary>
        /// as part of the matching process, this must be satisfied
        /// </summary>
        public string MustContain { set; get; }

        /// <summary>
        /// this will be removed from the request before sending to destination
        /// </summary>
        public string MustRemove { set; get; }
    }
}