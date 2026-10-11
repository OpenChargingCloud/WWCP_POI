# Incremental commit exchange and head adoption

`RoamingNetworkHistory` exchanges original immutable commits without transferring a full archive
for every update. The transport-independent APIs separate retained static history from the
published head. JSON and native CBOR carry the same identities, batches and peer signatures.
Current statuses, schedules, measurements and forecasts never enter these messages.

## Announce retained history

`GetReplicationState()` returns an immutable `RoamingNetworkReplicationState`:

| Field | Meaning |
| --- | --- |
| `Profile` | `wwcp-poi-replication-state-v1`, or version 2 for a trusted snapshot boundary |
| `ContentProfile` | `wwcp-poi-static-v2` |
| `Checkpoint` | Typed shared checkpoint commit ID |
| `Anchor` | Version 2 only: the retained snapshot root, excluding its earlier ancestry |
| `Head` | Typed published commit ID |
| `KnownTips` | Deterministically ordered retained DAG frontier, including unpublished branches |

Acknowledging a tip acknowledges every parent path **down to the declared replay root**, rather than only
its first-parent chain. Version 1 reaches the original checkpoint; version 2 explicitly stops at its snapshot
`Anchor` and does not acknowledge earlier parents. An announcement produced by the history satisfies this rule. Applications
must not claim partially received or merely advertised IDs as retained tips. The sender ignores
unknown tips, allowing replicas to announce local divergent branches the sender has not seen.

`ToJSON`/`Parse` and `ToCBOR`/`ParseCBOR` use typed digest tuples. The JSON label remains `json`
for commit identities transported as CBOR. Parsers reject duplicate/unknown fields and unsupported
profiles. Announcement parsers default to 4,096 tips and 1 MiB of raw input; limits are configurable.
The announcement is an unauthenticated inventory hint. The application authenticates the channel
and authorizes its participants; commit signatures authenticate individual original envelopes.

## Request a fixed tip in bounded pages

`TryCreateCommitPack(receiverState, targetTip, out pack, out result, maxCommits, maxBytes)` computes
the requested tip's ancestry, subtracts acknowledged ancestry and returns a deterministic
parent-before-child prefix. It includes **all merge parents**. The target may be an unpublished
retained branch. Pin its ID for the whole transfer even if the sender publishes another head.

Default bounds are 128 transitions and 1 MiB per page. The bound includes the entire envelope and
checkpoint signatures in both emitted JSON UTF-8 and deterministic CBOR; either representation
fits `maxBytes`. Commit envelopes are indivisible. An oversized next commit or checkpoint/header
returns `CommitTooLarge`; the application can explicitly increase its limits or reject the transfer.
The checkpoint envelope does not count toward the transition-count limit.

`RoamingNetworkCommitPack` has the following exact wire contract:

| Field | Meaning |
| --- | --- |
| `Profile` | `wwcp-poi-commit-pack-v1`, or `wwcp-poi-commit-pack-v2` when the page contains a full snapshot |
| `ContentProfile` | `wwcp-poi-static-v2` |
| `CheckpointCommit` | Shared checkpoint envelope with peer signatures, without a snapshot |
| `Tip` | Fixed requested commit ID |
| `Complete` | Sender has included all missing ancestry relative to this announcement |
| `Commits` | Distinct original transitions in parent-before-child order |

The checkpoint envelope allows its peer signatures to be delivered even when its ID is already
known. Known transition commits are omitted; use `TryStoreCommit` or an explicitly constructed
pack to deliver additional peers for such commits. Peer accumulation does not change commit IDs.

After each successful import, send a **fresh receiver announcement** and request the same target
again until `Complete` is true. No server cursor or pending-commit queue is needed. A complete
empty page is valid when the receiver already retains the tip. An incomplete page must contain
at least one transition. Messages do not select their declared tip as the local head.

## Atomic import and trust

