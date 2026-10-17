# Stored routines in Postgres: FUNCTION vs PROCEDURE, and where EF fits

## TL;DR

- **We can't use a Postgres `PROCEDURE` here** because a Postgres procedure cannot return rows
  to the caller — only a `FUNCTION` can. This is a hard capability gap, not a style choice.
- **We use `FUNCTION`** because it returns rows the same way a normal query does, and Dapper
  can call it with zero new plumbing.
- **Judged on security and performance — the priorities for this app — stored functions beat
  EF Core**: least-privilege `EXECUTE`-only DB grants, atomic single-round-trip concurrency-safe
  writes, no ORM translation/change-tracker overhead, and no raw-SQL injection escape hatch.
  Details below.

## Why not `PROCEDURE`

In MySQL and SQL Server, a "stored procedure" can run a `SELECT` and the results just flow
back to whoever called it — `CALL Usp_Foo(...)` in MySQL, `EXEC Usp_Foo` in SQL Server, and the
client reads the rows like any query result.

Postgres doesn't work that way. It has two different kinds of stored routine, and only one of
them can return rows:

| | Can it return rows to the caller? | How it's called |
|---|---|---|
| MySQL `PROCEDURE` | Yes | `CALL Usp_Foo(...)` |
| SQL Server `PROCEDURE` | Yes | `EXEC Usp_Foo` |
| Postgres `PROCEDURE` | **No** — only `OUT` parameters or a `refcursor` handle | `CALL sp_foo(...)` |
| Postgres `FUNCTION` | Yes | `SELECT * FROM sp_foo(...)` |

