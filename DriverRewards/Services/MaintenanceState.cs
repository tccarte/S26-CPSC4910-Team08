namespace DriverRewards.Services
{
    public static class MaintenanceState
    {
        public static bool IsEnabled { get; set; }
        public static DateTime? EndUtc { get; set; }

        public static bool IsActive()
        {
            if (!IsEnabled || EndUtc == null)
                return false;

            if (DateTime.UtcNow >= EndUtc.Value)
            {
                IsEnabled = false;
                EndUtc = null;
                return false;
            }

            return true;
        }

        public static void Start(DateTime endUtc)
        {
            IsEnabled = true;
            EndUtc = endUtc;
        }

        public static void Stop()
        {
            IsEnabled = false;
            EndUtc = null;
        }
    }
}