Full [snapshot links](SNAPSHOTS.md) are original commit envelopes and count as one transition.
Import requires their retained parent, exact unchanged static state, revision increment and unchanged
last batch ID; snapshot links introduce no batch-ID entry. Their signatures/authorization are checked
through the commit policies. `pack.WireProfile` selects the page profile; parsers reject inconsistent
version/payload combinations. Complete-history snapshot-free pages retain version 1. A full snapshot
among the new transitions is indivisible under the page limit; bounded archive bootstrap
can transfer an oversized state in fragments. Snapshot publication/adoption preserves local runtime
and original entity lifetimes. [Authorized snapshot entry](SNAPSHOT-BOUNDARIES.md) now also supports
absent earlier parents. Version 3 pages have exact fields `Profile`, `ContentProfile`, `CheckpointId`,
`AnchorId`, `AnchorSignatures`, `Tip`, `Complete` and `Commits`. They reference the already retained
snapshot rather than retransmitting its state. `Parse`/`ParseCBOR` require `resolveAnchor: history.GetCommit`
for such pages; import independently rechecks trust and the original chain ID. Missing boundary roots
require explicit bootstrap/ancestry retrieval, never implicit activation. Complete-history version 1/2
pages retain their original contract. The root is excluded from the transition count, while all root
ID and peer envelopes count toward the byte limit.

`TryImportCommitPack` validates a page in temporary immutable maps. It verifies incoming peers,
unions retained peer envelopes and verifies the combined envelopes, reauthorizes consumed local
ancestry, resolves every parent, enforces batch-ID uniqueness and replays each new transition
against its first parent's snapshot. Revision, network ID, applied batch ID and both resulting
ETags must match the original commit. A declaration of `Complete` requires the tip to be retained
after validation. The page's tip/completion fields are routing hints outside commit signatures;
they confer no publication authority.

Missing external parents produce sorted typed `MissingCommits` and `MissingParents`. Fetch those
IDs and their ancestry, then retry the unchanged page. Parents present in a page must precede
their children. There is no unbounded buffer for unresolved commits. Duplicate deliveries are
validated again, accumulate accepted peers and retain the same content/ancestry.

For a persistent history, the complete candidate archive is flushed/replaced **once** before
installing the maps. Validation, authorization, ID conflicts and persistence failures retain
nothing from the page, including checkpoint/transition peer additions. Successful import changes
neither the head ID nor its network/runtime instance. The head envelope can acquire more peers.
`StoredCount` counts new transitions; `DuplicateCount` counts already retained transition IDs.

Parse raw messages with `RoamingNetworkCommitPack.Parse`/`ParseCBOR` before import. These parsers
check the raw byte limit before constructing the JSON/CBOR tree, and the transition-count limit
before constructing commit objects. Import independently applies count and serialized-size bounds
to a preconstructed pack. Applications should also cap network reads and concurrent requests.

| Replication outcome | Meaning |
| --- | --- |
| `PackAvailable` | A bounded page was prepared, possibly complete and empty |
| `Imported` | All page content was accepted, with new transitions retained |
| `AlreadyStored` | No new transition IDs; accepted peers may still have been added |
| `CheckpointMismatch` | Explicit bootstrap is required; local history is untouched |
| `UnknownTip` | Requested sender tip is unknown, without a recorded pruning event |
| `MissingParents` | Typed external dependencies or a declared complete tip are missing |
| `CommitTooLarge` | A page/count/header/indivisible commit exceeds the chosen limits |
| `ChangeSetIdConflict` | An existing batch ID denotes different content or ancestry |
| `InvalidInput` | Contract, order, trust, replay or state validation failed |
| `PersistenceFailure` | Candidate archive could not be installed |
| `Unavailable` | History is disposed or mutation callbacks attempted reentry |
| `SnapshotRequired` | A root is unavailable/unacknowledged or a requested tip has a pruning receipt; an optional signed proposal requires explicit authorization |
| `HistoryRequired` | Required ancestry lies outside the selected boundary or an import needs recorded cold dependencies |

[Explicit retention](RETENTION.md) supplies an optional `RetentionReceipt` for recorded archived IDs,
including the exact source cold archive ETag. `LookupCommit` distinguishes retained, archived and unknown
identities. A proposed boundary is chosen only when all head parent paths terminate there. Neither
receipts nor proposals authorize replacing a receiver's head/root; locate cold history or explicitly
authorize a compatible snapshot bootstrap. Receipt hashes are bookkeeping, not signed membership proofs.

## Explicit head adoption

