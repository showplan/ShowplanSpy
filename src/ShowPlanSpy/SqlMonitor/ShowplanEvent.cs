using Microsoft.SqlServer.XEvent;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ShowplanSpy.SqlMonitor
{
    public class ShowplanEvent
    {
        public XMLData ShowplanXml { get; set; }
        public string Showplan { get; set; }
        public ulong Duration { get; set; }
        public int EstimatedRows { get; set; }
        public int EstimatedCost { get; set; }
        public string SqlStatement { get; set; }
        public ulong QueryHash { get; set; }
        public ulong QueryPlanHash { get; set; }
        public string QueryPlanHashString => $"0x{QueryPlanHash:X}";
        public byte[] PlanHandle { get; set; }
    }
}