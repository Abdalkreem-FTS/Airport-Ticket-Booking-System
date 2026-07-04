# The Transaction Engine — Design & Architecture

> This document explains **one thing only**: the little "database" this project builds on top of
> plain JSON files, which lives under
> [`ATBS.Console/Transactions/`](ATBS.Console/Transactions). It explains what it does, how each
> piece works, why it is built that way, and — just as importantly — what it **cannot** do (the
> trade-offs, limits, and boundaries).

---

## 1. Why this exists

The one hard rule of the project is:

> **Use the file system as the data storage layer. "My mentor's (Ahmed Abbas) dreams"**

The naive way to obey that rule is: read a JSON file into memory, change the list, write the whole
file back. That works until two things happen at the same time, or the program crashes halfway
through a write. Then you get **lost updates** (two people book the last seat), **torn files** (a
half-written JSON that won't even parse), **double bookings**, and **Inconsistent** files.

So instead of trusting raw file writes, this project puts a small **transaction engine** between the
app and the files. A *transaction* is a group of reads and writes that either **all happen or none
happen** — and while it runs, other transactions can't corrupt what it sees. This is the same idea a
real SQL database gives you. I just build a tiny version of it by hand, on top of `*.json` files
plus a folder of small log files.

The engine aims for the four classic **ACID** guarantees:

- **A**tomic — all-or-nothing.
- **C**onsistent — never leaves the data half-updated.
- **I**solated — concurrent transactions don't step on each other.
- **D**urable — once it says "committed", a crash can't lose it.

---

## 2. The one big idea: *mechanism* vs *policy*

The whole design rests on splitting two concerns that  usually tangle together:

- **Mechanism** — *how do I make a change safely?* Write to a temporary file, note it in a log,
  rename it into place, and know how to recover if the power dies mid-way. This is **the same for
  every situation**, so it lives in one class: [`FileTransaction`](ATBS.Console/Transactions/FileTransaction.cs).

- **Policy** — *how do concurrent transactions treat each other?* Can I see your unfinished data?
  Do I take a lock? What happens if we both edit the same file? This **changes depending on the
  chosen isolation level**, so it is pulled out into swappable **strategies** behind
  [`IConcurrencyControlStrategy`](ATBS.Console/Transactions/Abstractions/IConcurrencyControlStrategy.cs).

Because the mechanism never changes, there is exactly **one** transaction class. Because policy is a
strategy, adding a new isolation level means writing a new small strategy class — not touching the
transaction. This is the classic **Strategy pattern**.

---

## 3. The pieces at a glance

| Piece                                 | File                                      | Job                                                                                                      |
| ------------------------------------- | ----------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| **FileTransaction**                   | `FileTransaction.cs`                      | One unit of work. Stages writes, talks to the log, commits or rolls back.                                |
| **FileTransactionFactory**            | `FileTransactionFactory.cs`               | Starts transactions; runs your code with auto commit/rollback/**retry**.                                 |
| **FileTransactionScope**              | (inside the **FileTransactionFactory**)   | The "handle" you hold; disposing it rolls back if you didn't commit.                                     |
| **FileTransactionContext**            | `FileTransactionContext.cs`               | Remembers "the current transaction" so lower layers find it without being passed it.                     |
| **TransactionalJsonFileStorage**      | `Storage/TransactionalJsonFileStorage.cs` | Routes reads/writes through the current transaction, or straight to disk if there is none.               |
| **TransactionFileCatalog**            | `TransactionFileCatalog.cs`               | Turns a logical table name (`Flights`) into a real file path.                                            |
| **IConcurrencyControlStrategy**       | `Abstractions/…`                          | The policy interface (read visibility, locking, commit check).                                           |
| **LockBasedStrategy**                 | `ConcurrencyControl/LockBasedStrategy.cs` | Pessimistic policy: take locks, hold them to commit.                                                     |
| **SnapshotStrategy**                  | `ConcurrencyControl/SnapshotStrategy.cs`  | Optimistic policy: read a snapshot, check for conflicts at commit.                                       |
| **ConcurrencyControlStrategyFactory** | `ConcurrencyControl/…`                    | Maps each isolation level to the right strategy.                                                         |
| **LockManager**                       | `Management/LockManager.cs`               | The shared lock table, with deadlock detection.                                                          |
| **VersionStore**                      | `Management/VersionStore.cs`              | In-memory version history used by snapshot reads.                                                        |
| **StagedStore**                       | `Management/StagedStore.cs`               | Registry of "unfinished writes", so dirty reads can find them.                                           |
| **TransactionLog**                    | `TransactionLog.cs`                       | The write-ahead log (WAL): Source of truth.                                                              |
| **TransactionRecovery**               | `TransactionRecovery.cs`                  | Runs at startup; finishes or undoes anything a crash interrupted.                                        |
| **TransactionLogDirectory**           | `TransactionLogDirectory.cs`              | Knows where the log folder lives and lists/creates the per-transaction log files.                        |
| **TransientFileIo**                   | `TransientFileIo.cs`                      | The low-level "write to temporary, flush to physical disk, atomically rename" helper every layer reuses. |

> **In one line —** One transaction object per operation, three shared coordinators (locks, versions,
> staged writes), a crash-proof log, and a recovery pass at startup — that's the whole cast.

---

## 4. The mechanism: how one write is made safe

This is the heart of atomicity and durability, and it never varies by isolation level. Everything
here is in [`FileTransaction`](ATBS.Console/Transactions/FileTransaction.cs).

### 4.1 Writing (staging)

When a transaction writes `flights.json`, it does **not** touch the real file. Instead:

1. It writes the new content to a **temporary file** next to it, e.g.
   `flights.json.a1b2c3….temporary`.
2. It **flushes that temporary file all the way to the physical disk** — first `FlushAsync` (empties
   .NET's buffer into the operating system) and then `Flush(flushToDisk: true)` (tells the OS to
   push it onto the actual disk platter/SSD, not just its own cache). After this, the staged data
   truly survives a power cut.
3. It records the write in two places:
   - the **read cache** (so if this same transaction reads the file again, it sees its own change.
   - the **write-ahead log** as an *entry* saying "temporary file X should become final file Y".
4. It registers the temporary file in the **StagedStore** (so other transactions doing dirty reads can
   find it).

The crucial point: **the real file is untouched until commit.** If we crash now, the original file
is exactly as it was.

### 4.2 The write-ahead log (WAL)

Each transaction owns one small log file: `Data/TransactionLogs/{transaction-id}.log`. It is just
JSON: a **status** plus the list of "temporary → final" entries. See
[`TransactionLog.cs`](ATBS.Console/Transactions/TransactionLog.cs).

The status walks a tiny state machine:

```
Pending ──► Committing ──► Committed          (the happy path)
   │
   └──────► RollingBack                       (we gave up on purpose)
```

The log is written the same careful way as data files (to a temporary file, flushed to physical disk,
then renamed over the old log), so the log itself can't be left half-written.

Think of the WAL as the **Source of truth** where you **write down what you are about to do before you do it**.
After a crash, recovery reads the log and knows exactly where you were.

### 4.3 Committing — the exact order matters

Commit is a careful sequence ([`FileTransaction.Commit`](ATBS.Console/Transactions/FileTransaction.cs)):

1. **Capture the change set** — for each written file, remember the *before* content (pre-image) and
   the *after* content (post-image).
2. **Ask the strategy to validate** (`ValidateAndReserveCommit`). For snapshot isolation this is the
   conflict check ("did someone else change my files since I started?"). For lock-based isolation
   the locks already made it safe, so it just records the new versions.
3. **`MarkCommitting`** — flip the log to `Committing` and flush it to disk. **This is the point of
   no return.** Before this line, a crash undoes everything. After this line, a crash *finishes*
   everything.
4. **Move each temporary file onto the real file**, atomically (`File.Replace`, or `File.Move` if the
   file is brand new). Unregister each from the StagedStore.
5. **`MarkCommitted`**, release locks / end the snapshot, delete temp files and the log.

Why put `Committing` *before* any real file is overwritten? Because it removes ambiguity. After a
crash the log is always in one clean state, and recovery has exactly one correct action for each
state (see 7). There's never a "we're not sure if this committed" case.

> **Important — multi-file commits.** A commit can move several files (e.g. `flights.json`
> *and* `bookings.json`). The operating system can only rename **one** file atomically, so moving
> two files is *not* a single atomic step. If we crash after moving the first but before the second,
> the log is already `Committing`, so recovery simply **re-runs the remaining moves forward**. That
> is how the engine gets all-or-nothing across multiple files without a magic OS feature: the WAL
> guarantees the commit is *completable*, and recovery completes it.

### 4.4 Rolling back

Rolling back is easy *because* we never touched the originals: just throw the temporary files away and
release locks. There is no "restore from backup" step in the normal path (see the note about
`BackupPath` in 9).

> **In one line —** A write never touches the real file: it's staged to a flushed temporary file and
> logged first; commit flips the log to `Committing` (the point of no return) then renames temporary → real.
> Crash *before* that point = undo everything; crash *after* = finish everything.

---

## 5. The policy: isolation levels and strategies

Isolation is about **what a transaction is allowed to see and how it guards its data** while other
transactions run at the same time. This project offers the five standard levels, and maps each to a
strategy in
[`ConcurrencyControlStrategyFactory`](ATBS.Console/Transactions/ConcurrencyControl/ConcurrencyControlStrategyFactory.cs).

| Isolation level      | Strategy used                               | What a read sees                                                    | Read locks?            | Commit-time check    |
| -------------------- | ------------------------------------------- | ------------------------------------------------------------------- | ---------------------- | -------------------- |
| **Read Uncommitted** | `LockBasedStrategy(Staged, false)`          | may see other transactions' **unfinished** (dirty) writes           | none                   | none                 |
| **Read Committed**   | `LockBasedStrategy(LatestCommitted, false)` | the latest committed value, re-read every time                      | none                   | none                 |
| **Repeatable Read**  | `LockBasedStrategy(Pinned, true)`           | the value it read first, **pinned** for the rest of the transaction | shared, held to commit | none                 |
| **Serializable**     | `LockBasedStrategy(Pinned, true)`           | same as Repeatable Read                                             | shared, held to commit | none                 |
| **Snapshot**         | `SnapshotStrategy`                          | a consistent **as-of-start** picture, from memory                   | none                   | first-committer-wins |

Two things every reader should understand:

1. **Writes are ALWAYS protected, at every level.** Writing always takes an **exclusive** lock that
   is held until commit — even at Read Uncommitted. So **lost updates never happen**, no matter the
   level. Just like real SQL: the isolation level changes how *reads* behave, not how safe *writes*
   are.

2. **Locks are per-file ("table-level"), not per-row.** Each JSON file is treated as one whole
   table. This is simple, but it means Repeatable Read and Serializable behave **identically** here:
   a shared lock on the whole file, held to commit, already blocks anyone from inserting new rows
   into that file, so "phantom rows" are prevented for free. (Row-level locking would separate them,
   but that is a lot more harder for now.)

The business services all use **Serializable** — the safest level — and lean on automatic retry
(see 8) to smooth over the resulting contention.

> **In one line —** Writes are **always** locked exclusively, so lost updates never happen at any
> level; only *reads* change with the isolation level. The five levels map onto two strategies:
> lock-based (pessimistic) and snapshot (optimistic).

### 5.1 The pessimistic strategy (`LockBasedStrategy`)

"Pessimistic" = assume conflict, so **take a lock first**. See
[`LockBasedStrategy.cs`](ATBS.Console/Transactions/ConcurrencyControl/LockBasedStrategy.cs).

- **On write** it always grabs an **Exclusive** lock on the file and holds it to commit.
- **On read**, behavior depends on the configured `ReadVisibility`:
  - **Staged** (Read Uncommitted): return another transaction's unfinished write if one exists (a
    *dirty read*, via the StagedStore), otherwise the committed file. No lock.
  - **LatestCommitted** (Read Committed): just read the current file from disk each time. No lock. A
    second read may see a different value if someone committed in between.
  - **Pinned** (Repeatable/Serializable): take a **Shared** lock, read once, and cache the value so
    every later read in this transaction returns the same thing. The shared lock (held to commit)
    stops anyone from changing it underneath you.

> **In one line —** Take the lock *first*: exclusive on every write, and on reads either nothing
> (Read Uncommitted / Read Committed) or a held shared lock (Repeatable Read / Serializable).

### 5.2 The optimistic strategy (`SnapshotStrategy`)

"Optimistic" = assume no conflict, **don't take read locks**, and only check at the very end. See
[`SnapshotStrategy.cs`](ATBS.Console/Transactions/ConcurrencyControl/SnapshotStrategy.cs) and the
`VersionStore` in §6.2.

- When the transaction **begins**, it remembers the current global "version number".
- Its reads return the file content **as it was at that moment** — a stable, consistent snapshot,
  served from memory so it stays correct even while another commit is mid-way through swapping files.
- At **commit**, it asks: "did anyone commit a new version of a file I'm writing, after my snapshot
  started?" If yes → **write-write conflict**, the transaction is aborted with a
  `TransactionConflictException` (**first committer wins**), and the caller retries. If no → it
  appends its new versions and commits.

Optimistic wins when conflicts are rare (no locking overhead); it loses when conflicts are common
(work gets thrown away and retried).

> **In one line —** Take **no** read locks: read a frozen snapshot from memory, and only at commit
> check "did anyone change a file I'm writing since I started?" — first committer wins, the loser retries.

---

## 6. The shared coordinators

These three are **singletons** — one shared instance for the whole program — because coordination
only works if every transaction consults the *same* tables. (The transaction object itself is the
only per-operation piece.)

> **In one line —** Three singletons every transaction shares: **LockManager** (locks +
> deadlock detection), **VersionStore** (snapshot history), **StagedStore** (find unfinished writes).

### 6.1 LockManager — locks + deadlock detection

[`LockManager.cs`](ATBS.Console/Transactions/Management/LockManager.cs) is a single-process lock
table. Key facts, in plain terms:

- **Two lock modes only: Shared and Exclusive.** Many transactions can hold Shared on the same file
  at once (many readers). Exclusive is compatible with nothing (one writer, alone). That's the whole
  compatibility rule.
- **Lock upgrade in place.** If a transaction already holds Shared and now needs Exclusive, and no
  one else holds the file, it is upgraded on the spot without releasing first.
- **Waiting doesn't burn a thread.** If a lock isn't available, the transaction parks on a
  `TaskCompletionSource` and `await`s it. A blocked transaction uses **zero** thread-pool threads —
  it just sleeps until woken. This is the "hybrid" design: a short ordinary lock guards the internal
  bookkeeping, but the actual *waiting* happens asynchronously outside that lock.
- **Strict two-phase locking (2PL).** Locks are released **only when the transaction ends**, never
  in the middle. That is exactly what makes Repeatable Read / Serializable hold their guarantees.
- **Deadlock detection with a wait-for graph.** A deadlock is a cycle: A waits for B, B waits for A,
  forever. Before parking, the manager records "who am I waiting for" and runs a depth-first search
  for a cycle. If it finds one, it picks a **victim** — the **youngest** transaction (the one that
  started most recently, tracked by a "birth" counter). Killing the youngest lets older transactions
  finish, so no transaction can starve forever. The victim is aborted with a
  `DeadlockVictimException`.
  - This is also how the **classic upgrade deadlock** is handled: if two transactions both hold
    Shared and both want Exclusive, neither can proceed — the detector sees the cycle and aborts one,
    which then retries. (There is no separate "Update" lock mode; detection covers it.)
- **Timeout as a backstop.** If a lock can't be obtained within `timeoutMs` (default **15 s**), a
  `LockTimeoutException` is thrown.

Both `DeadlockVictimException` and `LockTimeoutException` inherit from
`TransactionConflictException`, so the retry loop treats every "transient collision" the same way.

> **In one line —** Shared/Exclusive locks held until the transaction ends (strict 2PL); waiting is
> async so no threads are wasted; cycles are detected and the **youngest** transaction is killed so
> nobody starves; a 15 s timeout is the last-resort backstop.

### 6.2 VersionStore — the memory for snapshots

[`VersionStore.cs`](ATBS.Console/Transactions/Management/VersionStore.cs) backs **Snapshot**
isolation. It is a **multi-version** store (MVCC): instead of one current value per file, it keeps a
short **history**.

- A global counter (`_globalSequence`) ticks up by one on **every commit**.
- Each file has a **version chain**: a list of `(sequence, content)` pairs. Sequence `0` is the
  "base" (what the file held before this program first committed it).
- A snapshot reads **"the newest version whose sequence ≤ the sequence I captured when I began"** —
  so it always sees a frozen, consistent picture, from memory, never a file that a concurrent commit
  is mid-way through replacing.
- **First touch reads the base from disk**, once, and then caches it in memory. (This disk read is
  done *outside* the internal lock, so a slow read never freezes the whole store.)
- **Commit check** (`TryCommitSnapshot`): if any file being written already has a version newer than
  the snapshot saw, that's a conflict → abort. Otherwise append the new versions.
- **Garbage collection:** versions that no still-running snapshot could ever need are pruned, so the
  chains stay short and memory doesn't grow forever.

Why keep this in memory? Because the **durable truth is still the JSON files.** The version store is
just a coordination cache that gives snapshot readers a consistent view without locks. It is rebuilt
naturally each time the program runs (base = whatever the files currently hold).

> **In one line —** Keeps a short in-memory history of each file so snapshot reads get a consistent
> "as-of-start" view **without locks** — while the JSON files stay the durable source of truth.

### 6.3 StagedStore — letting dirty reads find unfinished writes

[`StagedStore.cs`](ATBS.Console/Transactions/Management/StagedStore.cs) is a small thread-safe map:
"final file path → the temporary files currently staged for it." Its only purpose is to let a **Read
Uncommitted** transaction peek at another transaction's *unfinished* write. When a write is staged,
it's registered here; when it commits or rolls back, it's removed.

> **In one line —** A small map of "unfinished writes" so a Read-Uncommitted transaction can peek at
> another transaction's not-yet-committed data (a dirty read).

---

## 7. Crash recovery

At startup, before serving any request,
[`TransactionRecovery.RecoverAll`](ATBS.Console/Transactions/TransactionRecovery.cs) scans the log
folder and repairs anything a crash left behind. For each leftover log, the **status** tells it
exactly what to do:

| Log status found         | What it means                          | Recovery action                                                                      |
| ------------------------ | -------------------------------------- | ------------------------------------------------------------------------------------ |
| `Pending`                | crashed **before** commit started      | **roll back** — originals were never touched, just delete temporary files            |
| `Committing`             | crashed **mid-commit**                 | **roll forward** — re-run the remaining temporary → final moves, then mark committed |
| `Committed`              | crashed after commit, before cleanup   | just clean up temporary files + the log                                              |
| `RollingBack`            | crashed while undoing                  | roll back again (safe to repeat)                                                     |
| `RolledBack`             | already undone                         | clean up                                                                             |
| unreadable / corrupt log | the log file itself was torn mid-write | best-effort delete of orphaned `*.temporary` / `*.backup` files, then remove the log |

The key invariant, again: because the log becomes `Committing` **before** any real file is
overwritten, a half-finished commit is always **safe to finish** (roll forward) rather than
ambiguous. This is textbook WAL redo/undo reasoning — just implemented over ordinary files.

> **In one line —** At startup the log's **status** tells recovery exactly what to do — roll back,
> roll forward, or just clean up — so a crash is never an ambiguous "did it commit or not?".

---

## 8. Making it easy to use: the factory, retry, and the transaction context

### 8.1 `ExecuteAsync` — begin/commit/rollback/retry, done for you

Services never write `try/commit/catch/rollback/retry` by hand. They hand a piece of work to
[`FileTransactionFactory.ExecuteAsync`](ATBS.Console/Transactions/FileTransactionFactory.cs), which:

1. **Begins** a transaction at the chosen isolation level.
2. Runs your `work()`.
3. If `work` returns a **success** result → **commit**. If it returns a **business error** result
   (e.g. "no seats") → let the scope roll back (an error is not a crash, but it shouldn't commit).
4. If `work` throws a `TransactionConflictException` (deadlock victim, snapshot conflict, or lock
   timeout) → roll back and **retry**, with **exponential backoff + random jitter**, up to
   `maxRetries` (default **5**). The jitter stops retries from colliding again in lock-step. If
   retries run out, it returns a `Transactions.Conflict` error instead of looping forever.
5. Any **other** exception (a real bug) → roll back and rethrow. Bugs are never silently swallowed.

This mirrors how real SQL applications are expected to treat "deadlock victim" errors: catch, wait a
bit, try again.

> **In one line —** Hand your work to `ExecuteAsync` and it does begin/commit/rollback for you:
> success → commit, business error → roll back, transient conflict → retry (5 × with backoff + jitter),
> real bug → rethrow.

### 8.2 The transaction context (`FileTransactionContext` + `AsyncLocal`)

The problem: the transaction is started high up (in a service), but the code that actually reads and
writes files (`TransactionalJsonFileStorage`) sits several layers below. We don't want to pass the
transaction as an argument through every method in between (so I searched about something that solve this for me).

The solution:
[`FileTransactionContext`](ATBS.Console/Transactions/FileTransactionContext.cs) holds an
`AsyncLocal<FileTransaction?>` — an "ambient" slot that flows **down the async call chain
automatically**. `Begin` puts the new transaction in that slot; the scope restores the previous
value when disposed. So the storage layer can just ask **"is there a current transaction?"**:

```csharp
var json = transactionContext.Current is { } transaction
    ? await transaction.Read(file)     // inside a transaction → go through the engine
    : await ReadDirectAsync(path);     // no transaction (e.g. seeding) → plain file read
```

`AsyncLocal` (rather than a plain field) matters because the context is a **singleton**: a plain
field would be shared by every concurrent operation, but `AsyncLocal` keeps a separate "current
transaction" per logical async flow, so concurrent operations never see each other's transaction.
This is the exact same trick .NET's own `Transaction.Current` uses. **Nested transactions are
rejected on purpose** — this is a flat, one-transaction-per-operation model.

> **In one line —** An `AsyncLocal` slot carries "the current transaction" down the async call chain,
> so deep storage code finds it without it being passed through every method — one transaction per
> logical flow, and nesting is rejected on purpose.

---

## 9. Trade-offs, limitations, and boundaries

This engine is genuinely ACID — but it is a *small* database.

### Boundaries (what it is designed for)

- **One process at a time.** The LockManager, VersionStore, and StagedStore all live **in memory**.
  They coordinate transactions **inside a single running program only**. If you started **two** copies
  of the app pointed at the same `Data/` folder, they would **not** see each other's locks or
  versions, and could corrupt each other. (Each write is still individually atomic thanks to the WAL,
  but cross-process isolation is simply out of scope).
- **Whole files are the unit of storage and locking.** There are exactly three logical "tables"
  (`Flights`, `Bookings`, `Passengers`), each one JSON file, mapped by
  [`TransactionFileCatalog`](ATBS.Console/Transactions/TransactionFileCatalog.cs).

### Limitations (things it can't or doesn't do)

- **Every write rewrites the entire file:** Changing one booking loads the whole `bookings.json`,
  edits the list in memory, and writes the whole thing back. Cost is O(file size) per write. Perfect
  for a class-sized data set; wrong for millions of rows.
- **Table-level locking isn't the best case always:** Because a lock covers a whole file, *any* write to `flights.json`
  blocks *every* other transaction that needs `flights.json`, even if they care about different
  flights. This reduces real concurrency. It's the price of not building row-level locking.
- **Repeatable Read and Serializable are the same here:** With whole-file locks, the
  shared-lock-held-to-commit already blocks inserts, so there is no behavioral difference between the
  two levels. Acceptable and documented — but worth knowing if row-level access were ever added.
- **Snapshot history is per-run and in-memory.** The version store is rebuilt from the current files
  each time the program starts; it is not a persistent MVCC store. Snapshots are short-lived (one
  transaction), so this is fine, but it means snapshot state does not survive a restart.
- **Deadlock/timeout tuning is fixed in code.** The 15 s lock timeout and 5-retry budget with
  backoff are hard-coded defaults, not configurable at runtime.

### Costs you accept on purpose

- **Fsync on every staged write and every log update.** Flushing all the way to physical disk is what
  makes durability real, but it is slower than a buffered write. That's the correct trade for
  correctness.
- **Global in-memory locks around the coordinators.** The LockManager and VersionStore each guard
  their tables with a single short-held lock. Critical sections are tiny (pure bookkeeping, no I/O
  held under the lock), so this is not a real bottleneck at this scale — but it does serialize their
  internal operations.

> **In one line —** It is genuinely ACID, but on purpose it is **single-process**, locks **whole
> files**, and **rewrites the whole file** on each change.


