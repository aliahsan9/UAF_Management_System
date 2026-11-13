/*
    Query Store baseline configuration.
    Execute only after DBA approval and after checking SQL Server version/storage policy.
    Example: sqlcmd -S sql01 -E -v DatabaseName="UafManagementDb" -i scripts/database/Enable-UafQueryStore.sql
*/
:setvar DatabaseName "UafManagementDb"

ALTER DATABASE [$(DatabaseName)] SET QUERY_STORE = ON;

ALTER DATABASE [$(DatabaseName)]
SET QUERY_STORE
(
    OPERATION_MODE = READ_WRITE,
    QUERY_CAPTURE_MODE = AUTO,
    DATA_FLUSH_INTERVAL_SECONDS = 900,
    INTERVAL_LENGTH_MINUTES = 15,
    MAX_STORAGE_SIZE_MB = 1024,
    CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30),
    WAIT_STATS_CAPTURE_MODE = ON
);
