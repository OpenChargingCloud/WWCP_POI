/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

#region Usings

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial record RoamingNetworkChangeSet
    {

        /// <summary>
        /// Versioned, domain-separated Styx canonical JSON profile for complete ChangeSet signatures; v3 signs the Newtonsoft JSON representation.
        /// </summary>
        public const String SigningProfile = "wwcp-poi-changeset-json-v3";

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
                if (signature.KeyId != expectedKeyId)//Required(expectedKeyId, nameof(expectedKeyId)))
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
        /// Return the fixed profile's canonical UTF-8 signature input:
        /// the signature header and the JSON representation of this batch without its signatures,
        /// written as Styx canonical JSON. ETags use HEX.
        /// </summary>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="KeyId">The application identifier of the signing key.</param>
        public Byte[] GetSigningBytes(COSEAlgorithm  Algorithm,
                                      String         KeyId)
        {

            if (!Algorithm.IsSupportedForSigning)
                throw new ArgumentException("The algorithm is not supported for asymmetric signing.", nameof(Algorithm));

            if (String.IsNullOrWhiteSpace(KeyId))
                throw new ArgumentException("A key identification is required.", nameof(KeyId));

            return CanonicalJSON.ToUTF8Bytes(
                       new JObject(
                           new JProperty(RoamingNetworkChangeSetSignature.PropertyKeys.Profile,    SigningProfile),
                           new JProperty(RoamingNetworkChangeSetSignature.PropertyKeys.Algorithm,  Algorithm.Name),
                           new JProperty(RoamingNetworkChangeSetSignature.PropertyKeys.KeyId,      KeyId),
                           new JProperty(RoamingNetworkChangeSetSignature.PropertyKeys.Encoding,   "base64"),
                           new JProperty(PropertyKeys.ChangeSet,                                   ToJSON(Embedded:           true,
                                                                                                          IncludeSignatures:  false))
                       )
                   );

        }

    }

}
