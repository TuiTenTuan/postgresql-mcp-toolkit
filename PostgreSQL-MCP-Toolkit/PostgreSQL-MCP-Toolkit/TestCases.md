# PostgreSQL MCP Toolkit Test Cases

This file contains the test scenarios for the project without executing any commands.

## 1. Startup and configuration

1. Missing `POSTGRES_HOST` should fail startup with a configuration error.
2. Missing `POSTGRES_USER` should fail startup.
3. Missing `POSTGRES_PASSWORD` should fail startup.
4. Missing `POSTGRES_DATABASE` should fail startup.
5. Missing `API_KEY` should fail startup.
6. Valid environment values should allow the application to start successfully.
7. Missing `POSTGRES_PORT` should default to `5432`.
8. Missing `TIMEOUT` should default to `1024`.
9. Invalid numeric values for `POSTGRES_PORT` or `TIMEOUT` should fail fast.

## 2. Authentication

10. Request without the `x-api-key` header should return `401 Unauthorized`.
11. Request with an incorrect API key should return `401 Unauthorized`.
12. Request with the correct API key should be accepted.
13. `GET /health` should be accessible without any API key.
14. `OPTIONS` preflight requests should bypass the API key validation.
15. Validated MCP requests should reach the tool handler after authentication.

## 3. `list_tables`

16. `list_tables` with the default `public` schema should return all base tables in that schema.
17. `list_tables` should exclude PostgreSQL system schemas such as `pg_catalog` and `information_schema`.
18. `list_tables` for a valid custom schema should return only tables from that schema.
19. `list_tables` for a schema with no tables should return an empty list.
20. `list_tables` should return schema-qualified names in the format `schema.table`.
21. If the database connection fails, the tool should return a formatted error payload instead of crashing.

## 4. `get_table_structure`

22. `get_table_structure` for an existing table should return column metadata.
23. The metadata should include: column name, data type, UDT name, nullability, default value, and primary key flag.
24. A table with a primary key should mark the primary key column as `is_primary_key = true`.
25. A table without a primary key should return `false` for all primary-key flags.
26. `column_default` should return the actual default expression when present.
27. `column_default` should return `null` when no default value exists.
28. The method should return an error object when the target table does not exist.
29. The method should respect the provided schema and not default incorrectly when a custom schema is supplied.
30. Returned columns should be ordered by the PostgreSQL ordinal position.

## 5. `execute_raw_sql`

31. A valid `SELECT` query should return `columns`, `row_count`, and `rows` in the response.
32. An empty SQL string should return an error payload.
33. An invalid SQL statement should return a formatted exception error.
34. An `INSERT` statement should succeed and report affected rows.
35. An `UPDATE` statement should succeed and report affected rows.
36. A `DELETE` statement should succeed and report affected rows.
37. A `CREATE TABLE` statement should succeed when permissions allow it.
38. A query that returns zero rows should still return a valid result with `row_count = 0`.
39. `NULL` values in result rows should be serialized as `null`.
40. Queries involving many rows should return the full result set without truncation.
41. Permission errors in SQL execution should return a structured error instead of causing a crash.

## 6. Security and edge cases

42. A SQL injection attempt should be treated as a documented risk because the tool executes raw SQL as provided.
43. Very large SQL payloads should be handled without crashing the server.
44. Repeated valid requests in quick succession should not corrupt or reuse stale state.
45. Multiple requests with different API keys should remain isolated from one another.
46. When PostgreSQL is unavailable, all tool methods should fail gracefully and return formatted errors.
47. Mixed-case table or schema names should behave consistently with PostgreSQL rules.
48. Trailing whitespace or empty input should not be treated as valid SQL.

## 7. Priority test set

If only a small set is required first, prioritize these high-value tests:

- Missing or invalid API key rejects access.
- Valid API key allows MCP requests.
- `/health` is unauthenticated.
- `list_tables` returns proper schema-qualified table names.
- `get_table_structure` returns accurate metadata for a known table.
- `execute_raw_sql` works for `SELECT` and `INSERT` flows.
- Invalid SQL returns a clean error response.

## 8. Suggested test categories

- Configuration validation
- Authentication and authorization
- PostgreSQL introspection tools
- Raw SQL execution safety and correctness
- Error handling and graceful degradation

This file is intentionally written only as a test specification and was not executed.