`TryAdoptHead(expectedHead, targetTip, out result)` defaults to a **preview**. The target must
already be retained. The method rechecks trust on local and target ancestry and considers
**every parent edge** when deciding whether the target descends from the current head.

- `AdoptionAvailable`: a descendant can be selected; the preview changes nothing.
- `Adopted`: a call with `adopt: true` persisted and selected that original commit.
- `AlreadyCurrent`: idempotent current-tip delivery, accepted even with a stale expected ID.
- `AlreadyIntegrated`: the target is an ancestor; the head stays at its current descendant.
- `HeadConflict`: the expected head differs from the observed head.
- `Diverged`: neither tip descends from the other; explicitly preview/prepare a three-way merge.
- `UnknownTip`, `InvalidInput`, `PersistenceFailure` and `Unavailable`: no head change.

A merge prepared against left can therefore be adopted over its right parent after its original
ancestors arrive. This selects the **original signed commit**, retaining its identity, first-parent
revision and applied batch ID. It creates no synthetic transition, signature or merge record.
Revision numbers can decrease when adopting across a secondary parent whose chain was longer;
ancestry and the expected commit ID, rather than numeric revision, determine forward integration.
Generic additional parents are signed ancestry claims. Applications authorize those claims;
adoption does not rerun another sender's merge-resolution algorithm.

`adopt: true` captures local runtime while holding the same gate as `ApplyRuntimeUpdate`, then
persists the head reference before swapping the in-memory head. Failures leave the old network
and head available. Divergence never triggers an implicit merge, and this API provides no rewind
or force-reset option. Explicit full archive bootstrap remains a separate application decision.

### Local runtime lifetimes

The result's `RuntimeTransfer` explains the chosen policy:

| Policy | Runtime behavior |
| --- | --- |
| `None` | No head change |
| `FirstParentReplay` | Replay the original batches from the current head, preserving/resetting lifetimes through the existing copy-on-write rules |
| `ConservativeBranchContinuity` | Derive the retained target snapshot and copy local runtime only where continuity is proved across both first-parent branches |

For secondary-parent adoption, the shared first-parent ancestor anchors the proof. Graph nodes
must keep their complete owner chain, without any removal/recreation on either branch. Nested
meter/operator slots must additionally exist with the same owner path and identity in every
original branch source, without intervening clearing/removal/identity replacement. Changes are checked against their original source snapshots,
so removing an ancestor invalidates its descendants' lifetimes. Surviving statuses, history sizes,
measurements, forecasts and aggregation delegates are captured independently; foreign runtime
is never restored from a retained snapshot or transferred message.

This proof is deliberately conservative: objects introduced/recreated since that shared ancestor,
including objects present on the receiver branch and retained by the merge, start with domain
runtime defaults when continuity cannot be proved. Applications can deliver fresh scoped runtime
instructions after adoption. The static content/ETags do not change because of runtime retention.
Direct writes through old entity references still bypass the history gate.

## Example: exchange, preview, then select

Both histories have the same checkpoint and application-provided verifiers/authorization:

```csharp
var target = sender.GetReplicationState().Head; // Pin this ID across all pages.
var expected = receiver.Head.Id;
RoamingNetworkCommitPack page;
do
{
    var state = RoamingNetworkReplicationState.ParseCBOR(receiver.GetReplicationState().ToCBOR());
    if (!sender.TryCreateCommitPack(state, target, out var outgoing, out var export))
        throw new InvalidOperationException(export.Error);
    page = RoamingNetworkCommitPack.ParseCBOR(outgoing.ToCBOR());
    if (!receiver.TryImportCommitPack(page, out var import))
        throw new InvalidOperationException(import.Error);
}
while (!page.Complete);

if (!receiver.TryAdoptHead(expected, target, out var preview))
    throw new InvalidOperationException(preview.Error); // Diverged requires explicit TryMerge.
if (preview.Outcome == RoamingNetworkHeadAdoptionOutcome.AdoptionAvailable &&
    !receiver.TryAdoptHead(expected, target, out var adopted, adopt: true))
    throw new InvalidOperationException(adopted.Error);
```

