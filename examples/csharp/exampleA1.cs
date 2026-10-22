using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA1
{

    // Three organizations hold the keys of Roaming Network Operator "OCC" and create together Roaming Network "QA"

    public const Byte minConsensus = 2;


    /// <summary>
    /// What example A1 leaves to the examples building on it: the private keys of the three organizations, the trust
    /// anchor of `QA`, and its history, whose head is the current version of `QA`. Disposing it closes the store of the
    /// history.
    /// </summary>
    /// <param name="IdentityKeys">The identity key of each organization.</param>
    /// <param name="SigningKeys">The signing key of each organization.</param>
    /// <param name="Signers">The signers of commits as the roaming network operator, one of each organization.</param>
    /// <param name="TrustAnchor">The trust anchor of `QA`, as its QR code shows it.</param>
    /// <param name="History">The history of `QA`, its genesis commit and all later ones.</param>
    /// <param name="Store">The store of the history, in poi/QA.</param>
    public sealed record Result(IReadOnlyDictionary<String, COSEKeyPair>     IdentityKeys,
                                IReadOnlyDictionary<String, COSEKeyPair>     SigningKeys,
                                IReadOnlyList<RoamingNetworkCommitSigner>    Signers,
                                RoamingNetworkTrustAnchor                    TrustAnchor,
                                RoamingNetworkHistory                        History,
                                RoamingNetworkHistoryFileStore               Store) : IDisposable
    {

        /// <summary>
        /// The current version of `QA`: the head of its history, not the genesis version.
        /// </summary>
        public RoamingNetwork RoamingNetwork
            => History.Head.RoamingNetwork;

        public void Dispose()
            => Store.Dispose();

    }


    public static Result Run()
    {

        #region 1. The identity keys of the organizations

        // Each organization makes an identity key, the long-term key that states the quorum and certifies its own signing
        // keys, and keeps its private key to itself. Each chooses its own kind of key. Two of the three decide.
        var identityKeys          = new Dictionary<String, COSEKeyPair> {
                                        { "AlphaOrg", COSEKeyPair.NewES512()   },
                                        { "BetaOrg",  COSEKeyPair.NewEd448()   },
                                        { "GammaOrg", COSEKeyPair.NewMLDsa65() }
                                    };

        foreach (var organization in identityKeys.Keys)
        {
            Directory.CreateDirectory(organization);
            Helpers.SaveKey(identityKeys[organization].Private, $"{organization}/identity-privateKey.pem");
        }

        // The public halves, each naming its organization.
        var publicIdentityKeys    = identityKeys.Keys.Select(organization => POIPublicKey.From(identityKeys[organization].Private, Holder: organization)).ToArray();

        #endregion

        #region 2. The key statement for the Roaming Network Operator "OCC" and the Roaming Network "QA"

        var now                   = Timestamp.Now;
        var occId                 = RoamingNetworkOperator_Id.Parse("OCC");
        var qaId                  = RoamingNetwork_Id.        Parse("QA");

        // AlphaOrg creates and signs the key statement.
        var keyStatementAlphaOrg  = RoamingNetworkKeyStatement.Create(
                                        identityKeys["AlphaOrg"],
                                        minConsensus,
                                        qaId,
                                        occId,
                                        IdentityKeys:  publicIdentityKeys,
                                        SignedAt:      now,
                                        ValidUntil:    now.AddYears(1)
                                    );

        // BetaOrg later also signs the key statement: 2/3 quorum reached!
        var keyStatementFinal     = keyStatementAlphaOrg.WithSignature(identityKeys["BetaOrg"]);
        var keyStatementJSON      = keyStatementFinal.ToJSON().ToString();

        #endregion

        #region 3. The signing keys, each certifies them itself via its identity keys

        // Each organization makes a signing key for everyday use: it signs commits, change sets and attestations, for the
        // party only (POIKeyUsages). Each certifies its own with its identity key, alone: renewing a signing key later
        // needs no one else, as it gives its organization one voice only.
        var signingKeys           = identityKeys.Keys.ToDictionary(organization => organization,
                                                                   organization => COSEKeyPair.NewES256());

        var publicSigningKeys     = identityKeys.Keys.Select(organization => POIPublicKey.From(signingKeys[organization].Private,
                                                                                               NotAfter:  now.AddYears(1),
                                                                                               Holder:    organization,
                                                                                               Usages:    [ POIKeyUsages.Party ])).ToArray();

        var certificates          = publicSigningKeys.Select(signingKey => RoamingNetworkSigningKeyCertificate.Create(
                                                                               identityKeys[signingKey.Holder!],
                                                                               occId,
                                                                               signingKey,
                                                                               now
                                                                           )).ToArray();

        foreach (var organization in signingKeys.Keys)
            Helpers.SaveKey(signingKeys[organization].Private, $"{organization}/signing-privateKey.pem");

        #endregion

        #region 4. Create `OCC` and `QA`

        var occ                   = new RoamingNetworkOperator(
                                        keyStatementFinal,
                                        Name:                    I18NString.Create("Open Charging Cloud"),
                                        SigningKeyCertificates:  certificates
                                    );

        var qa                    = occ.CreateRoamingNetwork(
                                        keyStatementFinal.RoamingNetworkId,
                                        Name:          ImmutableI18NString.Create("Quality Assurance"),
                                        Description:   ImmutableI18NString.Create("The roaming network of the examples")
                                    );

        #endregion

        #region 5. The OCC/QA trust anchor; also as QR code

        // The trust anchor, e.g. as a QR code on the website of OCC: the @ids of the identity keys and the quorum.
        var trustAnchor           = qa.CreateTrustAnchor(
                                        keyStatementFinal,
                                        URL.Parse("https://open.charging.cloud/RNs/QA")
                                    );

        Directory.CreateDirectory("published");
        File.WriteAllText("published/QA-trustAnchor.txt", trustAnchor.ToQRCode());

        Console.WriteLine(trustAnchor.ToJSON());
        Console.WriteLine($"QR code: {trustAnchor.ToQRCode().Length} characters, {trustAnchor.ToCBOR().ToByteArray(CBORWriterOptions.Canonical).Length} bytes of CBOR");
        Console.WriteLine(trustAnchor.ToQRCode());
        Console.WriteLine(trustAnchor.ToURL());
        Console.WriteLine();

        #endregion

        #region 6. RoamingNetworkHistory and the genesis commit

        // The genesis commit, signed by the signing keys of Alpha and Beta and Gamma, as the roaming network operator.
        RoamingNetworkCommitSigner SignerOf(String Holder)

            => new (signingKeys[Holder].Private,
                    POIPublicKey.From(signingKeys[Holder].Private).Id,
                    signingKeys[Holder].Algorithm,
                    RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                    occId.ToString());

        RoamingNetworkCommitSigner[] signers = [
            SignerOf("AlphaOrg"),
            SignerOf("BetaOrg"),
            SignerOf("GammaOrg")
        ];

        // The store stays open for the examples building on this one: the result closes it.
        var store                 = new RoamingNetworkHistoryFileStore($"poi/{qa.Id}");
        var history               = RoamingNetworkHistory.Start(
                                        qa,
                                        Signers:  signers,
                                        Store:    store
                                    );

        // What everybody may read: the roaming network with its ETags, and the commit leading to it.
        File.WriteAllText($"published/{qa.Id}.json",         history.Head.RoamingNetwork.ToJSON(IncludeETags: true).ToString());
        File.WriteAllText($"published/{qa.Id}-commit.json",  history.Head.Commit.        ToJSON().                  ToString());

        #endregion


        return new (identityKeys,
                    signingKeys,
                    signers,
                    trustAnchor,
                    history,
                    store);

    }


    /// <summary>
    /// Checks `QA` as a charging station operator or an app does: from what is published only, the text of the QR code,
    /// the roaming network and its commit; nothing of what Run() made.
    /// </summary>
    public static void Validate()
    {

        #region 7. Checking `QA`, as a charging station operator or an app, from the QR code and what it read

        // a) The trust anchor, as scanned.
        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        var networkJSON         = Helpers.ReadJSON("published/QA.json");
        var commitJSON          = Helpers.ReadJSON("published/QA-commit.json");

        // b) The roaming network: its @context, its content, the ETags it lists are those of its content, its quorum,
        //    the key statement it embeds is signed by identity keys of enough of its holders, and every certificate of a
        //    signing key by an identity key of the holder of that key.
        if (!RoamingNetwork.TryParse(networkJSON, out var network, out error))
            Helpers.Fail(error);

        if (network.Id != anchor.Id)
            Helpers.Fail("this is not the roaming network of the trust anchor");

        // c) The commit: its @id is the digest of its content.
        if (!RoamingNetworkCommit.TryParse(commitJSON, out var commit, out error))
            Helpers.Fail(error);

        // d) The commit leads to this roaming network: its ETags are those of the network.
        if (commit.RoamingNetworkId != network.Id || !network.MatchesCommitETags(commit.ETags))
            Helpers.Fail("the commit does not lead to the roaming network");

        // e) The key statement is signed by trusted identity keys of as many holders as the trust anchor requires,
        //    and is valid now.
        var statement = network.Operator?.KeyStatement;

        if (statement is null)
            Helpers.Fail("the roaming network operator states no keys");

        if (!statement.TryVerify(anchor.Keys,
                                 network.Operator!.IdentityKeys,
                                 out error,
                                 anchor.MinConsensus,
                                 At: Timestamp.Now))
            Helpers.Fail(error);

        // f) The commit is signed, as the roaming network operator, by signing keys of as many holders, each certified by a
        //    trusted identity key of its holder, valid when it signed, and one that may sign for the party. The identity keys
        //    of the key statement checked in e) are trusted as well.
        var trustedIds   = anchor.Keys.Concat(statement.IdentityKeys.Select(identityKey => identityKey.Id)).ToHashSet();
        var identityPool = statement.IdentityKeys.Concat(network.Operator.IdentityKeys).ToArray();
        var signedBy     = new HashSet<String>();

        foreach (var signature in commit.Signatures.Where(signature => signature.Role == RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator))
        {

            var key = network.Operator.SigningKeyCertificates.
                            FirstOrDefault(certificate => certificate.SigningKey.Id == signature.KeyId         &&
                                                          certificate.SigningKey.IsValidAt(signature.SignedAt) &&
                                                          certificate.SigningKey.MaySign(POIKeyUsages.Party)   &&
                                                          certificate.TryVerify(identityPool, out _, out _, trustedIds))?.
                            SigningKey;

            if (key is null)
                Helpers.Fail($"the key '{signature.KeyId}' is no signing key a trusted identity key of its holder certifies");

            if (!commit.VerifySignature(signature, key.Key, key.Id, out error))
                Helpers.Fail(error);

            signedBy.Add(key.HolderOrUnnamed);

        }

        if (signedBy.Count < Math.Max(anchor.MinConsensus, statement.MinConsensus))
            Helpers.Fail($"the commit is signed by {signedBy.Count} holder(s) of the roaming network operator only");

        // g) What to show: the name and description of the checked network and the name of its operator, which the
        //    signatures cover, not the texts of the QR code, which are not trusted; a QR code naming the network otherwise
        //    is told.
        Console.WriteLine($"Roaming network '{network.Id}'/'{network.Name.FirstText()}' ('{network.Description.FirstText()}'), operated by '{network.Operator.Id}'/'{network.Operator.Name.FirstText()}'");

        foreach (var (kind, shown, checkedText) in new[] { ("name",        anchor.Name,        network.Name),
                                                           ("description", anchor.Description, network.Description) })
            foreach (var text in shown)
                if (!checkedText.Has(text.Language) || checkedText[text.Language] != text.Text)
                    Console.WriteLine($"warning: the QR code shows the {kind} '{text.Text}' ({text.Language}), not the one of the network!");

        Console.WriteLine();

        // The same ETags, by hand: SHA-256 of the canonical JSON and of the deterministic CBOR of the network without
        // its eTags, and the root of its hash tree.
        var content    = (JObject) networkJSON.DeepClone();
        content.Remove("eTags");

        var json       = ETag.Compute(ETagFormat.JSON, CanonicalJSON.ToUTF8Bytes(content));
        var cbor       = ETag.Compute(ETagFormat.CBOR, CBORJSON.ToCBOR(CanonicalJSON.ToUTF8Bytes(content)).ToByteArray(CBORWriterOptions.Canonical));
        var tree       = network.EffectiveTreeETag(ETagHashAlgorithm.SHA256);

        Console.WriteLine($"json          {json}");
        Console.WriteLine($"cbor          {cbor}");
        Console.WriteLine($"effectiveTree {tree}");
        Console.WriteLine($"the commit's  {String.Join(", ", commit.ETags.Select(eTag => eTag.Format.ToString()))}: {(commit.ETags.SequenceEqual([ json, cbor, tree ]) ? "the same" : "others")}");

        // Checked: the version a change set of the charging station operator starts from.
        Console.WriteLine();
        Console.WriteLine($"QA is checked: revision {commit.Revision}, commit {commit.Id}, signed by {String.Join(" and ", signedBy.Order())}");
        Console.WriteLine($"beforeETags of the next change set: {String.Join(", ", network.CalcChangeSetETags())}");

        #endregion

    }

}
