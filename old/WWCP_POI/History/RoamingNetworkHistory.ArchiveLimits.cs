/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Text;
using System.Text.Json;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkHistory
{
    private void RequireCatalogLimits(RoamingNetworkHistoryLimits limits)
    {
        limits.Require(RoamingNetworkHistoryLimitKind.RetentionReceipts, retentionReceipts.Length);
        Int64 ids = 0;
        foreach (var receipt in retentionReceipts)
        {
            ids += (Int64) receipt.PrunedCommits.Length + receipt.ArchiveOnlyTips.Length;
            limits.Require(RoamingNetworkHistoryLimitKind.CatalogCommitIds, ids);
        }
    }

    // These scans visit only the archive and receipt containers. Commit payloads and
    // catalog identities are skipped without materializing a JSON/CBOR document.
    private static Byte[] CheckJSONArchive(String json, RoamingNetworkHistoryLimits limits)
    {
        ArgumentNullException.ThrowIfNull(json);
        limits.Require(RoamingNetworkHistoryLimitKind.ArchiveBytes, Encoding.UTF8.GetByteCount(json));
        var bytes = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(bytes);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected an archive object.");
        Int64 commits = 1, receipts = 0, ids = 0;
        while (ReadJSONItem(ref reader, JsonTokenType.EndObject))
        {
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected an archive property.");
            var isCommits = reader.ValueTextEquals("Commits");
            var isReceipts = reader.ValueTextEquals("RetentionReceipts");
            if (!reader.Read()) throw new JsonException("Missing archive property value.");
            if (isCommits) CountJSONArray(ref reader, limits, RoamingNetworkHistoryLimitKind.RetainedCommits, ref commits);
            else if (isReceipts)
            {
                RequireJSONArray(ref reader);
                while (ReadJSONItem(ref reader, JsonTokenType.EndArray))
                {
                    limits.Require(RoamingNetworkHistoryLimitKind.RetentionReceipts, ++receipts);
                    if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected a receipt object.");
                    while (ReadJSONItem(ref reader, JsonTokenType.EndObject))
                    {
                        if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected a receipt property.");
                        var isIds = reader.ValueTextEquals("PrunedCommits") || reader.ValueTextEquals("ArchiveOnlyTips");
                        if (!reader.Read()) throw new JsonException("Missing receipt property value.");
                        if (isIds) CountJSONArray(ref reader, limits, RoamingNetworkHistoryLimitKind.CatalogCommitIds, ref ids);
                        else reader.Skip();
                    }
                }
            }
            else reader.Skip();
        }
        if (reader.Read()) throw new JsonException("Trailing archive data.");
        return bytes;
    }

    private static Boolean ReadJSONItem(ref Utf8JsonReader reader, JsonTokenType end)
    {
        if (!reader.Read()) throw new JsonException("Incomplete archive container.");
        return reader.TokenType != end;
    }

    private static void RequireJSONArray(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected an archive array.");
    }

    private static void CountJSONArray(ref Utf8JsonReader reader, RoamingNetworkHistoryLimits limits,
        RoamingNetworkHistoryLimitKind kind, ref Int64 count)
    {
        RequireJSONArray(ref reader);
        while (ReadJSONItem(ref reader, JsonTokenType.EndArray))
        {
            limits.Require(kind, ++count);
            reader.Skip();
        }
    }

    private static void CheckCBORArchive(ReadOnlySpan<Byte> bytes, RoamingNetworkHistoryLimits limits)
        => CheckCBORArchiveCore(bytes, limits, default);

    private static void CheckCBORArchiveCore(ReadOnlySpan<Byte> bytes, RoamingNetworkHistoryLimits limits,
        POIArchiveReadProgress progress)
    {
        limits.Require(RoamingNetworkHistoryLimitKind.ArchiveBytes, bytes.Length);
        progress.Check(POIArchiveReadStage.Limits);
        var reader = new CBORReader(bytes);
        reader.ReadStartMap();
        Int64 commits = 1, receipts = 0, ids = 0;
        while (reader.PeekState() != CBORReaderState.EndMap)
        {
            progress.Check(POIArchiveReadStage.Limits, reader.Position);
            switch (reader.ReadTextString())
            {
                case "Commits":
                    CountCBORArray(ref reader, limits, RoamingNetworkHistoryLimitKind.RetainedCommits, ref commits, progress);
                    break;
                case "RetentionReceipts":
                    StartCBORArray(ref reader, limits, RoamingNetworkHistoryLimitKind.RetentionReceipts, receipts);
                    while (reader.PeekState() != CBORReaderState.EndArray)
                    {
                        progress.Check(POIArchiveReadStage.Limits, reader.Position);
                        limits.Require(RoamingNetworkHistoryLimitKind.RetentionReceipts, ++receipts);
                        reader.ReadStartMap();
                        while (reader.PeekState() != CBORReaderState.EndMap)
                        {
                            progress.Check(POIArchiveReadStage.Limits, reader.Position);
                            var name = reader.ReadTextString();
                            if (name is "PrunedCommits" or "ArchiveOnlyTips")
                                CountCBORArray(ref reader, limits, RoamingNetworkHistoryLimitKind.CatalogCommitIds, ref ids, progress);
                            else SkipCBORValue(ref reader, progress);
                        }
                        reader.ReadEndMap();
                    }
                    reader.ReadEndArray();
                    break;
                default:
                    SkipCBORValue(ref reader, progress);
                    break;
            }
        }
        reader.ReadEndMap();
        progress.Check(POIArchiveReadStage.Limits, reader.Position);
        if (reader.BytesRemaining != 0) throw new ArgumentException("Trailing archive data.");
    }

    private static void StartCBORArray(ref CBORReader reader, RoamingNetworkHistoryLimits limits,
        RoamingNetworkHistoryLimitKind kind, Int64 count)
    {
        if (reader.ReadStartArray() is Int32 length) limits.Require(kind, count + length);
    }

    private static void CountCBORArray(ref CBORReader reader, RoamingNetworkHistoryLimits limits,
        RoamingNetworkHistoryLimitKind kind, ref Int64 count, POIArchiveReadProgress progress)
    {
        StartCBORArray(ref reader, limits, kind, count);
        while (reader.PeekState() != CBORReaderState.EndArray)
        {
            progress.Check(POIArchiveReadStage.Limits, reader.Position);
            limits.Require(kind, ++count);
            SkipCBORValue(ref reader, progress);
        }
        reader.ReadEndArray();
    }

    private static void SkipCBORValue(ref CBORReader reader, POIArchiveReadProgress progress)
    {
        progress.Check(POIArchiveReadStage.Limits, reader.Position);
        reader.SkipValue();
        progress.Check(POIArchiveReadStage.Limits, reader.Position);
    }

}