For different checkpoints, explicitly inspect/authenticate a full archive and bootstrap a separate
history. [Bootstrap](BOOTSTRAP.md) now provides a frozen manifest, bounded JSON/CBOR fragments,
verified disk receipts, restart and preview/explicit activation. It preserves the complete original
checkpoint/history and initializes fresh local runtime. This incremental pack contract still requires
a shared checkpoint; it does not carry the snapshot itself. Full archives can also be imported through
`RoamingNetworkHistory.Parse`/`ParseCBOR`, with application read/storage and expected-head policy.

## Implementation and evidence

Sources: [replication](../WWCP_POI/History/RoamingNetworkHistory.Replication.cs),
[announcement](../WWCP_POI/History/RoamingNetworkReplicationState.cs),
[pack codec](../WWCP_POI/History/RoamingNetworkCommitPack.cs),
[adoption](../WWCP_POI/History/RoamingNetworkHistory.Adoption.cs) and
[runtime capture](../WWCP_POI/ChangeSets/RoamingNetwork.RuntimeState.cs).

The dedicated package has **61 passing cases**; the full suite on 2026-10-08 passed **952 tests**,
with one separately executed crash worker skipped in the parent process. Existing fixed reference
files were unchanged by that package. These executed cases predate static-v2; current files
were subsequently regenerated. The fixtures provide the following baseline evidence:

| Fixture | Cases and assertions |
| --- | --- |
| [ReplicationTests](../WWCP_POI_Tests/Interoperability/ReplicationTests.cs) | 34 cases: exact count/byte limits, UTF-8 raw bytes, bounded prefixes/oversized children, fixed-tip multi-page JSON/CBOR exchange, DAG frontier subtraction, checkpoint mismatch, missing/out-of-order parents, invalid ETags/preconditions/signatures/authorization, complete rollback of peer additions/new states/batch IDs, idempotent peers and revoked stored ancestry |
| [HeadAdoptionTests](../WWCP_POI_Tests/Interoperability/HeadAdoptionTests.cs) | 17 cases: original signed merge over its second parent, previews, fresh local runtime capture before lazy materialization, independent histories/measurements/forecasts, recreated/new/equal-ID meter and EVSE lifetimes, connection-point children, first-parent replay, revision decreases, divergence/stale heads, revoked secondary-parent trust, mutation reentry, competing adoption and runtime delivery waiting on the gate |
| [ReplicationPersistenceTests](../WWCP_POI_Tests/Interoperability/ReplicationPersistenceTests.cs) | 10 cases: a single archive replacement per page, disk-before-memory ordering, import/adoption failures before writing or after flushing, byte-for-byte unchanged disk/history/runtime, temporary-file cleanup, retry, original signature/head recovery and continued publication |

These are targeted cases, rather than exhaustive proofs for every graph/ownership/conflict
combination. The new persistence cases inject pre-replacement exceptions and exercise clean
reopen; the earlier crash fixture separately covers abrupt process exits around archive replacement.
The separate [bootstrap fixture](BOOTSTRAP.md#outcomes-and-evidence) adds 32 passing cases.
The [bootstrap crash fixture](BOOTSTRAP-CRASH-RECOVERY.md) adds 89 cases for receipt/activation exits,
exact destination recovery after acknowledgement loss and fresh trust/runtime before continuation.
The [structural merge fixture](MERGING.md#structural-merge-evidence) adds 34 passing cases for
integration conflicts, typed references, scoped identities, explicit ancestor choices and resolver failures.
HTTP transport, key negotiation, streaming archive replay, independent peer implementations
and larger production measurements remain application/future work. Separate [scaling evidence](SCALING.md)
covers retained archives/bootstrap/cold replay, rather than page transport or remote throughput. Export currently traverses retained
history, and persistence rewrites the full archive; page bounds do not bound total history memory
or replay/archive work. See the [roadmap](ROADMAP.md).
Administrator-controlled [archival/pruning](RETENTION.md) is implemented separately; its new catalog
and replication responses are verified in the [76-case snapshot/retention package](VERIFICATION-SNAPSHOTS-RETENTION.md).
The existing recreated-EVSE adoption test now asserts ReplaceModify and requires an explicit subtree
choice before verifying runtime reset and unrelated runtime preservation.
