namespace FrietBot.Config;

public static class OrderConfig
{
    // Base time configuration
    public const DayOfWeek OrderDay = DayOfWeek.Thursday;
    public const int OrderHour = 013;
    public const int OrderMinute = 57;

    public const string OrderingLink = "https://v0-friet-order-website.vercel.app/";
    
    // Job timing offsets from base time
    public static class Offsets
    {
        // Close orders job (base + 1 hour)
        public const int CloseOrdersHours = 0; // 1
        public const int CloseOrdersMinutes = 1; // 0
        
        // Reset cycle job (base + 3 hours)
        public const int ResetCycleHours = 0; // 3
        public const int ResetCycleMinutes = 2; // 0
        
        // Clear orders job (base + 4 hours)
        public const int ClearOrdersHours = 0; // 4
        public const int ClearOrdersMinutes = 3; // 0
    }
}