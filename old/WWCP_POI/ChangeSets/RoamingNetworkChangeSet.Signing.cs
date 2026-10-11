/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

using Org.BouncyCastle.Crypto;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial record RoamingNetworkChangeSet
    {

        /// <summary>
        /// Versioned, domain-separated Styx canonical JSON profile for complete ChangeSet signatures.
        /// </summary>
        public const String SigningProfile = "wwcp-poi-changeset-json-v2";

        /// <summary>
        /// Append a peer signature using Styx's asymmetric COSE algorithm primitives and return
        /// a signed immutable copy. Deterministic ECDSA uses RFC 6979; private keys are never retained.
        /// </summary>
        public RoamingNetworkChangeSet Sign(AsymmetricKeyParameter privateKey,
                                            String keyId,
                                            COSEAlgorithm algorithm,
                                            Boolean deterministic = true)
        {
            ArgumentNullException.ThrowIfNull(privateKey);
            if (!privateKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(privateKey));

            var bytes = GetSigningBytes(algorithm, keyId);
            var value = algorithm.Sign(bytes, privateKey, Deterministic: deterministic);
            return WithSignature(new(algorithm.Name, keyId, Convert.ToBase64String(value)));
        }

        /// <summary>
        /// Sign with a Styx COSE key, honoring its declared algorithm restriction when present.
        /// </summary>
        public RoamingNetworkChangeSet Sign(COSEKey privateKey,
                                            String keyId,
                                            COSEAlgorithm? algorithm = null,
                                            Boolean deterministic = true)
        {
            ArgumentNullException.ThrowIfNull(privateKey);
            var selected = algorithm ?? privateKey.Algorithm ??
                           throw new ArgumentException("An explicit algorithm or a key algorithm is required.", nameof(algorithm));
            if (privateKey.Algorithm is { } restricted && restricted != selected)
                throw new ArgumentException("The selected algorithm differs from the key's algorithm restriction.", nameof(algorithm));
            return Sign(privateKey.ToPrivateKey(), keyId, selected, deterministic);
        }

        /// <summary>
        /// Attempt signing without returning a partially signed batch.
        /// </summary>
        public Boolean TrySign(AsymmetricKeyParameter privateKey,
                               String keyId,
                               COSEAlgorithm algorithm,
                               [NotNullWhen(true)] out RoamingNetworkChangeSet? signedChangeSet,
                               [NotNullWhen(false)] out String? error,
                               Boolean deterministic = true)
        {
            signedChangeSet = null;
            error = null;
            try
            {
                signedChangeSet = Sign(privateKey, keyId, algorithm, deterministic);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Attempt signing with a Styx COSE key without returning a partial result.
        /// </summary>
        public Boolean TrySign(COSEKey privateKey,
                               String keyId,
                               [NotNullWhen(true)] out RoamingNetworkChangeSet? signedChangeSet,
                               [NotNullWhen(false)] out String? error,
                               COSEAlgorithm? algorithm = null,
                               Boolean deterministic = true)
        {
            signedChangeSet = null;
            error = null;
            try
            {
                signedChangeSet = Sign(privateKey, keyId, algorithm, deterministic);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Verify this complete batch with a trusted public key and its expected application key ID.
        /// Unknown profiles, noncanonical Base64, mismatched keys and malformed inputs return false.
        /// </summary>
        public Boolean VerifySignature(RoamingNetworkChangeSetSignature signature,
                                       AsymmetricKeyParameter publicKey,
                                       String expectedKeyId,
                                       [NotNullWhen(false)] out String? error)
        {
            error = null;
            try
            {
                ArgumentNullException.ThrowIfNull(publicKey);
                ArgumentNullException.ThrowIfNull(signature);
                if (publicKey.IsPrivate)
                    throw new ArgumentException("Verification requires a public asymmetric key.", nameof(publicKey));
                if (!Signatures.Contains(signature))
                    throw new ArgumentException("The signature is not part of this ChangeSet.", nameof(signature));
                if (signature.Profile != SigningProfile || signature.Encoding != "base64")
                    throw new ArgumentException("Unsupported ChangeSet signature profile or encoding.");
                if (signature.KeyId != Required(expectedKeyId, nameof(expectedKeyId)))
                    throw new ArgumentException("The signature key ID does not match the trusted key ID.");
                if (!COSEAlgorithm.TryParse(signature.Algorithm, out var algorithm) || algorithm.Name != signature.Algorithm)
                    throw new ArgumentException("Unknown or noncanonical signature algorithm.");
                var value = Convert.FromBase64String(signature.Value);
                if (value.Length == 0 || Convert.ToBase64String(value) != signature.Value)
                    throw new ArgumentException("Signature Value must contain nonempty canonical standard Base64.");

                return algorithm.Verify(GetSigningBytes(algorithm, signature.KeyId), value, publicKey, out error);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Verify using a trusted Styx COSE public key, honoring its algorithm restriction.
        /// </summary>
        public Boolean VerifySignature(RoamingNetworkChangeSetSignature signature,
                                       COSEKey publicKey,
                                       String expectedKeyId,
                                       [NotNullWhen(false)] out String? error)
        {
            error = null;
            try
            {
                ArgumentNullException.ThrowIfNull(publicKey);
                ArgumentNullException.ThrowIfNull(signature);
                if (publicKey.IsPrivate)
                    throw new ArgumentException("Supply the public COSE key for verification.", nameof(publicKey));
                if (publicKey.Algorithm is { } restricted && signature.Algorithm != restricted.Name)
                    throw new ArgumentException("The signature algorithm differs from the key's algorithm restriction.");
                return VerifySignature(signature, publicKey.ToPublicKey(), expectedKeyId, out error);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Verify every peer signature using trusted public keys resolved by the application.
        /// An unsigned batch, unknown key or any invalid signature returns false.
        /// </summary>
        public Boolean VerifySignatures(Func<RoamingNetworkChangeSetSignature, AsymmetricKeyParameter?> resolvePublicKey,
                                        [NotNullWhen(false)] out String? error)
        {
            error = null;
            try
            {
                ArgumentNullException.ThrowIfNull(resolvePublicKey);
                if (Signatures.IsEmpty)
                    throw new InvalidOperationException("The ChangeSet is unsigned.");
                using var preparation = POICanonicalPreparation.Enter();
                for (var index = 0; index < Signatures.Length; index++)
                {
                    var signature = Signatures[index];
                    var publicKey = resolvePublicKey(signature);
                    if (publicKey is null)
                    {
                        error = $"Signatures[{index}]: no trusted public key for '{signature.KeyId}'.";
                        return false;
                    }
                    if (!VerifySignature(signature, publicKey, signature.KeyId, out var verificationError))
                    {
                        error = $"Signatures[{index}] ('{signature.KeyId}'): {verificationError}";
                        return false;
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Return the fixed profile's canonical UTF-8 signature input, independent of serializer options.
        /// ETags use HEX; operation numbers and unit strings retain their exact JSON representations.
        /// </summary>
        public Byte[] GetSigningBytes(COSEAlgorithm algorithm, String keyId)
        {
            if (!algorithm.IsSupportedForSigning)
                throw new ArgumentException("The algorithm is not supported for asymmetric signing.", nameof(algorithm));
            Required(keyId, nameof(keyId));

            return POICanonicalPreparation.SigningBytes(this, WriteUnsignedContent, writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("Profile", SigningProfile);
                writer.WriteString("Algorithm", algorithm.Name);
                writer.WriteString("KeyId", keyId);
                writer.WriteString("Encoding", "base64");
                writer.WriteNull("ChangeSet");
                writer.WriteEndObject();
            }, "ChangeSet", () => GetOriginalSigningBytes(algorithm, keyId));
        }

        private Byte[] GetOriginalSigningBytes(COSEAlgorithm algorithm, String keyId)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteString("Profile", SigningProfile);
                writer.WriteString("Algorithm", algorithm.Name);
                writer.WriteString("KeyId", keyId);
                writer.WriteString("Encoding", "base64");
                writer.WritePropertyName("ChangeSet");
                WriteUnsignedContent(writer);
                writer.WriteEndObject();
            }
            using var document = JsonDocument.Parse(stream.ToArray());
            return CanonicalJSON.ToUTF8Bytes(document);
        }

        // Shared by the v2 batch signature and the commit identity profile.
        internal void WriteUnsignedContent(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Id", Id);
            writer.WriteString("RoamingNetworkId", RoamingNetworkId);
            writer.WriteNumber("BaseRevision", BaseRevision);
            writer.WriteString("CreatedAt", CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            WriteSigningETags(writer, "BeforeETags", BeforeETags);
            WriteSigningETags(writer, "AfterETags", AfterETags);
            writer.WritePropertyName("Description");
            writer.WriteStartObject();
            foreach (var entry in Description)
                writer.WriteString(entry.Key, entry.Value);
            writer.WriteEndObject();
            writer.WritePropertyName("Metadata");
            writer.WriteStartObject();
            foreach (var entry in Metadata)
            {
                writer.WritePropertyName(entry.Key);
                ValidateSigningJSON(entry.Value);
                entry.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
            writer.WritePropertyName("Changes");
            writer.WriteStartArray();
            foreach (var operation in Changes)
            {
                writer.WriteStartObject();
                writer.WriteString("Kind", operation.Kind.ToString());
                writer.WriteString("EntityType", operation.EntityType);
                writer.WriteString("EntityId", operation.EntityId);
                if (operation.PropertyName is { } property)
                    writer.WriteString("PropertyName", property);
                if (operation.ParentEntityType is { } parentType)
                    writer.WriteString("ParentEntityType", parentType);
                if (operation.ParentEntityId is { } parentId)
                    writer.WriteString("ParentEntityId", parentId);
                writer.WritePropertyName("ElementPath");
                writer.WriteStartArray();
                foreach (var segment in operation.ElementPath)
                {
                    writer.WriteStartObject();
                    writer.WriteString("PropertyName", segment.PropertyName);
                    if (segment.ElementId is { } elementId)
                        writer.WriteString("ElementId", elementId);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                WriteSigningValue(writer, "OldValue", operation.OldValue);
                WriteSigningValue(writer, "NewValue", operation.NewValue);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private static void WriteSigningETags(Utf8JsonWriter writer, String field,
                                               System.Collections.Immutable.ImmutableArray<ETag> tags)
        {
            writer.WritePropertyName(field);
            writer.WriteStartArray();
            foreach (var tag in tags)
                tag.WriteTo(writer);
            writer.WriteEndArray();
        }

        private static void WriteSigningValue(Utf8JsonWriter writer, String field, JsonElement? value)
        {
            if (value is { } element)
            {
                ValidateSigningJSON(element);
                writer.WritePropertyName(field);
                element.WriteTo(writer);
            }
        }

        internal static void ValidateSigningJSON(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Undefined)
                throw new ArgumentException("Undefined JSON values cannot be signed or stored as metadata.");
            if (value.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<String>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                        throw new ArgumentException($"Duplicate JSON property '{property.Name}' is not allowed in signed content.");
                    ValidateSigningJSON(property.Value);
                }
            }
            else if (value.ValueKind == JsonValueKind.Array)
                foreach (var item in value.EnumerateArray())
                    ValidateSigningJSON(item);
        }

    }

}
