namespace TheShed.Server.Services
{
    /// <summary>Periodically hard-deletes trash items past their retention window (<see cref="TrashSettings.RetentionDays"/>).</summary>
    public class TrashPurgeService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TrashPurgeService> _logger;

        public TrashPurgeService(IServiceScopeFactory scopeFactory, ILogger<TrashPurgeService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var trash = scope.ServiceProvider.GetRequiredService<ITrashService>();
                    var purged = await trash.PurgeExpiredAsync(DateTime.UtcNow, stoppingToken);
                    if (purged > 0)
                    {
                        _logger.LogInformation("Trash purge removed {Count} expired item(s).", purged);
                    }
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Shutdown requested during the delay.
                }
            }
        }
    }
}
