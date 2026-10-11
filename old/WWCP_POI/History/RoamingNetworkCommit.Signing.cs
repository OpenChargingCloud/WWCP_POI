/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Org.BouncyCastle.Crypto;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkCommit
{
    /// <summary>
    /// The canonical commit signature profile, binding ancestry independently of batch signatures.
    /// </summary>
    public const String SigningProfile = "wwcp-poi-commit-signature-json-v1";

    /// <summary>
    /// The domain-separated signing profile for complete snapshot links and their ancestry.
    /// </summary>
    public const String SnapshotSigningProfile = "wwcp-poi-snapshot-commit-signature-json-v1";

    /// <summary>
    /// The required signature profile for this payload kind.
    /// </summary>
    public String SignatureProfile => Snapshot is null ? SigningProfile : SnapshotSigningProfile;

    /// <summary>
    /// Return the canonical signature input including algorithm, trusted key ID and full unsigned commit.
    /// </summary>
    public Byte[] GetSigningBytes(COSEAlgorithm algorithm, String keyId)
    {
        if (!algorithm.IsSupportedForSigning) throw new ArgumentException("Unsupported signing algorithm.", nameof(algorithm));
        if (String.IsNullOrWhiteSpace(keyId)) throw new ArgumentException("KeyId must not be empty.", nameof(keyId));
        return POICanonicalPreparation.SigningBytes(this, WriteIdentity, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Profile", SignatureProfile);
            writer.WriteString("Algorithm", algorithm.Name);
            writer.WriteString("KeyId", keyId);
            writer.WriteString("Encoding", "base64");
            writer.WritePropertyName("CommitId"); Id.Hash.WriteTo(writer);
            writer.WriteNull("Commit");
            writer.WriteEndObject();
        }, "Commit", () => GetOriginalSigningBytes(algorithm, keyId));
    }

    private Byte[] GetOriginalSigningBytes(COSEAlgorithm algorithm, String keyId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("Profile", SignatureProfile);
            writer.WriteString("Algorithm", algorithm.Name);
            writer.WriteString("KeyId", keyId);
            writer.WriteString("Encoding", "base64");
            writer.WritePropertyName("CommitId"); Id.Hash.WriteTo(writer);
            writer.WritePropertyName("Commit"); WriteIdentity(writer);
            writer.WriteEndObject();
        }
        using var document = JsonDocument.Parse(stream.ToArray());
        return CanonicalJSON.ToUTF8Bytes(document);
    }

    /// <summary>
    /// Append an equal peer signature authenticating this commit and its ancestry.
    /// </summary>
    public RoamingNetworkCommit Sign(AsymmetricKeyParameter privateKey, String keyId, COSEAlgorithm algorithm,
                                     Boolean deterministic = true)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        if (!privateKey.IsPrivate) throw new ArgumentException("Signing requires a private asymmetric key.", nameof(privateKey));
        var value = algorithm.Sign(GetSigningBytes(algorithm, keyId), privateKey, Deterministic: deterministic);
        return WithSignature(new(algorithm.Name, keyId, Convert.ToBase64String(value), SignatureProfile));
    }

    /// <summary>
    /// Sign using a Styx COSE private key and honor its algorithm restriction.
    /// </summary>
    public RoamingNetworkCommit Sign(COSEKey privateKey, String keyId, COSEAlgorithm? algorithm = null,
                                     Boolean deterministic = true)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        var selected = algorithm ?? privateKey.Algorithm ?? throw new ArgumentException("A signing algorithm is required.");
        if (privateKey.Algorithm is { } restricted && selected != restricted)
            throw new ArgumentException("The algorithm differs from the key's restriction.");
        return Sign(privateKey.ToPrivateKey(), keyId, selected, deterministic);
    }

    /// <summary>
    /// Try to sign without returning a partially signed commit.
    /// </summary>
    public Boolean TrySign(AsymmetricKeyParameter privateKey, String keyId, COSEAlgorithm algorithm,
                           [NotNullWhen(true)] out RoamingNetworkCommit? signedCommit,
                           [NotNullWhen(false)] out String? error, Boolean deterministic = true)
    {
        try { signedCommit = Sign(privateKey, keyId, algorithm, deterministic); error = null; return true; }
        catch (Exception exception) { signedCommit = null; error = exception.Message; return false; }
    }

    /// <summary>
    /// Try to sign using a restricted Styx COSE private key.
    /// </summary>
    public Boolean TrySign(COSEKey privateKey, String keyId,
                           [NotNullWhen(true)] out RoamingNetworkCommit? signedCommit,
                           [NotNullWhen(false)] out String? error, COSEAlgorithm? algorithm = null,
                           Boolean deterministic = true)
    {
        try { signedCommit = Sign(privateKey, keyId, algorithm, deterministic); error = null; return true; }
        catch (Exception exception) { signedCommit = null; error = exception.Message; return false; }
    }

    /// <summary>
    /// Verify one peer against an explicitly trusted public key and application key identifier.
    /// </summary>
    public Boolean VerifySignature(RoamingNetworkChangeSetSignature signature, AsymmetricKeyParameter publicKey,
                                   String expectedKeyId, [NotNullWhen(false)] out String? error)
    {
        error = null;
        try
        {
            ArgumentNullException.ThrowIfNull(publicKey);
            ArgumentNullException.ThrowIfNull(signature);
            if (publicKey.IsPrivate) throw new ArgumentException("Verification requires a public key.");
            if (!Signatures.Contains(signature)) throw new ArgumentException("The signature is not part of this commit.");
            if (signature.Profile != SignatureProfile || signature.Encoding != "base64")
                throw new ArgumentException("Unsupported commit signature profile or encoding.");
            if (String.IsNullOrWhiteSpace(expectedKeyId) || signature.KeyId != expectedKeyId)
                throw new ArgumentException("The signature key ID does not match the trusted key ID.");
            if (!COSEAlgorithm.TryParse(signature.Algorithm, out var algorithm) || algorithm.Name != signature.Algorithm)
                throw new ArgumentException("Unknown or noncanonical signature algorithm.");
            var value = Convert.FromBase64String(signature.Value);
            if (value.Length == 0 || Convert.ToBase64String(value) != signature.Value)
                throw new ArgumentException("Signature Value requires nonempty canonical standard Base64.");
            return algorithm.Verify(GetSigningBytes(algorithm, signature.KeyId), value, publicKey, out error);
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }

    /// <summary>
    /// Verify with a trusted Styx COSE public key and honor its algorithm restriction.
    /// </summary>
    public Boolean VerifySignature(RoamingNetworkChangeSetSignature signature, COSEKey publicKey,
                                   String expectedKeyId, [NotNullWhen(false)] out String? error)
    {
        error = null;
        try
        {
            ArgumentNullException.ThrowIfNull(publicKey);
            ArgumentNullException.ThrowIfNull(signature);
            if (publicKey.IsPrivate) throw new ArgumentException("Verification requires a public COSE key.");
            if (publicKey.Algorithm is { } restricted && signature.Algorithm != restricted.Name)
                throw new ArgumentException("The signature algorithm differs from the key's restriction.");
            return VerifySignature(signature, publicKey.ToPublicKey(), expectedKeyId, out error);
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }

    /// <summary>
    /// Verify all equal peers using application-trusted keys; unsigned commits return false.
    /// </summary>
    public Boolean VerifySignatures(Func<RoamingNetworkChangeSetSignature, AsymmetricKeyParameter?> resolvePublicKey,
                                    [NotNullWhen(false)] out String? error)
    {
        error = null;
        try
        {
            ArgumentNullException.ThrowIfNull(resolvePublicKey);
            if (Signatures.IsEmpty) throw new InvalidOperationException("The commit is unsigned.");
            using var preparation = POICanonicalPreparation.Enter();
            for (var index = 0; index < Signatures.Length; index++)
            {
                var signature = Signatures[index];
                var key = resolvePublicKey(signature);
                if (key is null) throw new ArgumentException($"Signatures[{index}]: no trusted key for '{signature.KeyId}'.");
                if (!VerifySignature(signature, key, signature.KeyId, out var verificationError))
                    throw new ArgumentException($"Signatures[{index}] ('{signature.KeyId}'): {verificationError}");
            }
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }
}
