namespace FrietBot.Config;

public static class OrderConfig
{
    // Base time configuration
    public const DayOfWeek OrderDay = DayOfWeek.Friday;
    public const int OrderHour = 11;
    public const int OrderMinute = 00;

    public const string OrderingLink = "http://localhost:3000"; // TODO live domein
    
    // Job timing offsets from base time
    public static class Offsets
    {
        // Close orders job (base + 1 hour)
        public const int CloseOrdersHours = 1; // 1
        public const int CloseOrdersMinutes = 0; // 0
        
        // Reset cycle job (base + 3 hours)
        public const int ResetCycleHours = 3; // 3
        public const int ResetCycleMinutes = 0; // 0
        
        // Clear orders job (base + 4 hours)
        public const int ClearOrdersHours = 4; // 4
        public const int ClearOrdersMinutes = 0; // 0
    }
}