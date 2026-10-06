# External dependency patch

[Repository overview](../README.md)

`Styx.RestoreTimestamps.patch` records the one source change made outside this repository:
`../Styx/Styx/Illias/Helpers/AInternalData.cs` gained the protected
`RestoreTimestamps(DateTimeOffset Created, DateTimeOffset LastChange)` method.

The method restores persisted creation/change timestamps directly, without property-change events.
`AEMobilityEntity.RestoreSnapshotTimestamps()` calls it during JSON deserialization.
The current POI sources require this API in their Styx dependency.

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