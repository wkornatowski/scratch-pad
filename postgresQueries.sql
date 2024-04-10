SELECT pid, now() - pg_stat_activity.query_start AS duration, query, state
FROM pg_stat_activity
WHERE (now() - pg_stat_activity.query_start) > interval '5 minutes'
AND state = 'active'
ORDER BY duration DESC;


-- Enable the extension if it's not already enabled
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- Query to find high CPU usage queries based on total time
SELECT query, calls, total_time, rows, 100.0 * total_time / sum(total_time) OVER () AS perc_total_time
FROM pg_stat_statements
ORDER BY total_time DESC
LIMIT 10;
