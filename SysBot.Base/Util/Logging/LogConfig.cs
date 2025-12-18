namespace SysBot.Base;

/// <summary>
/// Configuration for the logging system
/// </summary>
public static class LogConfig
{
    /// <summary>
    /// Enable or disable all logging
    /// </summary>
    public static bool LoggingEnabled { get; set; } = true;

    /// <summary>
    /// Number of archived log files to keep (default: 14 days)
    /// </summary>
    public static int MaxArchiveFiles { get; set; } = 14;

    /// <summary>
    /// Enable per-bot log files in addition to the master log
    /// When enabled, each bot will have its own log file: logs/{BotName}/SysBotLog.txt
    /// DEFAULT: true - each IP gets its own folder with all related logs
    /// </summary>
    public static bool EnablePerBotLogging { get; set; } = true;

    /// <summary>
    /// When enabled, the master log file will still contain all bot logs
    /// When disabled, only per-bot logs are written (saves disk space and improves performance)
    /// DEFAULT: true - master log contains everything for backward compatibility
    /// </summary>
    public static bool EnableMasterLog { get; set; } = true;

    /// <summary>
    /// Include timestamp in log file names (useful for debugging specific sessions)
    /// Format: logs/{BotName}/SysBotLog_{yyyy-MM-dd_HH-mm-ss}.txt
    /// </summary>
    public static bool IncludeTimestampInFilename { get; set; } = false;

    /// <summary>
    /// Maximum size for individual log files before archiving (in bytes)
    /// Default: 50MB (52428800 bytes)
    /// </summary>
    public static long MaxLogFileSize { get; set; } = 52428800;

    /// <summary>
    /// System components will log to a single "System" folder instead of individual folders
    /// Reduces clutter when you have many system services running
    /// DEFAULT: true - consolidate all system logs into one folder
    /// </summary>
    public static bool ConsolidateSystemLogs { get; set; } = true;

    /// <summary>
    /// List of identity prefixes that are considered "system" components
    /// These will all log to the "System" folder if ConsolidateSystemLogs is enabled
    /// </summary>
    public static readonly string[] SystemIdentities =
    [
        // Core system components
        "System", "SysBot", "Bot", "Form", "Hub",

        // Recovery and monitoring
        "Recovery", "RecoveryNotification",

        // Web services
        "WebServer", "WebTrade", "WebDump", "WebTradeNotifier", "TCP",

        // Discord integration
        "Discord", "SysCord",

        // Queue and trade management
        "QueueHelper", "TradeQueueInfo", "BatchTracker", "Barrier",

        // Utilities
        "Echo", "Dump", "Tray", "Legalizer",

        // Services
        "BotTaskService", "RestartManager", "PokemonPool"
    ];

    /// <summary>
    /// Enable periodic cleanup of old archived log files.
    /// When disabled, log retention is controlled only by NLog's MaxArchiveFiles setting.
    /// DEFAULT: false - rely on NLog's built-in archiving behavior
    /// </summary>
    public static bool EnableLogFileCleanup { get; set; } = false;

    /// <summary>
    /// Number of days to retain archived log files when EnableLogFileCleanup is true.
    /// Files older than this will be deleted during periodic cleanup.
    /// DEFAULT: 7 days
    /// </summary>
    public static int LogFileRetentionDays { get; set; } = 7;
}
