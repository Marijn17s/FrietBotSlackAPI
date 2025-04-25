using FrietBot.Config;
using Serilog;

namespace FrietBot.Services;

public interface IOrderStatusService
{
    (bool IsOpen, DateTime? NextOpening, DateTime? Deadline) GetOrderStatus();
    void ResetCycle();
    bool ManuallyReopenOrdering();
    void CloseOrdering();
}

public class OrderStatusService : IOrderStatusService
{
    private readonly TimeZoneInfo _amsterdamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
    private DateTime? _nextCycleStart;
    private DateTime? _manualReopenEnd;

    public bool ManuallyReopenOrdering()
    {
        // Get current time in Amsterdam
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _amsterdamTimeZone);
        
        // Set the manual reopen end time to 30 minutes from now
        _manualReopenEnd = TimeZoneInfo.ConvertTimeToUtc(now.AddMinutes(30), _amsterdamTimeZone);
        
        Log.Information("Ordering manually reopened until {EndTime}", _manualReopenEnd);
        return true;
    }

    public void CloseOrdering()
    {
        _manualReopenEnd = null;
        Log.Information("Ordering closed");
    }

    public (bool IsOpen, DateTime? NextOpening, DateTime? Deadline) GetOrderStatus()
    {
        // Get current time in Amsterdam
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _amsterdamTimeZone);
        var today = now.Date;
        var currentDayOfWeek = today.DayOfWeek;
        var currentTime = now.TimeOfDay;
        var orderTime = new TimeSpan(OrderConfig.OrderHour, OrderConfig.OrderMinute, 0);
        var totalOrderTime = new TimeSpan(
            OrderConfig.OrderHour + OrderConfig.Offsets.CloseOrdersHours,
            OrderConfig.OrderMinute + OrderConfig.Offsets.CloseOrdersMinutes,
            0);

        // Check if we're in a manual reopen window
        if (_manualReopenEnd.HasValue)
        {
            var manualReopenEndAmsterdam = TimeZoneInfo.ConvertTimeFromUtc(_manualReopenEnd.Value, _amsterdamTimeZone);
            
            // If we're past the manual reopen end time, clear it
            if (now >= manualReopenEndAmsterdam)
            {
                _manualReopenEnd = null;
            }
            else
            {
                // If we're in the manual reopen window, ordering is open
                return (true, _nextCycleStart, _manualReopenEnd);
            }
        }

        // If we have a stored next cycle start, use that
        if (_nextCycleStart.HasValue)
        {
            var nextCycleStartAmsterdam = TimeZoneInfo.ConvertTimeFromUtc(_nextCycleStart.Value, _amsterdamTimeZone);
            
            // If we're past the next cycle start, clear it
            if (now >= nextCycleStartAmsterdam)
            {
                _nextCycleStart = null;
            }
            else
            {
                // Calculate deadline for the next cycle
                var cycleDeadlineAmsterdam = nextCycleStartAmsterdam.Date.Add(totalOrderTime);
                var cycleDeadlineUtc = TimeZoneInfo.ConvertTimeToUtc(cycleDeadlineAmsterdam, _amsterdamTimeZone);
                return (false, _nextCycleStart, cycleDeadlineUtc);
            }
        }

        // Calculate deadline for today
        var todayDeadlineAmsterdam = today.Add(totalOrderTime);
        var todayDeadlineUtc = TimeZoneInfo.ConvertTimeToUtc(todayDeadlineAmsterdam, _amsterdamTimeZone);

        // If it's past the deadline, ordering is closed
        if (now >= todayDeadlineAmsterdam)
        {
            return (false, null, todayDeadlineUtc);
        }

        // If it's the order day and past the order time, ordering is open
        if (currentDayOfWeek == OrderConfig.OrderDay && currentTime >= orderTime)
        {
            return (true, null, todayDeadlineUtc);
        }

        // Calculate next opening time in Amsterdam
        var daysUntilNextOrder = ((int)OrderConfig.OrderDay - (int)currentDayOfWeek + 7) % 7;
        DateTime nextOpeningAmsterdam;
        
        if (daysUntilNextOrder == 0 && currentTime < orderTime)
        {
            // Today is the order day but it's not open yet
            nextOpeningAmsterdam = today.Add(orderTime);
        }
        else
        {
            nextOpeningAmsterdam = today.AddDays(daysUntilNextOrder).Add(orderTime);
        }

        // Calculate deadline for the next order day
        var nextDeadlineAmsterdam = nextOpeningAmsterdam.Date.Add(totalOrderTime);
        var nextDeadlineUtc = TimeZoneInfo.ConvertTimeToUtc(nextDeadlineAmsterdam, _amsterdamTimeZone);

        // Convert opening time to UTC for API response
        var nextOpeningUtc = TimeZoneInfo.ConvertTimeToUtc(nextOpeningAmsterdam, _amsterdamTimeZone);
        return (false, nextOpeningUtc, nextDeadlineUtc);
    }

    public void ResetCycle()
    {
        // Get current time in Amsterdam
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _amsterdamTimeZone);
        var today = now.Date;
        var currentDayOfWeek = today.DayOfWeek;

        // Calculate days until next Wednesday
        var daysToNextOrder = ((int)OrderConfig.OrderDay - (int)currentDayOfWeek + 7) % 7;
        if (daysToNextOrder == 0)
        {
            // If today is Wednesday, add 7 days to get to next Wednesday
            daysToNextOrder = 7;
        }

        // Calculate next opening time in Amsterdam
        var nextOrderOpeningAmsterdam = today.AddDays(daysToNextOrder).Add(new TimeSpan(OrderConfig.OrderHour, OrderConfig.OrderMinute, 0));
        _nextCycleStart = TimeZoneInfo.ConvertTimeToUtc(nextOrderOpeningAmsterdam, _amsterdamTimeZone);

        Log.Information($"Cycle reset. Next opening time set to: {_nextCycleStart}");
    }
} 