# External dependency patch

[Repository overview](../README.md)

`Styx.RestoreTimestamps.patch` retains an earlier timestamp-helper change outside this repository:
`../Styx/Styx/Illias/Helpers/AInternalData.cs` gained the protected
`RestoreTimestamps(DateTimeOffset Created, DateTimeOffset LastChange)` method.

The method restores persisted creation/change timestamps directly, without property-change events.
Current POI entities use the local `AImmutableInternalData.RestoreTimestamps` implementation
through `AImmutableEMobilityEntity.RestoreSnapshotTimestamps()`; the unused mutable
`AEMobilityEntity` base has been removed. This patch is retained as a record and is not a
prerequisite of the current immutable POI metadata implementation.

The current serialization contract uses Styx canonical JSON, deterministic CBOR and
metrological tag 44252. The local readonly `ETag` value type serializes as a JSON/CBOR tuple,
with an explicit `hex`/`base64` encoding in JSON and native digest bytes in CBOR.
ChangeSets bind both source/result ETags; nested defaults
derive from the fixed batch timestamp. See [ETags/CBOR](../docs/ETAGS-CBOR.md) and
[ChangeSets](../docs/CHANGESETS.md) for the current dependency/API contract.

## Applying the patch

From this repository's root, inspect whether the method is already present:

```powershell
rg -n "protected void RestoreTimestamps" ../Styx/Styx/Illias/Helpers/AInternalData.cs
```

If it is absent and the Styx repository has working Git metadata:

```powershell
git -C ../Styx apply --check ../WWCP_POI/patches/Styx.RestoreTimestamps.patch
git -C ../Styx apply ../WWCP_POI/patches/Styx.RestoreTimestamps.patch
```

If the method is already present, do not apply the patch again. For a Styx version with different
surrounding source, port this small method manually and review it against that version.

The patch is kept here because the supplied sibling checkout's `.git` file points to missing
submodule metadata, preventing a separate Styx commit. It is not an automatic build step and does
not replace committing or adopting the API in the upstream Styx repository.