Postgres added `PROCEDURE` in version 11 (2018) for one specific reason: letting a routine run
`COMMIT`/`ROLLBACK` itself, mid-routine, across multiple steps. It was never meant to be
"the kind of routine that returns query results" — that job already belonged to `FUNCTION`,
which has existed since the beginning. SevaOne's repositories never need to commit/rollback
*inside* a routine (`RepositoryBase`'s `IUnitOfWork` already owns that from the C# side), so
the one thing `PROCEDURE` is for doesn't help us, and the one thing we need — rows back —
is exactly what it can't do.

**What it would take to force a `PROCEDURE` to return rows anyway:** open a `refcursor`
inside it, hand back the cursor's name, then run a *second* statement (`FETCH ALL IN
"<cursor>"`) inside the same transaction to actually drain it. That's two round trips per
read, an explicit transaction the caller has to manage by hand, and no clean Dapper support —
strictly worse than just using a function, on every axis (simplicity, round trips, speed).

## Why `FUNCTION` works cleanly

```sql
CREATE OR REPLACE FUNCTION portal.sp_get_citizen_by_id(p_id uuid)
RETURNS SETOF portal.sevaone_user
LANGUAGE sql STABLE AS $$
    SELECT * FROM portal.sevaone_user WHERE id = p_id;
$$;
```

```csharp
// same call shape as the inline SQL it replaces
var cmd = new CommandDefinition(
    "SELECT * FROM portal.sp_get_citizen_by_id(@id)",
    new { id }, cancellationToken: ct);
return await conn.QueryFirstOrDefaultAsync<SevaOneUser>(cmd);
```

One round trip. No transaction ceremony for a plain read. `conn.QueryAsync<T>(sql, params)` —
literally what Dapper already does everywhere in this codebase, just with the SQL text
replaced by a one-line function call. `LANGUAGE sql STABLE` also lets Postgres inline a
single-statement function straight into the calling query plan, so there's no extra runtime
cost versus the inline SQL it replaced.

For writes, the reference `Usp_...` style returns a `Success/Message` row — a Postgres
function does that as `RETURNS TABLE(success boolean, message text)`, still called with a
plain `SELECT`, no `OUT` parameters, no `CommandType.StoredProcedure` special-casing.

## EF vs. stored functions, judged on security and performance only

Developer convenience (less boilerplate, LINQ, automatic migrations) is a real EF advantage,
but it isn't the priority for this app. Security and performance are. Judged strictly on those
two axes:

### Performance

- **No ORM translation layer.** EF turns LINQ into SQL through a query translator, which can
  produce unexpected joins, over-fetching, or cartesian-explosion results with nested
  `Include()`s. A stored function *is* the exact SQL that runs — nothing to mistranslate,
  nothing to review except the SQL itself.
- **No change-tracker overhead.** EF snapshots entities to detect changes on `SaveChanges()`.
  Dapper + functions never materialize a tracked object graph — it maps rows directly to plain
  DTOs and moves on.
- **Concurrency-safe writes cost one round trip, not a retry loop.** EF's optimistic
  concurrency (`xmin`) detects a conflicting write by throwing `DbUpdateConcurrencyException`
  *after* the conflict happens, and expects the caller to catch it and retry. A stored function
  does the atomic `UPDATE ... WHERE <still-valid-condition>` and returns a row-count-based
  success flag in the same single statement — no exception path, no retry, no extra round trip
  under contention. For wallet-deduction and token-redemption specifically, that's a real
  latency difference under concurrent load, not a theoretical one.
- **Reads that are `STABLE` inline into the caller's query plan** — zero function-call overhead
  on top of the SQL itself (see above). EF has no equivalent of this.

### Security

- **Least-privilege database access.** The app's DB role can be granted `EXECUTE` on named
  functions only, with *no* direct `SELECT`/`INSERT`/`UPDATE`/`DELETE` grants on the underlying
  tables. If the app's DB credentials ever leak, the attacker can only invoke the specific,
  whitelisted operations those functions expose — not run arbitrary queries against
  `portal.sevaone_user` or `portal.operator_master`. An EF-based app normally connects with a
  role that has full CRUD rights on every mapped table, because EF needs that to work at all.
- **Every write is parameterized and centrally logged.** Each write function funnels through
  one place, with `EXCEPTION WHEN OTHERS` logging to `audit.db_errors` before re-raising — one
  spot to monitor/alert on for anomalous DB errors across the whole app, instead of scattered
  `try/catch` around LINQ calls in individual services.
- **No dynamic-SQL injection surface.** Function calls are parameterized the same way Dapper's
  existing inline SQL already is (`SELECT * FROM fn(@p1, @p2)`). This sidesteps a real-world EF
  footgun: `FromSqlRaw`/`FromSqlInterpolated` calls that get built with string concatenation
  instead of parameters — an easy mistake in EF codebases that doesn't exist here because
  there's no raw-SQL escape hatch being reached for in the first place.
- **Atomicity closes the check-then-act window.** Wallet balance checks and token-revocation
  checks happen in the *same statement* as the update, at the database engine level. There is
  no gap between "app checked the balance" and "app writes the deduction" for a second request
  to land in — the kind of window that toctou-style races exploit. EF's default pattern (load
  entity, mutate in memory, `SaveChanges()`) has that gap unless every such operation is
  specifically hand-written around it, at which point it's not really "using EF" for that path
  anymore.

## Summary

| | Returns rows? | Perf | Security |
|---|---|---|---|
| Postgres `PROCEDURE` | No (only `OUT`/`refcursor`) | N/A — can't replace the SELECT-returning SQL we need | N/A |
| Postgres `FUNCTION` | Yes, like a normal query | No ORM overhead, `STABLE` reads inline, atomic writes in one round trip | `EXECUTE`-only grants, parameterized by construction, atomic checks close race windows |
| EF Core | Yes, via LINQ | Change-tracker overhead, retry-on-conflict for concurrent writes | App role typically needs full table CRUD grants; raw-SQL escape hatches are a real injection risk if misused |

Naming keeps the `sp_` prefix from the reference SQL Server style — so it still *reads* as
"the stored-procedure layer" — even though the underlying Postgres object is a `FUNCTION`.
