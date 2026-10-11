/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Globalization;
using System.Text.Json;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

// The archive adapter emits existing wire fields directly. Standalone/pack codecs and identity
// preimages retain their own representations, including lossless application number spelling.
internal static class POIArchiveCBORValues
{
    internal static void WriteArchiveCBOR(this RoamingNetworkCommit commit, POIArchiveCBORWriter writer)
        => writer.Map(
            ("Profile", value => value.Text(commit.Profile)),
            ("ContentProfile", value => value.Text(commit.ContentProfile)),
            ("Id", value => value.Value(commit.Id.ToCBOR())),
            ("RoamingNetworkId", value => value.Text(commit.RoamingNetworkId)),
            ("Revision", value => value.Value(CBORValue.FromInt64(commit.Revision))),
            ("Parents", value => value.Array(commit.Parents.Length, commit.Parents, (output, id) => output.Value(id.ToCBOR()))),
            ("StateETags", value => value.Array(commit.StateETags.Length, commit.StateETags, (output, tag) => output.Value(tag.ToCBOR()))),
            ("AppliedChangeSetId", value => value.Value(commit.AppliedChangeSetId is { } id ? CBORValue.FromText(id) : CBORValue.Null)),
            commit.Snapshot is { } snapshot ? ("Snapshot", value => WriteSnapshot(snapshot, value)) :
                ("ChangeSet", value => { if (commit.ChangeSet is { } batch) value.ChangeSet(batch); else value.Value(CBORValue.Null); }),
            ("Signatures", value => value.Array(commit.Signatures.Length, commit.Signatures, (output, signature) => output.Map(
                ("Profile", field => field.Text(signature.Profile)), ("Algorithm", field => field.Text(signature.Algorithm)),
                ("KeyId", field => field.Text(signature.KeyId)), ("Encoding", field => field.Text(signature.Encoding)),
                ("Value", field => field.Text(signature.Value))))));

    private static void WriteSnapshot(RoamingNetworkSnapshotContent snapshot, POIArchiveCBORWriter writer)
        => writer.Map(
            ("CreatedAt", value => value.Text(snapshot.CreatedAt.ToString("O", CultureInfo.InvariantCulture))),
            ("Description", value => value.Value(RoamingNetworkChangeSet.EncodeApplicationJSON(JsonSerializer.SerializeToElement(snapshot.Description)))),
            ("Metadata", value => value.Value(RoamingNetworkChangeSet.EncodeApplicationJSON(JsonSerializer.SerializeToElement(snapshot.Metadata)))),
            ("State", value => value.POI(snapshot.State)));

    internal static void WriteArchiveCBOR(this RoamingNetworkRetentionReceipt receipt, POIArchiveCBORWriter writer)
        => writer.Map(
            ("Id", value => value.Value(receipt.Id.ToCBOR())),
            ("Profile", value => value.Text(RoamingNetworkRetentionReceipt.Profile)),
            ("PlanId", value => value.Value(receipt.PlanId.ToCBOR())),
            ("CheckpointId", value => value.Value(receipt.Checkpoint.ToCBOR())),
            ("BeforeAnchor", value => value.Value(receipt.BeforeAnchor.ToCBOR())),
            ("AfterAnchor", value => value.Value(receipt.AfterAnchor.ToCBOR())),
            ("Head", value => value.Value(receipt.Head.ToCBOR())),
            ("CreatedAt", value => value.Text(receipt.CreatedAt.ToString("O", CultureInfo.InvariantCulture))),
            ("SourceArchiveETag", value => value.Value(receipt.SourceArchiveETag.ToCBOR())),
            ("PrunedCommits", value => value.Array(receipt.PrunedCommits.Length, receipt.PrunedCommits, (output, id) => output.Value(id.ToCBOR()))),
            ("ArchiveOnlyTips", value => value.Array(receipt.ArchiveOnlyTips.Length, receipt.ArchiveOnlyTips, (output, id) => output.Value(id.ToCBOR()))));
}
