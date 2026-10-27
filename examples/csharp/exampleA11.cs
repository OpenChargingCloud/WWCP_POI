using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA11
{

    // Cryptography ages: QA moves into epoch 2, of SHA-512 identifications and of the post-quantum ML-DSA signatures only.
    // New locks first, next to the old ones, still in epoch 1: the holders of OCC, DE*GEF and the witnesses make ML-DSA
    // keys. The final checkpoint 6 closes epoch 1 and names epoch 2, whose genesis is the version of that checkpoint; the
    // first commit of epoch 2 takes the old locks out. Its first checkpoint is signed with the old and the new keys, so a
    // verifier trusting only the old ones follows into epoch 2. Epoch 1 stays readable: the driver's evidence of example
    // A10 proves what it proved.


    /// <summary>
    /// The profiles of epoch 2: SHA-512 identifications, ML-DSA-65 and ML-DSA-87 signatures only.
    /// </summary>
    internal static readonly RoamingNetworkEpochProfiles PostQuantum = new (ETagHashAlgorithm.SHA512, [ "ML-DSA-65", "ML-DSA-87" ]);


    /// <summary>
    /// What example A11 leaves to the examples building on it: the post-quantum keys of the organizations, of DE*GEF and of
    /// the witnesses, the new trust anchor of `QA`, and its history in epoch 2. Disposing it closes the store of the history.
    /// </summary>
    /// <param name="IdentityKeys">The ML-DSA identity key of each organization.</param>
    /// <param name="SigningKeys">The ML-DSA signing key of each organization.</param>
    /// <param name="Signers">The signers of commits as the roaming network operator, one of each organization.</param>
    /// <param name="GEF">The ML-DSA keys of DE*GEF.</param>
    /// <param name="Witnesses">The ML-DSA keys of the witnesses, their copies of the history of `QA` in epoch 2, and their journals, kept since epoch 1.</param>
    /// <param name="TrustAnchor">The trust anchor of `QA` naming the ML-DSA identity keys.</param>
    /// <param name="History">The history of `QA` in epoch 2.</param>
    /// <param name="Store">The store of the history, in poi/QA-epoch-2.</param>
    public sealed record Result(IReadOnlyDictionary<String, COSEKeyPair>   IdentityKeys,
                                IReadOnlyDictionary<String, COSEKeyPair>   SigningKeys,
                                IReadOnlyList<RoamingNetworkCommitSigner>  Signers,
                                ExampleA2.Result                           GEF,
                                ExampleA6.Result                           Witnesses,
                                RoamingNetworkTrustAnchor                  TrustAnchor,
                                RoamingNetworkHistory                      History,
                                RoamingNetworkHistoryFileStore             Store) : IDisposable
    {

        public void Dispose()
            => Store.Dispose();

    }


    public static Result Run(ExampleA1.Result A1, ExampleA2.Result A2, ExampleA6.Result A6)
    {

        var now           = Timestamp.Now;
        var occ           = A1.RoamingNetwork.Operator!;
        var checkpoint5   = A1.History.Checkpoints[5];

        #region 1. New locks next to the old ones: ML-DSA keys

        // AlphaOrg and BetaOrg make a new identity key, ML-DSA-87; the one of GammaOrg is ML-DSA-65 already. Each makes a new
        // signing key, ML-DSA-65, and certifies it with its post-quantum identity key, alone, as in example A1.
        var identityKeys  = new Dictionary<String, COSEKeyPair> {
                                { "AlphaOrg", COSEKeyPair.NewMLDsa87() },
                                { "BetaOrg",  COSEKeyPair.NewMLDsa87() },
                                { "GammaOrg", A1.IdentityKeys["GammaOrg"] }
                            };

        var signingKeys   = identityKeys.Keys.ToDictionary(organization => organization,
                                                           organization => COSEKeyPair.NewMLDsa65());

        foreach (var organization in identityKeys.Keys)
        {

            if (organization != "GammaOrg")
                Helpers.SaveKey(identityKeys[organization].Private, $"{organization}/identity-mldsa-privateKey.pem");

            Helpers.SaveKey(signingKeys[organization].Private, $"{organization}/signing-mldsa-privateKey.pem");

        }

        var newIdentityKeys  = identityKeys.Where (identityKey => identityKey.Key != "GammaOrg").
                                            Select(identityKey => POIPublicKey.From(identityKey.Value.Private, Holder: identityKey.Key)).
                                            ToArray();

        var newCertificates  = identityKeys.Keys.Select(organization => RoamingNetworkSigningKeyCertificate.Create(
                                                                            identityKeys[organization],
                                                                            occ.Id,
                                                                            POIPublicKey.From(signingKeys[organization].Private,
                                                                                              NotAfter:  now.AddYears(1),
                                                                                              Holder:    organization,
                                                                                              Usages:    [ POIKeyUsages.Party ]),
                                                                            now
                                                                        )).ToArray();

        // The witnesses make ML-DSA-65 keys as well; trusted by configuration, as their first ones.
        var witnessKeys   = ExampleA6.Witnesses.ToDictionary(witness => witness,
                                                             witness => COSEKeyPair.NewMLDsa65());

        File.WriteAllText("trusted/witnesses-mldsa.json",
                          new JArray(witnessKeys.Select(witness => new JObject(new JProperty("witness", witness.Key),
                                                                               new JProperty("key",     POIPublicKey.From(witness.Value.Private).ToJSON())))).ToString());

        #endregion

        #region 2. OCC states its new keys, next to the old ones

        // Its identity keys, the old and the new ones; a key statement naming all of them, signed by the old identity keys of
        // AlphaOrg and BetaOrg, so that a verifier trusting the old trust anchor trusts the new keys as well; and the
        // certificates of the old and the new signing keys. The quorum changes its keys with identity keys, its key
        // statement, as its data, with signing keys; every new identity key signs as the proof that its holder holds it.
        var allIdentityKeys  = occ.IdentityKeys.Concat(newIdentityKeys).ToArray();

        var bridgeStatement  = RoamingNetworkKeyStatement.Create(A1.IdentityKeys["AlphaOrg"],
                                                                 ExampleA1.minConsensus,
                                                                 A1.RoamingNetwork.Id,
                                                                 occ.Id,
                                                                 IdentityKeys:  allIdentityKeys,
                                                                 SignedAt:      now,
                                                                 ValidUntil:    now.AddYears(1)).
                                   WithSignature(A1.IdentityKeys["BetaOrg"]);

        var addKeys          = A1.RoamingNetwork.CreateChangeSet(
                                   [
                                       RoamingNetworkChange.UpdateProperty(occ, POIPartyKeys.IdentityKeysProperty,                null, Keys(allIdentityKeys)),
                                       RoamingNetworkChange.UpdateProperty(occ, RoamingNetworkKeyStatement.JSONProperty,          null, bridgeStatement.ToJSON(Embedded: true)),
                                       RoamingNetworkChange.UpdateProperty(occ, RoamingNetworkSigningKeyCertificate.JSONProperty, null, Certificates(occ.SigningKeyCertificates.Concat(newCertificates)))
                                   ],
                                   Description: ImmutableI18NString.Create(Languages.en, "OCC: ML-DSA keys next to the old ones")
                               );

        foreach (var key in new[] { A1.IdentityKeys["AlphaOrg"], A1.IdentityKeys["BetaOrg"], identityKeys["AlphaOrg"], identityKeys["BetaOrg"], A1.SigningKeys["AlphaOrg"], A1.SigningKeys["BetaOrg"] })
            addKeys = addKeys.Sign(key.Private, POIPublicKey.From(key.Private).Id, key.Algorithm);

        Committed(A1.History, addKeys);

        // Signed by the new signing keys from now on; the final checkpoint by the old ones as well.
        RoamingNetworkCommitSigner[] signers = [ .. identityKeys.Keys.Select(organization => new RoamingNetworkCommitSigner(
                                                                                                   signingKeys[organization].Private,
                                                                                                   POIPublicKey.From(signingKeys[organization].Private).Id,
                                                                                                   signingKeys[organization].Algorithm,
                                                                                                   RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                                                                                   occ.Id.ToString())) ];

        A1.History.Signers             = [ .. signers ];
        A1.History.AttestationSigners  = [ .. signers ];
        A1.History.CheckpointSigners   = [ .. A1.Signers, .. signers ];

        #endregion

        #region 3. DE*GEF replaces its keys

        // Two identity keys of two strengths, and a signing key for each usage, all ML-DSA. Its keys are its own: signed by
        // one of its identity keys, and by each new one as its proof. In epoch 1 its new keys sign at once.
        var gefIdentityKeys  = new[] {
                                   COSEKeyPair.NewMLDsa65(),
                                   COSEKeyPair.NewMLDsa87()
                               };
        var gefSigningKeys   = new Dictionary<String, COSEKeyPair> {
                                   { POIKeyUsages.Infrastructure, COSEKeyPair.NewMLDsa65() },
                                   { POIKeyUsages.Tariffs,        COSEKeyPair.NewMLDsa65() },
                                   { POIKeyUsages.Party,          COSEKeyPair.NewMLDsa65() }
                               };

        for (var number = 1; number <= gefIdentityKeys.Length; number++)
            Helpers.SaveKey(gefIdentityKeys[number - 1].Private, $"DE-GEF/identity{number}-mldsa-privateKey.pem");

        foreach (var usage in gefSigningKeys.Keys)
            Helpers.SaveKey(gefSigningKeys[usage].Private, $"DE-GEF/signing-{usage}-mldsa-privateKey.pem");

        if (!A1.History.Head.RoamingNetwork.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        var replaceKeys      = A1.History.Head.RoamingNetwork.CreateChangeSet(
                                   [
                                       RoamingNetworkChange.UpdateProperty(gef, POIPartyKeys.IdentityKeysProperty, null, Keys(gefIdentityKeys.Select(identityKey => POIPublicKey.From(identityKey.Private)))),
                                       RoamingNetworkChange.UpdateProperty(gef, POIPartyKeys.SigningKeysProperty,  null, Keys([
                                           POIPublicKey.From(gefSigningKeys[POIKeyUsages.Infrastructure].Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Infrastructure ]),
                                           POIPublicKey.From(gefSigningKeys[POIKeyUsages.Tariffs].       Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Tariffs, POIKeyUsages.PriceStatements ]),
                                           POIPublicKey.From(gefSigningKeys[POIKeyUsages.Party].         Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Party ])
                                       ]))
                                   ],
                                   Description: ImmutableI18NString.Create(Languages.en, "DE*GEF: ML-DSA keys in place of its old ones")
                               );

        foreach (var key in new[] { A2.IdentityKeys[0], gefIdentityKeys[0], gefIdentityKeys[1] })
            replaceKeys = replaceKeys.Sign(key.Private, POIPublicKey.From(key.Private).Id, key.Algorithm);

        Committed(A1.History, replaceKeys);

        Console.WriteLine();

        #endregion

        #region 4. The final checkpoint closes epoch 1

        // Checkpoint 6, of the head, names epoch 2 and its profiles; signed by the old and the new keys of OCC.
        if (!A1.History.TryCloseEpoch(PostQuantum, out var final, out var error))
            Helpers.Fail(error);

        PublishCheckpoint(A1.History, final, "published/QA-checkpoint-6");
        ExampleA4.PublishCommitsSince(A1, checkpoint5);
        ExampleA4.Publish(A1);

        Console.WriteLine($"Checkpoint {final.Sequence}, final, of revision {final.Revision}: {final.CommitsSize} commit(s), names epoch {final.Next!.Number}, {final.Next.Profiles.IdentificationHash.ToText()}, {String.Join(", ", final.Next.Profiles.SignatureAlgorithms)}");
        Console.WriteLine($"Epoch {A1.History.Epoch.Number}: {A1.History.EpochStatus}; a commit now: {A1.History.Commit(A1.History.Head.RoamingNetwork.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(A1.History.Head.RoamingNetwork, "dataSource", null, "too late") ])).ErrorResponse}");

        // The witnesses take the two commits and the final checkpoint and sign it with their old keys, as every checkpoint of
        // epoch 1.
        var signatures = new List<(RoamingNetworkCheckpoint_Id CheckpointId, RoamingNetworkCommitSignature Signature)>();

        foreach (var (witness, history) in A6.Histories)
        {

            ExampleA6.AppendCommits(history, "published/QA-commits-since-checkpoint-5.json");

            if (!ExampleA6.TryTakeCheckpoint(history, final.Sequence, out var taken, out error))
                Helpers.Fail(error);

            if (!history.TryWitness(taken.Id, ExampleA6.WitnessOf(A6, witness, A6.Keys[witness]), out _, out var signature, out error))
                Helpers.Fail(error);

            signatures.Add((taken.Id, signature));

        }

        ExampleA9.TakeSignatures(A1, signatures);

        Console.WriteLine();

        #endregion

        #region 5. Epoch 2 starts from the version of the final checkpoint

        // Nothing is applied: the genesis of epoch 2 is the version of checkpoint 6, identified with SHA-512, signed by the
        // ML-DSA keys of OCC. Every server starting epoch 2 from checkpoint 6 makes the same genesis; a server serves it in
        // place of epoch 1 at the same URL (TryServeNextEpoch), and epoch 1 stays readable.
        var store = new RoamingNetworkHistoryFileStore("poi/QA-epoch-2");

        if (!A1.History.TryStartNextEpoch(out var epoch2, out error, Signers: signers, Store: store))
            Helpers.Fail(error);

        epoch2.AttestationSigners = [ .. signers ];

        // The version of its genesis certifies the old signing keys still: they sign its first checkpoint.
        var genesisVersion = epoch2.Head.RoamingNetwork;

        File.WriteAllText("published/QA-epoch-2-genesis.json", epoch2.ToSnapshotJSON(IncludeRuntimeStatus: false).ToString());

        Console.WriteLine($"Epoch {epoch2.Epoch.Number}, genesis {epoch2.Root.Id.ToString().Length} hexadecimal digits: {epoch2.Root.Id}");
        Console.WriteLine($"  revision {epoch2.Root.Revision}, the ETags of checkpoint {final.Sequence}: {(epoch2.Root.ETags.SequenceEqual(final.ETags) ? "the same" : "others")}; epoch 1: {A1.History.EpochStatus}");

        // Of the current price statements, only those signed with an algorithm of epoch 2 are taken along; the chains of the
        // others go on there (7).
        var notTakenAlong = A1.History.PriceStatements.Where(statement => !epoch2.Epoch.Profiles.AllowsSignatureAlgorithm(statement.Algorithm)).ToArray();

        Console.WriteLine($"  price statements taken along: {epoch2.PriceStatements.Count} of {A1.History.PriceStatements.Count}; not those signed with {String.Join(", ", notTakenAlong.Select(statement => statement.Algorithm).Distinct())}");

        // Each witness starts epoch 2 from its own copy, holding checkpoint 6: the same genesis.
        var witnessHistories = new Dictionary<String, RoamingNetworkHistory>();
        var witnessGenesis   = new Dictionary<String, RoamingNetwork>();

        foreach (var (witness, history) in A6.Histories)
        {

            if (!history.TryStartNextEpoch(out var next, out error))
                Helpers.Fail(error);

            witnessHistories.Add(witness, next);
            witnessGenesis.  Add(witness, next.Head.RoamingNetwork);

            Console.WriteLine($"  {witness}: genesis {next.Root.Id.ToString()[..8]}…, {(next.Root.Id == epoch2.Root.Id ? "the same" : "another one")}");

        }

        // Epoch 1 still proves its commits, e.g. the one of the driver's station proof of example A10, in checkpoint 4.
        var evidence = Helpers.ReadJSON(ExampleA10.Evidence);

        if (!RoamingNetworkCheckpointProof.TryParse((JObject) evidence["commitProof"]!, out var driversProof, out error))
            Helpers.Fail(error);

        Console.WriteLine($"Epoch 1, revision {driversProof.Revision}: {(A1.History.TryGetCommitProof(driversProof.CommitId, out var again, out error) ? $"proved in checkpoint {A1.History.Checkpoints.Single(checkpoint => checkpoint.Id == again.CheckpointId).Sequence}, {(again.ToJSON().ToString() == driversProof.ToJSON().ToString() ? "as the driver keeps it" : "otherwise")}" : error)}");
        Console.WriteLine();

        #endregion

        #region 6. The first commit of epoch 2 takes the old locks out

        // OCC keeps only its ML-DSA identity keys, a key statement of them, signed by the new identity keys of AlphaOrg and
        // BetaOrg, and the certificates of its ML-DSA signing keys: signed by those identity keys and their signing keys, as
        // the quorum.
        var pqStatement  = RoamingNetworkKeyStatement.Create(identityKeys["AlphaOrg"],
                                                             ExampleA1.minConsensus,
                                                             epoch2.RoamingNetworkId,
                                                             occ.Id,
                                                             IdentityKeys:  [ .. allIdentityKeys.Where(identityKey => identityKeys.Values.Any(pair => POIPublicKey.From(pair.Private).Id == identityKey.Id)) ],
                                                             SignedAt:      now,
                                                             ValidUntil:    now.AddYears(1)).
                               WithSignature(identityKeys["BetaOrg"]);

        var occ2         = epoch2.Head.RoamingNetwork.Operator!;
        var removeKeys   = epoch2.Head.RoamingNetwork.CreateChangeSet(
                               [
                                   RoamingNetworkChange.UpdateProperty(occ2, POIPartyKeys.IdentityKeysProperty,                null, Keys(pqStatement.IdentityKeys)),
                                   RoamingNetworkChange.UpdateProperty(occ2, RoamingNetworkKeyStatement.JSONProperty,          null, pqStatement.ToJSON(Embedded: true)),
                                   RoamingNetworkChange.UpdateProperty(occ2, RoamingNetworkSigningKeyCertificate.JSONProperty, null, Certificates(newCertificates))
                               ],
                               Description: ImmutableI18NString.Create(Languages.en, "OCC: the old keys out")
                           );

        // By the old identity keys, as in epoch 1: refused, ES512 and Ed448 are no algorithms of epoch 2.
        var byOldKeys = removeKeys;

        foreach (var key in new[] { A1.IdentityKeys["AlphaOrg"], A1.IdentityKeys["BetaOrg"], A1.SigningKeys["AlphaOrg"], A1.SigningKeys["BetaOrg"] })
            byOldKeys = byOldKeys.Sign(key.Private, POIPublicKey.From(key.Private).Id, key.Algorithm);

        Console.WriteLine($"Signed by the old identity keys: {epoch2.Commit(byOldKeys).ErrorResponse}");

        foreach (var key in new[] { identityKeys["AlphaOrg"], identityKeys["BetaOrg"], signingKeys["AlphaOrg"], signingKeys["BetaOrg"] })
            removeKeys = removeKeys.Sign(key.Private, POIPublicKey.From(key.Private).Id, key.Algorithm);

        Committed(epoch2, removeKeys);

        // The new trust anchor names the ML-DSA identity keys: the QR code on the website of OCC is replaced.
        var trustAnchor  = epoch2.Head.RoamingNetwork.CreateTrustAnchor(pqStatement, A1.TrustAnchor.URLs);

        File.WriteAllText("published/QA-trustAnchor-mldsa.txt", trustAnchor.ToQRCode());

        Console.WriteLine($"The new trust anchor: {trustAnchor.Keys.Length} keys, minConsensus {trustAnchor.MinConsensus}, QR code of {trustAnchor.ToQRCode().Length} characters");

        #endregion

        #region 7. DE*GEF in epoch 2

        // Its DC tariff lowered, signed by its ML-DSA key for its tariffs; the same by its old key is refused.
        if (!epoch2.Head.RoamingNetwork.TryGetChargingStationOperatorById(ExampleA2.gefId, out gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        var lower         = epoch2.Head.RoamingNetwork.CreateChangeSet(
                                [ RoamingNetworkChange.UpdateProperty(gef.ChargingTariffs.Single(tariff => tariff.Id == ExampleA8.DC), "elements", null, new JArray(new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.69m, StepSize: 1) ]).ToJSON())) ],
                                Description: ImmutableI18NString.Create(Languages.en, "The DC charging tariff: 0.69 €/kWh")
                            );

        var oldTariffsKey = A2.SigningKeys[POIKeyUsages.Tariffs];
        var tariffsKey    = gefSigningKeys[POIKeyUsages.Tariffs];

        Console.WriteLine($"Signed by its old tariffs key: {epoch2.Commit(lower.Sign(oldTariffsKey.Private, POIPublicKey.From(oldTariffsKey.Private).Id, oldTariffsKey.Algorithm)).ErrorResponse}");

        Committed(epoch2, lower.Sign(tariffsKey.Private, POIPublicKey.From(tariffsKey.Private).Id, tariffsKey.Algorithm));

        // Its chain of price statements of DE*GEF*E1*1 goes on: the statement of example A10, signed with ES256, was not taken
        // along into epoch 2, the next one names it all the same, the last one of epoch 1, and the final checkpoint, the
        // latest DE*GEF knows. No server checks it against that one, as none holds it as the current one in epoch 2.
        var previous      = A1.History.PriceStatementsOf(ExampleA10.EVSEId).Single(statement => statement.IsOf(nameof(EVSE), ExampleA10.EVSEId.ToString()));
        var at            = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.Now.ToUnixTimeMilliseconds());

        RoamingNetworkPriceStatement StatementBy(COSEKeyPair Key)

            => RoamingNetworkPriceStatement.Create(Key.Private,
                                                   POIPublicKey.From(Key.Private).Id,
                                                   Key.Algorithm,
                                                   epoch2.RoamingNetworkId,
                                                   nameof(EVSE),
                                                   ExampleA10.EVSEId.ToString(),
                                                   ExampleA8.Prices(ExampleA8.DC, 0.59m),
                                                   previous.Sequence + 1,
                                                   previous.Digest,
                                                   SignedAt:    at,
                                                   ValidFrom:   at,
                                                   ValidUntil:  at.AddHours(1),
                                                   After:       final.Id);

        epoch2.TryAddPriceStatement(StatementBy(oldTariffsKey), out error);

        Console.WriteLine($"A price statement by its old tariffs key: {error}");

        var statement     = StatementBy(tariffsKey);

        Console.WriteLine($"{ExampleA10.EVSEId}, 0.59 €/kWh, sequence {statement.Sequence}, {statement.Algorithm}: {epoch2.TryAddPriceStatement(statement, out error)}{(error is null ? "" : $", {error}")}");
        Console.WriteLine();

        #endregion

        #region 8. The first checkpoint of epoch 2, with the old and the new locks

        // Checkpoint 0 of epoch 2 names the final checkpoint of epoch 1 and is signed by the old and the new keys of OCC, the
        // old ones as the genesis certifies them: a verifier trusting only those of epoch 1 follows.
        epoch2.CheckpointSigners = [ .. signers, .. A1.Signers ];

        if (!epoch2.TryMakeCheckpoint(out var first, out error))
            Helpers.Fail(error);

        PublishCheckpoint(epoch2, first, "published/QA-epoch-2-checkpoint-0");

        if (!epoch2.TryGetCommitsSince(epoch2.Root.Id, out var commits, out error))
            Helpers.Fail(error);

        File.WriteAllText("published/QA-epoch-2-commits-since-genesis.json",       new JArray(commits.Select(commit => commit.ToJSON())).ToString());
        File.WriteAllText("published/QA-epoch-2-checkpoint-0-priceStatements.json", new JArray((epoch2.PriceStatementsOf(first.Id) ?? []).Select(named => named.ToJSON())).ToString());
        File.WriteAllText($"published/{epoch2.RoamingNetworkId}.json",              epoch2.Head.RoamingNetwork.ToJSON(IncludeETags: true).ToString());
        File.WriteAllText($"published/{epoch2.RoamingNetworkId}-commit.json",       epoch2.Head.Commit.ToJSON().ToString());

        Console.WriteLine($"Checkpoint {first.Sequence} of epoch {first.Epoch}, of revision {first.Revision}: {first.CommitsSize} commit(s), {first.PriceStatementsSize} price statement(s), after checkpoint {final.Sequence} of epoch {first.PreviousEpoch!.Epoch}, signed with {String.Join(", ", first.Signatures.Select(signature => signature.Algorithm).Distinct())}");

        // The witnesses take the commits and the checkpoint with its statement, and sign it with their old and their new keys.
        var trusted = new[] { ExampleA6.TrustedWitnesses(), TrustedWitnesses() };

        signatures.Clear();

        foreach (var (witness, history) in witnessHistories)
        {

            ExampleA6.AppendCommits(history, "published/QA-epoch-2-commits-since-genesis.json");

            if (!RoamingNetworkCheckpoint.TryParse(Unwitnessed(Helpers.ReadJSON("published/QA-epoch-2-checkpoint-0.json")), out var taken, out error) ||
                !history.TryAddCheckpoint(taken,
                                          out error,
                                          Verify(ExampleA6Anchor(), [ witnessGenesis[witness], history.Head.RoamingNetwork ]),
                                          [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ],
                                          ExampleA9.Statements("published/QA-epoch-2-checkpoint-0-priceStatements.json")))
                Helpers.Fail(error!);

            foreach (var key in new[] { A6.Keys[witness], witnessKeys[witness] })
            {

                if (!history.TryWitness(taken.Id, ExampleA6.WitnessOf(A6, witness, key), out _, out var signature, out error))
                    Helpers.Fail(error);

                signatures.Add((taken.Id, signature));

            }

            Console.WriteLine($"{witness}: revision {history.Head.Commit.Revision} of epoch {history.Epoch.Number}, signs checkpoint {taken.Sequence} with {A6.Keys[witness].Algorithm} and {witnessKeys[witness].Algorithm}");

        }

        if (!epoch2.TryAddCheckpointSignatures(first.Id, signatures.Select(signature => signature.Signature), Verify(A1.TrustAnchor, [ genesisVersion, epoch2.Head.RoamingNetwork ], trusted), out var signed, out _, out error))
            Helpers.Fail(error);

        PublishCheckpoint(epoch2, signed, "published/QA-epoch-2-checkpoint-0");

        Console.WriteLine($"Checkpoint {signed.Sequence} of epoch {signed.Epoch}: {signed.Signatures.Length} signatures");
        Console.WriteLine();

        #endregion

        return new (identityKeys,
                    signingKeys,
                    signers,
                    new (gefIdentityKeys, gefSigningKeys),
                    new (witnessKeys, witnessHistories, A6.Journals),
                    trustAnchor,
                    epoch2,
                    store);

    }


    /// <summary>
    /// Checks the move into epoch 2 from what is published only: as a verifier trusting only the trust anchor and the
    /// witnesses of epoch 1, and as one trusting only those of epoch 2.
    /// </summary>
    public static void Validate()
    {

        #region 9. A verifier trusting only the old locks

        // a) The trust anchor it scanned once, the witnesses it trusts since, and QA as of checkpoint 5, joined from its
        //    snapshot, signed by the quorum of OCC and both witnesses.
        var anchor     = ExampleA6Anchor();
        var witnesses  = ExampleA6.TrustedWitnesses();
        String[] roles = [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, RoamingNetworkCommitSignatureRoles.Witness ];

        var snapshot5  = Helpers.ReadJSON("published/QA-checkpoint-5-snapshot.json");

        if (!RoamingNetwork.TryParse((JObject) snapshot5["roamingNetwork"]!, out var version5, out var error))
            Helpers.Fail(error);

        if (!RoamingNetworkHistory.TryFromCheckpointSnapshot(snapshot5, out var history, out error, Verify(anchor, [ version5 ], witnesses), roles))
            Helpers.Fail(error);

        // b) The commits since, each checked in the version before: the one stating the new keys signed by the old quorum,
        //    so the new keys are trusted as well; and the final checkpoint 6, signed by OCC and both witnesses.
        ExampleA6.AppendCommits(history, "published/QA-commits-since-checkpoint-5.json");

        if (!RoamingNetworkCheckpoint.TryParse(Helpers.ReadJSON("published/QA-checkpoint-6.json"), out var final, out error) ||
            !history.TryAddCheckpoint(final, out error, Verify(anchor, [ history.Head.RoamingNetwork ], witnesses), roles))
            Helpers.Fail(error!);

        Console.WriteLine($"Old trust: revision {history.Head.Commit.Revision}, checkpoint {final.Sequence}, final, witnessed by {String.Join(", ", ExampleA6.WitnessedBy(final, witnesses))}; next: epoch {final.Next!.Number}, {final.Next.Profiles.IdentificationHash.ToText()}, {String.Join(", ", final.Next.Profiles.SignatureAlgorithms)}");

        // c) It starts epoch 2 itself: the genesis the server published.
        if (!history.TryStartNextEpoch(out var epoch2, out error))
            Helpers.Fail(error);

        if (!RoamingNetworkHistory.TryFromSnapshot(File.ReadAllText("published/QA-epoch-2-genesis.json"), out var published, out error))
            Helpers.Fail(error);

        if (epoch2.Root.Id != published.Root.Id)
            Helpers.Fail("the published genesis of epoch 2 is not the one of checkpoint 6");

        Console.WriteLine($"Old trust: epoch {epoch2.Epoch.Number} started, genesis {epoch2.Root.Id.ToString()[..8]}…, the published one");

        // d) The commits of epoch 2, and its checkpoint 0: the signatures it checks with the keys of epoch 1, of OCC as the
        //    genesis certifies them and of the witnesses it trusts; the others are no business of this verifier.
        var genesis = epoch2.Head.RoamingNetwork;

        ExampleA6.AppendCommits(epoch2, "published/QA-epoch-2-commits-since-genesis.json");

        var checkpoint0JSON = Helpers.ReadJSON("published/QA-epoch-2-checkpoint-0.json");

        if (!RoamingNetworkCheckpoint.TryParse(OnlyOf(checkpoint0JSON, sig => sig.Role != RoamingNetworkCommitSignatureRoles.Witness ||
                                                                              witnesses.Values.Any(key => key.Id == sig.KeyId)), out var first, out error) ||
            !epoch2.TryAddCheckpoint(first, out error, Verify(anchor, [ genesis, epoch2.Head.RoamingNetwork ], witnesses), roles))
            Helpers.Fail(error!);

        var oldSigners = first.Signatures.Where (signature => signature.Role == RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator && !PostQuantum.AllowsSignatureAlgorithm(signature.Algorithm)).
                                          Select(signature => ExampleA4.HolderOf(anchor, genesis, signature)).
                                          OfType<String>().
                                          Distinct().
                                          Order();

        Console.WriteLine($"Old trust: checkpoint {first.Sequence} of epoch {first.Epoch}, after checkpoint {final.Sequence} of epoch {first.PreviousEpoch!.Epoch}: {(first.PreviousEpoch.CheckpointId == final.Id ? "the final one it took" : "another one")}; with the old keys signed by {String.Join(", ", oldSigners)}, witnessed by {String.Join(", ", ExampleA6.WitnessedBy(first, witnesses))}");

        // e) The head of epoch 2 with the old trust anchor alone: its key statement is signed by the ML-DSA identity keys of
        //    AlphaOrg and BetaOrg, which the old trust anchor does not name. The history it followed trusts them; the new
        //    trust anchor names them, so it takes that one from now on.
        if (!RoamingNetwork.TryParse(Helpers.ReadJSON("published/QA.json"), out var head, out error))
            Helpers.Fail(error);

        Console.WriteLine($"The head with the old trust anchor alone: {(head.Operator!.KeyStatement!.TryVerify(anchor.Holders, head.Operator.IdentityKeys, out _, out error, anchor.MinConsensus, Timestamp.Now) ? "valid" : error)}");

        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor-mldsa.txt"), out var newAnchor, out error))
            Helpers.Fail(error);

        var followed = epoch2.Head.RoamingNetwork.Operator!.KeyStatement!;

        if (head.Id != epoch2.Head.RoamingNetwork.Id || !head.MatchesCommitETags(epoch2.Head.Commit.ETags))
            Helpers.Fail("the published head is not the one of the history it followed");

        Console.WriteLine($"The new trust anchor: {(newAnchor.Keys.Order().SequenceEqual(followed.IdentityKeys.Select(identityKey => identityKey.Id).Order()) && newAnchor.MinConsensus == followed.MinConsensus ? "the identity keys and the quorum of the key statement it followed" : "other keys")}");
        Console.WriteLine();

        #endregion

        #region 10. A verifier trusting only the new locks

        // a) The new trust anchor and the ML-DSA keys of the witnesses. It joins epoch 2 from checkpoint 0, checking only the
        //    signatures of the algorithms of epoch 2.
        var pqWitnesses = TrustedWitnesses();
        var snapshot0   = Helpers.ReadJSON("published/QA-epoch-2-checkpoint-0-snapshot.json");

        snapshot0["checkpoint"] = OnlyOf((JObject) snapshot0["checkpoint"]!, signature => PostQuantum.AllowsSignatureAlgorithm(signature.Algorithm));

        if (!RoamingNetwork.TryParse((JObject) snapshot0["roamingNetwork"]!, out var version0, out error))
            Helpers.Fail(error);

        if (!RoamingNetworkHistory.TryFromCheckpointSnapshot(snapshot0, out var joined, out error, Verify(newAnchor, [ version0 ], pqWitnesses), roles, ExampleA9.Statements("published/QA-epoch-2-checkpoint-0-priceStatements.json")))
            Helpers.Fail(error);

        var checkpoint0 = joined.Checkpoints[^1];

        Console.WriteLine($"New trust: joined epoch {joined.Epoch.Number} from checkpoint {checkpoint0.Sequence}, revision {joined.Head.Commit.Revision}, {checkpoint0.Id.ToString().Length} hexadecimal digits, signed with {String.Join(", ", checkpoint0.Signatures.Select(signature => signature.Algorithm).Distinct())}, witnessed by {String.Join(", ", ExampleA6.WitnessedBy(checkpoint0, pqWitnesses))}");

        // b) The price statement of DE*GEF*E1*1 in checkpoint 0: ML-DSA, after the final checkpoint of epoch 1.
        var statement = joined.PriceStatementsOf(checkpoint0.Id)!.Single(named => named.IsOf(nameof(EVSE), ExampleA10.EVSEId.ToString()));

        Console.WriteLine($"{statement.EntityId}, sequence {statement.Sequence}, {statement.PriceStatus.Prices[0].Elements[0].PriceComponents[0].Price.Euro()} €/kWh, {statement.Algorithm}, after checkpoint {final.Sequence} of epoch 1: {(statement.After == checkpoint0.PreviousEpoch!.CheckpointId ? "the final one" : "another one")}");

        // c) What epoch 2 refuses: a commit of epoch 1, e.g. the last one, given as one of epoch 2.
        if (!RoamingNetworkCommit.TryParse((JObject) Helpers.ReadJSONArray("published/QA-commits-since-checkpoint-5.json")[^1]!, out var ofEpoch1, out error))
            Helpers.Fail(error);

        joined.TryAppend([ ofEpoch1 ], out _, out error, VerifyCommitSignature: (commit, signature) => true);

        Console.WriteLine($"A commit of epoch 1: {error}");

        // d) A genesis of epoch 2 of another version, e.g. of another name: its ETags are not those of its content.
        var other = Helpers.ReadJSON("published/QA-epoch-2-genesis.json");
        other["roamingNetwork"]!["name"]!["en"] = "Quality Assurance, otherwise";

        Console.WriteLine($"A genesis of another version: {(RoamingNetworkHistory.TryFromSnapshot(other, out _, out error) ? "taken" : error)}");

        // e) The evidence of the driver of example A10, in epoch 1: its commit proof against checkpoint 4, as before.
        var evidence = Helpers.ReadJSON(ExampleA10.Evidence);

        if (!RoamingNetworkCheckpointProof.TryParse((JObject) evidence["commitProof"]!, out var commitProof, out error))
            Helpers.Fail(error);

        if (!RoamingNetworkCheckpoint.TryParse((JObject) evidence["checkpoints"]![0]!, out var checkpoint4, out error))
            Helpers.Fail(error);

        Console.WriteLine($"The driver's evidence of example A10, revision {commitProof.Revision} in checkpoint {checkpoint4.Sequence} of epoch {checkpoint4.Epoch}: {(commitProof.Verify(checkpoint4, out error) ? "valid" : error)}");

        #endregion

    }


    #region Helpers

    // The given keys, as POI writes them in a party.
    private static JArray Keys(IEnumerable<POIPublicKey> Keys)

        => new (Keys.Select(key => key.ToJSON(Embedded: true)));

    // The given certificates, as POI writes them in the roaming network operator.
    private static JArray Certificates(IEnumerable<RoamingNetworkSigningKeyCertificate> Certificates)

        => new (Certificates.Select(certificate => certificate.ToJSON(Embedded: true)));

    // The given change set committed to the given history.
    private static void Committed(RoamingNetworkHistory History, RoamingNetworkChangeSet ChangeSet)
    {

        var result = History.Commit(ChangeSet);

        if (!result.IsCommitted)
            Helpers.Fail(result.ErrorResponse ?? result.Status.ToString());

        Console.WriteLine($"{result.Status}, revision {result.Commit?.Revision} of epoch {History.Epoch.Number}: {ChangeSet.Description.FirstText()}");

    }

    // The given checkpoint and its snapshot, as everybody may read them.
    private static void PublishCheckpoint(RoamingNetworkHistory History, RoamingNetworkCheckpoint Checkpoint, String File)
    {

        if (!History.TryGetCheckpointSnapshotJSON(Checkpoint.Id, out var snapshot, out var error))
            Helpers.Fail(error);

        System.IO.File.WriteAllText($"{File}.json",          Checkpoint.ToJSON().ToString());
        System.IO.File.WriteAllText($"{File}-snapshot.json", snapshot.ToString());

    }

    // The trust anchor of QA the witnesses scanned, as example A6 reads it.
    private static RoamingNetworkTrustAnchor ExampleA6Anchor()
    {

        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        return anchor;

    }

    // The ML-DSA keys of the witnesses a verifier trusts, by configuration.
    private static IReadOnlyDictionary<String, POIPublicKey> TrustedWitnesses()
    {

        var witnesses = new Dictionary<String, POIPublicKey>();

        foreach (var json in Helpers.ReadJSONArray("trusted/witnesses-mldsa.json").Cast<JObject>())
        {

            if (!POIPublicKey.TryParse((JObject) json["key"]!, out var key, out var error))
                Helpers.Fail(error);

            witnesses.Add(json["witness"]!.Value<String>()!, key);

        }

        return witnesses;

    }

    // A verifier of the signatures of a checkpoint: of the roaming network operator by its signing keys in one of the given
    // versions, of a witness by one of the keys a verifier trusts for it.
    private static VerifyCheckpointSignatureDelegate Verify(RoamingNetworkTrustAnchor                            Anchor,
                                                            IEnumerable<RoamingNetwork>                          Versions,
                                                            params IReadOnlyDictionary<String, POIPublicKey>[]   Witnesses)

        => (checkpoint, signature) => signature.Role == RoamingNetworkCommitSignatureRoles.Witness
                                          ? Witnesses.Any(trusted => signature.Signer is String witness &&
                                                                     trusted.TryGetValue(witness, out var key) &&
                                                                     key.Id == signature.KeyId &&
                                                                     checkpoint.VerifySignature(signature, key.Key, key.Id, out _))
                                          : Versions.Any(version => ExampleA4.SigningKeyOf(Anchor, version, signature) is POIPublicKey key &&
                                                                    checkpoint.VerifySignature(signature, key.Key, key.Id, out _));

    // The given checkpoint JSON without the signatures of witnesses: those are checked by those trusting them.
    private static JObject Unwitnessed(JObject Checkpoint)

        => OnlyOf(Checkpoint, signature => signature.Role != RoamingNetworkCommitSignatureRoles.Witness);

    // The given checkpoint JSON with only the signatures a verifier checks.
    private static JObject OnlyOf(JObject Checkpoint, Func<RoamingNetworkCommitSignature, Boolean> Checks)
    {

        var json = (JObject) Checkpoint.DeepClone();

        foreach (var signature in ((JArray) json["signatures"]!).Cast<JObject>().ToArray())
            if (!RoamingNetworkCommitSignature.TryParse(signature, out var parsed, out _) || !Checks(parsed))
                signature.Remove();

        return json;

    }

    #endregion

}
