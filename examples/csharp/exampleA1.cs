using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA1
{

    // The roaming network "QA" and its roaming network operator "OCC" begin with one organization, AlphaOrg, alone.
    // BetaOrg joins, two of two deciding from then on; GammaOrg joins, two of three. Each of them serves a copy of QA.
    // Each of them is a RoamingShareholder: an organization with its identity key, its signing key and the URL of its
    // copy, holding one voice among those who decide as OCC.

    /// <summary>
    /// How many of the shareholders of `OCC` decide, since BetaOrg joined AlphaOrg.
    /// </summary>
    public const Byte minConsensus = 2;


    /// <summary>
    /// What example A1 leaves to the examples building on it: the three shareholders of `OCC` with their private keys,
    /// the trust anchor of `QA`, and its history, whose head is the current version of `QA`. Disposing it closes the
    /// store of the history.
    /// </summary>
    /// <param name="AlphaOrg">The shareholder who began `QA`.</param>
    /// <param name="BetaOrg">The shareholder who joined first.</param>
    /// <param name="GammaOrg">The shareholder who joined second.</param>
    /// <param name="Signers">The signers of commits as the roaming network operator, one of each shareholder.</param>
    /// <param name="TrustAnchor">The trust anchor of `QA`, as its QR code shows it.</param>
    /// <param name="History">The history of `QA`, its genesis commit and all later ones.</param>
    /// <param name="Store">The store of the history, in poi/QA.</param>
    public sealed record Result(RoamingShareholder                           AlphaOrg,
                                RoamingShareholder                           BetaOrg,
                                RoamingShareholder                           GammaOrg,
                                IReadOnlyList<RoamingNetworkCommitSigner>    Signers,
                                RoamingNetworkTrustAnchor                    TrustAnchor,
                                RoamingNetworkHistory                        History,
                                RoamingNetworkHistoryFileStore               Store) : IDisposable
    {

        /// <summary>
        /// The shareholders of `OCC`, in the order they came.
        /// </summary>
        public IReadOnlyList<RoamingShareholder> Shareholders
            => [ AlphaOrg, BetaOrg, GammaOrg ];

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

        var now           = Timestamp.Now;
        var occId         = RoamingNetworkOperator_Id.Parse("OCC");
        var qaId          = RoamingNetwork_Id.        Parse("QA");

        Directory.CreateDirectory("published");

        #region Helpers: a new shareholder, and what a revision publishes

        // An organization makes an identity key, the long-term key that states who holds the keys of OCC and certifies its
        // own signing key, and a signing key for everyday use, ES256: it signs commits, change sets and attestations, for
        // the party only (POIKeyUsages). It keeps both private keys to itself, and serves its copy of QA at a URL of its
        // own: one roaming network, reachable at each of them.
        RoamingShareholder NewShareholder(String Name, COSEKeyPair IdentityKey, String Host)
        {

            var shareholder = RoamingShareholder.Create(
                                  Name,
                                  IdentityKey,
                                  URL: URL.Parse($"https://{Host}/RNs/{qaId}")
                              );

            Directory.CreateDirectory(Name);
            Helpers.SaveKey(shareholder.IdentityKey.Private, $"{Name}/identity-privateKey.pem");
            Helpers.SaveKey(shareholder.SigningKey. Private, $"{Name}/signing-privateKey.pem");

            return shareholder;

        }

        // What everybody may read of a revision: the roaming network with its ETags, the commit leading to it, and its
        // trust anchor, as a QR code: the @ids of the identity keys per holder, the quorum and the URLs of QA.
        RoamingNetworkTrustAnchor Publish(RoamingNetworkHistory History, String Headline)
        {

            var head         = History.Head;
            var network      = head.RoamingNetwork;
            var trustAnchor  = network.CreateTrustAnchor(network.Operator!.KeyStatement!);

            File.WriteAllText($"published/{qaId}-revision{head.Commit.Revision}.json",             network.    ToJSON(IncludeETags: true).ToString());
            File.WriteAllText($"published/{qaId}-revision{head.Commit.Revision}-commit.json",      head.Commit.ToJSON().                  ToString());
            File.WriteAllText($"published/{qaId}-trustAnchor-revision{head.Commit.Revision}.txt",  trustAnchor.ToQRCode());

            Console.WriteLine($"Revision {head.Commit.Revision}: {Headline}");
            Console.WriteLine($"  holders {String.Join(", ", POIPartyKeys.Holders(network.Operator.IdentityKeys).Order())}; minConsensus {network.Operator.MinConsensus}; commit {head.Commit.Id.ToString()[..8]}…, signed by {head.Commit.Signatures.Length} signing key(s)");
            Console.WriteLine($"  urls {String.Join(", ", network.URLs)}");
            Console.WriteLine($"  trust anchor: QR code of {trustAnchor.ToQRCode().Length} characters, {trustAnchor.ToCBOR().ToByteArray(CBORWriterOptions.Canonical).Length} bytes of CBOR");
            Console.WriteLine();

            return trustAnchor;

        }

        #endregion


        #region 1. AlphaOrg alone: its keys, its key statement, `OCC` and `QA`, revision 0

        var alpha                 = NewShareholder("AlphaOrg", COSEKeyPair.NewES512(), "alpha.example");

        // The key statement: who holds the identity keys of OCC in QA, and how many of the holders decide. One holder, one
        // deciding, signed by its identity key.
        var keyStatementAlpha     = alpha.StateKeys(
                                        qaId,
                                        occId,
                                        IdentityKeys:  [ alpha.IdentityPublicKey ],
                                        MinConsensus:  1,
                                        SignedAt:      now,
                                        ValidUntil:    now.AddYears(1)
                                    );

        // AlphaOrg certifies its own signing key with its identity key, alone: renewing a signing key later needs no one
        // else, as it gives its shareholder one voice only.
        var occ                   = new RoamingNetworkOperator(
                                        keyStatementAlpha,
                                        Name:                    I18NString.Create("Open Charging Cloud"),
                                        SigningKeyCertificates:  [ alpha.CertifySigningKey(occId, now, NotAfter: now.AddYears(1)) ]
                                    );

        // QA names where it is served: so far at AlphaOrg only.
        var qa                    = occ.CreateRoamingNetwork(
                                        qaId,
                                        Name:          ImmutableI18NString.Create("Quality Assurance"),
                                        Description:   ImmutableI18NString.Create("The roaming network of the examples"),
                                        URLs:          [ alpha.URL!.Value ]
                                    );

        // The genesis commit, signed by the signing key of AlphaOrg, as the roaming network operator. The store stays open
        // for the examples building on this one: the result closes it.
        var store                 = new RoamingNetworkHistoryFileStore($"poi/{qa.Id}");
        var history               = RoamingNetworkHistory.Start(
                                        qa,
                                        Signers:  [ alpha.CommitSignerFor(occId) ],
                                        Store:    store
                                    );

        Publish(history, "AlphaOrg alone");

        // Where a server or a witness following QA from its start begins: the genesis version with its commit.
        File.WriteAllText($"published/{qa.Id}-genesis.json", history.ToSnapshotJSON(IncludeRuntimeStatus: false).ToString());

        #endregion

        #region 2. BetaOrg joins: two of two, revision 1

        // BetaOrg read revision 0 at AlphaOrg and checked it against the trust anchor of AlphaOrg, as section 8 does.
        // It makes its own keys, sets up its own copy of QA, and asks to hold the keys of OCC with AlphaOrg.
        var beta                  = NewShareholder("BetaOrg", COSEKeyPair.NewEd448(), "beta.example");

        // The key statement of two holders, both deciding: AlphaOrg, the quorum so far, states it, BetaOrg signs it too.
        var keyStatementBeta      = beta.CoSign(
                                        alpha.StateKeys(
                                            qaId,
                                            occId,
                                            IdentityKeys:  [ alpha.IdentityPublicKey, beta.IdentityPublicKey ],
                                            MinConsensus:  minConsensus,
                                            SignedAt:      Timestamp.Now,
                                            ValidUntil:    now.AddYears(1)
                                        )
                                    );

        // One change set admits BetaOrg: its identity key, the key statement, the certificate of its signing key and the
        // quorum of OCC, and the URL of its copy of QA. Between its changes the quorum fits the keys of no version: the
        // version after all of them is the one checked.
        var betaJoins             = history.Head.RoamingNetwork.Admit(
                                        beta,
                                        keyStatementBeta,
                                        SigningKeyNotAfter:  now.AddYears(1),
                                        Description:         ImmutableI18NString.Create(Languages.en, "BetaOrg joins AlphaOrg: two of two")
                                    );

        // AlphaOrg signs it: with its identity key, as the one holder so far decides who holds the keys, and with its
        // signing key, as the key statement and the URLs are content of OCC and QA.
        betaJoins                 = alpha.SignWithSigningKey(alpha.SignWithIdentityKey(betaJoins));

        // Without BetaOrg it is refused: a new identity key signs the change set stating it, as the proof that its
        // holder has it, and asked for it.
        var withoutBetaOrg        = history.Commit(betaJoins);

        Console.WriteLine($"BetaOrg joins, signed by AlphaOrg only: {(withoutBetaOrg.IsCommitted ? "committed" : withoutBetaOrg.ErrorResponse)}");
        Console.WriteLine();

        // BetaOrg downloads the change set at AlphaOrg, checks what it states, signs it with its identity key and sends
        // its signature back. From now on, every commit is signed by both.
        betaJoins                 = beta.SignWithIdentityKey(betaJoins);
        history.Signers           = [ alpha.CommitSignerFor(occId), beta.CommitSignerFor(occId) ];

        var betaJoined            = history.Commit(betaJoins);

        if (!betaJoined.IsCommitted)
            Helpers.Fail(betaJoined.ErrorResponse ?? "BetaOrg did not join");

        Publish(history, "BetaOrg joined, two of two decide");

        #endregion

        #region 3. GammaOrg joins: two of three, revision 2

        var gamma                 = NewShareholder("GammaOrg", COSEKeyPair.NewMLDsa65(), "gamma.example");

        // The key statement of three holders, two deciding, signed by the quorum so far: AlphaOrg and BetaOrg.
        var keyStatementGamma     = beta.CoSign(
                                        alpha.StateKeys(
                                            qaId,
                                            occId,
                                            IdentityKeys:  [ alpha.IdentityPublicKey, beta.IdentityPublicKey, gamma.IdentityPublicKey ],
                                            MinConsensus:  minConsensus,
                                            SignedAt:      Timestamp.Now,
                                            ValidUntil:    now.AddYears(1)
                                        )
                                    );

        // The quorum stays two: of three now, so the change set admitting GammaOrg does not change minConsensus.
        var gammaJoins            = history.Head.RoamingNetwork.Admit(
                                        gamma,
                                        keyStatementGamma,
                                        SigningKeyNotAfter:  now.AddYears(1),
                                        Description:         ImmutableI18NString.Create(Languages.en, "GammaOrg joins AlphaOrg and BetaOrg: two of three")
                                    );

        // GammaOrg signs with its identity key, the proof that it holds it, and AlphaOrg alone of the holders so far:
        // refused, as two of them decide.
        gammaJoins                = gamma.SignWithIdentityKey(gammaJoins);
        gammaJoins                = alpha.SignWithSigningKey(alpha.SignWithIdentityKey(gammaJoins));

        var withoutBetaOrgAgain   = history.Commit(gammaJoins);

        Console.WriteLine($"GammaOrg joins, signed by GammaOrg and AlphaOrg only: {(withoutBetaOrgAgain.IsCommitted ? "committed" : withoutBetaOrgAgain.ErrorResponse)}");
        Console.WriteLine();

        // BetaOrg signs as well, with its identity key and its signing key: the quorum so far. Every commit is signed by
        // all three from now on; two of them would be enough.
        gammaJoins                = beta.SignWithSigningKey(beta.SignWithIdentityKey(gammaJoins));
        history.Signers           = [ alpha.CommitSignerFor(occId), beta.CommitSignerFor(occId), gamma.CommitSignerFor(occId) ];

        var gammaJoined           = history.Commit(gammaJoins);

        if (!gammaJoined.IsCommitted)
            Helpers.Fail(gammaJoined.ErrorResponse ?? "GammaOrg did not join");

        var trustAnchor           = Publish(history, "GammaOrg joined, two of three decide");

        #endregion

        #region 4. What is published of the head, and the key statements one after the other

        // The head, where the examples after this one go on: the roaming network with its ETags, the commit leading to
        // it, and its trust anchor, e.g. as a QR code on the websites of the three organizations.
        File.WriteAllText($"published/{qa.Id}.json",             history.Head.RoamingNetwork.ToJSON(IncludeETags: true).ToString());
        File.WriteAllText($"published/{qa.Id}-commit.json",      history.Head.Commit.        ToJSON().                  ToString());
        File.WriteAllText($"published/{qa.Id}-trustAnchor.txt",  trustAnchor.ToQRCode());

        // The key statements of OCC one after the other, as GET ~/RNs/QA/keyStatements gives them: whoever pinned an
        // older trust anchor follows them to the keys of today.
        File.WriteAllText($"published/{qa.Id}-keyStatements.json",
                          new JObject(new JProperty(POI_API.PropertyKeys.KeyStatements,
                                                    new JArray(history.KeyStatements().Select(keyStatement => keyStatement.ToJSON(Embedded: true))))).ToString());

        Console.WriteLine(trustAnchor.ToJSON());
        Console.WriteLine($"QR code: {trustAnchor.ToQRCode().Length} characters, {trustAnchor.ToCBOR().ToByteArray(CBORWriterOptions.Canonical).Length} bytes of CBOR");
        Console.WriteLine(trustAnchor.ToQRCode());
        Console.WriteLine(trustAnchor.ToURL());
        Console.WriteLine();

        #endregion


        return new (alpha,
                    beta,
                    gamma,
                    history.Signers,
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

        #region 5. Checking `QA`, as a charging station operator or an app, from the QR code and what it read

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

        //    The holders are counted as the trust anchor names them, and the key statement must name them so.
        if (!statement.TryVerify(anchor.Holders,
                                 network.Operator!.IdentityKeys,
                                 out var holderOf,
                                 out error,
                                 anchor.MinConsensus,
                                 At: Timestamp.Now))
            Helpers.Fail(error);

        // f) The commit is signed, as the roaming network operator, by signing keys of as many holders, each certified by a
        //    trusted identity key of its holder, valid when it signed, and one that may sign for the party. The identity keys
        //    of the key statement checked in e) are trusted as well; a signature counts for the holder of the identity key
        //    that certified its key, as e) knows it, never as the signing key names itself.
        var trustedIds   = holderOf.Keys.ToHashSet();
        var identityPool = statement.IdentityKeys.Concat(network.Operator.IdentityKeys).ToArray();
        var signedBy     = new HashSet<String>();

        foreach (var signature in commit.Signatures.Where(signature => signature.Role == RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator))
        {

            var certificate = network.Operator.SigningKeyCertificates.
                                  FirstOrDefault(certificate => certificate.SigningKey.Id == signature.KeyId         &&
                                                                certificate.SigningKey.IsValidAt(signature.SignedAt) &&
                                                                certificate.SigningKey.MaySign(POIKeyUsages.Party)   &&
                                                                certificate.TryVerify(identityPool, out _, out _, trustedIds));

            if (certificate is null)
                Helpers.Fail($"the key '{signature.KeyId}' is no signing key a trusted identity key of its holder certifies");

            if (!commit.VerifySignature(signature, certificate.SigningKey.Key, certificate.SigningKey.Id, out error))
                Helpers.Fail(error);

            signedBy.Add(holderOf[certificate.KeyId]);

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


    /// <summary>
    /// Checks how somebody who pinned `QA` when AlphaOrg held its keys alone gets to the keys of today: from what is
    /// published only, the QR code of revision 0 and the key statements of `OCC` one after the other.
    /// </summary>
    public static void ValidateSuccession()
    {

        #region 6. From the trust anchor of revision 0 to the keys of today

        // The trust anchor as it was scanned when AlphaOrg was alone, and the one of today, to compare with.
        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor-revision0.txt"), out var pinned, out var error))
            Helpers.Fail(error);

        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor.txt"),           out var today,  out error))
            Helpers.Fail(error);

        // The key statements, the oldest first, as GET ~/RNs/QA/keyStatements gives them.
        var keyStatements = new List<RoamingNetworkKeyStatement>();

        foreach (var keyStatementJSON in (JArray) Helpers.ReadJSON("published/QA-keyStatements.json")[POI_API.PropertyKeys.KeyStatements]!)
        {

            if (!RoamingNetworkKeyStatement.TryParse((JObject) keyStatementJSON, out var keyStatement, out error))
                Helpers.Fail(error);

            keyStatements.Add(keyStatement);

        }

        Console.WriteLine($"Pinned at revision 0: {pinned.Holders.Count()} holder, minConsensus {pinned.MinConsensus}; today: {today.Holders.Count()} holders, minConsensus {today.MinConsensus}; {keyStatements.Count} key statements");

        // The key statement of today alone is not signed by enough holders the old trust anchor knows: two decide today,
        // and it knows one.
        Console.WriteLine($"The key statement of today alone: {(keyStatements[^1].TryVerify(pinned.Holders, null, out _, out error, pinned.MinConsensus, At: Timestamp.Now) ? "accepted" : error)}");

        // One after the other: each signed by the quorum of the one before, among the keys that one names, and by its own.
        if (!RoamingNetworkKeyStatement.TryVerifyChain(keyStatements,
                                                       pinned.Holders,
                                                       out var verified,
                                                       out var holderOf,
                                                       out error,
                                                       pinned.MinConsensus,
                                                       At: Timestamp.Now))
            Helpers.Fail(error);

        Console.WriteLine($"One after the other: the keys of {String.Join(", ", holderOf.Values.Distinct().Order())}, minConsensus {verified.MinConsensus}");

        // Those are the keys of the trust anchor of today, per holder.
        var followed = verified.Holders.Select(keys => String.Join(";", keys.Order())).Order().ToArray();
        var current  = today.   Holders.Select(keys => String.Join(";", keys.Order())).Order().ToArray();

        if (!followed.SequenceEqual(current) || verified.MinConsensus != today.MinConsensus)
            Helpers.Fail("the key statements do not lead to the trust anchor of today");

        Console.WriteLine("They are the keys and the quorum of the trust anchor of today.");

        #endregion

    }

}
