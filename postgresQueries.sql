SELECT pid, now() - pg_stat_activity.query_start AS duration, query, state
FROM pg_stat_activity
WHERE (now() - pg_stat_activity.query_start) > interval '5 minutes'
AND state = 'active'
ORDER BY duration DESC;


SELECT pid, datname, usename, application_name, state, query, 
       now() - state_change AS idle_duration
FROM pg_stat_activity
WHERE state = 'idle'
ORDER BY idle_duration DESC;


SELECT 
    a.pid, 
    now() - a.query_start AS duration, 
    a.query, 
    a.state,
    s.total_time, 
    s.calls, 
    s.rows 
FROM 
    pg_stat_activity a
JOIN 
    pg_stat_statements s ON a.query = s.query 
WHERE 
    (now() - a.query_start) > interval '5 minutes' 
    AND a.state = 'active'
ORDER BY 
    duration DESC;
