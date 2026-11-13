/*
    Read-only SQL Server performance review queries for UAF Management System.
    Run in the target database using a DBA/read-only diagnostic identity.
    These queries inspect Query Store and index DMVs; they do not create indexes.
*/

SET NOCOUNT ON;

-- 1. Query Store status and capture mode.
SELECT
    actual_state_desc,
    desired_state_desc,
    readonly_reason,
    current_storage_size_mb,
    max_storage_size_mb,
    query_capture_mode_desc,
    wait_stats_capture_mode_desc,
    stale_query_threshold_days
FROM sys.database_query_store_options;

-- 2. Highest average duration queries captured by Query Store (last 7 days).
SELECT TOP (25)
    q.query_id,
    p.plan_id,
    qt.query_sql_text,
    rs.count_executions,
    CAST(rs.avg_duration / 1000.0 AS decimal(18, 2)) AS avg_duration_ms,
    CAST(rs.max_duration / 1000.0 AS decimal(18, 2)) AS max_duration_ms,
    CAST(rs.avg_cpu_time / 1000.0 AS decimal(18, 2)) AS avg_cpu_ms,
    rs.last_execution_time
FROM sys.query_store_query AS q
INNER JOIN sys.query_store_query_text AS qt ON qt.query_text_id = q.query_text_id
INNER JOIN sys.query_store_plan AS p ON p.query_id = q.query_id
INNER JOIN sys.query_store_runtime_stats AS rs ON rs.plan_id = p.plan_id
INNER JOIN sys.query_store_runtime_stats_interval AS rsi
    ON rsi.runtime_stats_interval_id = rs.runtime_stats_interval_id
WHERE rsi.start_time >= DATEADD(DAY, -7, SYSUTCDATETIME())
ORDER BY rs.avg_duration DESC;

-- 3. Highest total logical reads by query in Query Store (last 7 days).
SELECT TOP (25)
    q.query_id,
    qt.query_sql_text,
    SUM(rs.count_executions) AS execution_count,
    SUM(rs.avg_logical_io_reads * rs.count_executions) AS estimated_total_logical_reads,
    MAX(rs.last_execution_time) AS last_execution_time
FROM sys.query_store_query AS q
INNER JOIN sys.query_store_query_text AS qt ON qt.query_text_id = q.query_text_id
INNER JOIN sys.query_store_plan AS p ON p.query_id = q.query_id
INNER JOIN sys.query_store_runtime_stats AS rs ON rs.plan_id = p.plan_id
INNER JOIN sys.query_store_runtime_stats_interval AS rsi
    ON rsi.runtime_stats_interval_id = rs.runtime_stats_interval_id
WHERE rsi.start_time >= DATEADD(DAY, -7, SYSUTCDATETIME())
GROUP BY q.query_id, qt.query_sql_text
ORDER BY estimated_total_logical_reads DESC;

-- 4. Index usage: compare reads to writes before proposing index changes.
SELECT
    OBJECT_SCHEMA_NAME(i.object_id) AS schema_name,
    OBJECT_NAME(i.object_id) AS table_name,
    i.name AS index_name,
    i.type_desc,
    COALESCE(us.user_seeks, 0) AS user_seeks,
    COALESCE(us.user_scans, 0) AS user_scans,
    COALESCE(us.user_lookups, 0) AS user_lookups,
    COALESCE(us.user_updates, 0) AS user_updates
FROM sys.indexes AS i
LEFT JOIN sys.dm_db_index_usage_stats AS us
    ON us.object_id = i.object_id
    AND us.index_id = i.index_id
    AND us.database_id = DB_ID()
WHERE i.object_id > 100
  AND i.is_hypothetical = 0
ORDER BY COALESCE(us.user_updates, 0) DESC;

-- 5. Missing-index DMV suggestions (hypotheses only; validate execution plans first).
SELECT TOP (25)
    OBJECT_SCHEMA_NAME(mid.object_id, mid.database_id) AS schema_name,
    OBJECT_NAME(mid.object_id, mid.database_id) AS table_name,
    migs.user_seeks,
    migs.user_scans,
    CAST(migs.avg_total_user_cost AS decimal(18, 2)) AS avg_user_cost,
    CAST(migs.avg_user_impact AS decimal(18, 2)) AS avg_user_impact_percent,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns
FROM sys.dm_db_missing_index_group_stats AS migs
INNER JOIN sys.dm_db_missing_index_groups AS mig
    ON migs.group_handle = mig.index_group_handle
INNER JOIN sys.dm_db_missing_index_details AS mid
    ON mig.index_handle = mid.index_handle
WHERE mid.database_id = DB_ID()
ORDER BY migs.user_seeks DESC, migs.avg_user_impact DESC;

/*
    Important:
    - DMV counters reset after SQL Server restart and are not historical evidence.
    - Missing-index suggestions can overlap, over-index, or recommend very wide indexes.
    - Review actual execution plans, Query Store waits, write workload and storage cost.
    - Never apply an index recommendation directly from this report without review.
*/
