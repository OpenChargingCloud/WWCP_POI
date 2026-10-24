using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA2
{

    // The roaming network "QA" of example A1 gets its first party: the charging station operator "DE*GEF", and with it the
    // manufacturers of its charging stations and of their energy meters.

    public static readonly ChargingStationOperator_Id      gefId           = ChargingStationOperator_Id.Parse("DE*GEF");
    public static readonly ChargingStationManufacturer_Id  stationMakerId  = ChargingStationManufacturer_Id.Parse("GraphDefined");
    public static readonly EnergyMeterManufacturer_Id      acMeterMakerId  = EnergyMeterManufacturer_Id.Parse("SAALE");
    public static readonly EnergyMeterManufacturer_Id      dcMeterMakerId  = EnergyMeterManufacturer_Id.Parse("KERNBERG");


    /// <summary>
    /// What example A2 leaves to the examples building on it: the private keys of "DE*GEF".
    /// </summary>
    /// <param name="IdentityKeys">The identity keys of DE*GEF, which sign a change of its keys.</param>
    /// <param name="SigningKeys">The signing keys of DE*GEF, by the usage each may sign.</param>
    public sealed record Result(IReadOnlyList<COSEKeyPair>                IdentityKeys,
                                IReadOnlyDictionary<String, COSEKeyPair>  SigningKeys);


    public static Result Run(ExampleA1.Result A1)
    {

        #region 1. DE*GEF makes its keys

        // Two identity keys of two kinds, the long-term keys which sign a change of its keys: should one kind of key be
        // broken one day, the other still signs. And a signing key for each usage: its charging pools, stations and EVSEs,
        // its tariffs and the prices it signs at runtime, and the party itself, its own properties. A key stolen from the
        // system publishing the prices signs no charging pools then.
        var now                   = Timestamp.Now;
        var identityKeys          = new[] {
                                        COSEKeyPair.NewES512(),
                                        COSEKeyPair.NewEd448()
                                    };
        var signingKeys           = new Dictionary<String, COSEKeyPair> {
                                        { POIKeyUsages.Infrastructure, COSEKeyPair.NewES256() },
                                        { POIKeyUsages.Tariffs,        COSEKeyPair.NewES256() },
                                        { POIKeyUsages.Party,          COSEKeyPair.NewES256() }
                                    };

        // The private keys stay with DE*GEF, where nobody else may read them.
        Directory.CreateDirectory("DE-GEF");
        for (var number = 1; number <= identityKeys.Length; number++)
            Helpers.SaveKey(identityKeys[number - 1].Private, $"DE-GEF/identity{number}-privateKey.pem");

        foreach (var usage in signingKeys.Keys)
            Helpers.SaveKey(signingKeys[usage].Private, $"DE-GEF/signing-{usage}-privateKey.pem");

        // What the roaming network operator gets: the operator as POI writes it, with the public halves of its keys.
        var gef                   = new ChargingStationOperator(
                                        gefId,
                                        Name:          I18NString.Create(Languages.en, "GraphDefined Charging Solutions"),
                                        Description:   I18NString.Create(Languages.en, "GraphDefined the first Charging Station Operator on the Open Charging Cloud"),
                                        SigningKeys:   [
                                                           POIPublicKey.From(signingKeys[POIKeyUsages.Infrastructure].Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Infrastructure ]),
                                                           POIPublicKey.From(signingKeys[POIKeyUsages.Tariffs].       Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Tariffs, POIKeyUsages.PriceStatements ]),
                                                           POIPublicKey.From(signingKeys[POIKeyUsages.Party].         Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Party ])
                                                       ],
                                        IdentityKeys:  [ .. identityKeys.Select(identityKey => POIPublicKey.From(identityKey.Private)) ]
                                    );

        File.WriteAllText("DE-GEF/operator.json", gef.ToJSON().ToString());

        // The manufacturers of its charging stations and of the energy meters of its EVSEs, which QA does not know yet: its
        // stations and meters will name them. They are a registry of the roaming network operator, which decides on them
        // together with DE*GEF.
        var manufacturers         = new[] {
                                        RoamingNetworkChange.Add(new ChargingStationManufacturer(
                                                                     stationMakerId,
                                                                     Name:      I18NString.Create(Languages.en, "GraphDefined Charging Stations"),
                                                                     Homepage:  URL.Parse("https://stations.graphdefined.example")
                                                                 )),
                                        RoamingNetworkChange.Add(new EnergyMeterManufacturer(
                                                                     acMeterMakerId,
                                                                     Name:         ImmutableI18NString.Create(Languages.en, "Saale Metering"),
                                                                     Description:  ImmutableI18NString.Create(Languages.en, "Calibrated AC energy meters"),
                                                                     Homepage:     URL.Parse("https://saale-metering.example")
                                                                 )),
                                        RoamingNetworkChange.Add(new EnergyMeterManufacturer(
                                                                     dcMeterMakerId,
                                                                     Name:         ImmutableI18NString.Create(Languages.en, "Kernberg DC Metering"),
                                                                     Description:  ImmutableI18NString.Create(Languages.en, "Calibrated DC energy meters"),
                                                                     Homepage:     URL.Parse("https://kernberg-metering.example")
                                                                 ))
                                    };

        #endregion

        #region 2. DE*GEF asks to be added to QA

        // DE*GEF makes the change set adding it and its manufacturers, for the version of QA it checked
        // (ExampleA1.Validate), and signs it with each of its identity keys: the proof that it holds them, and that it asks
        // for it. Without, OCC could add DE*GEF without it knowing, or anybody the keys of another one.
        if (!RoamingNetwork.TryParse(Helpers.ReadJSON("published/QA.json"), out var checkedQA, out var error))
            Helpers.Fail(error);

        var addGEF                = checkedQA.CreateChangeSet(
                                        [ RoamingNetworkChange.Add(gef), .. manufacturers ],
                                        Description:  ImmutableI18NString.Create(Languages.en, "DE*GEF, GraphDefined Charging Solutions, and the manufacturers of its charging stations and energy meters")
                                    );

        foreach (var identityKey in identityKeys)
            addGEF                = addGEF.Sign(identityKey.Private,
                                                POIPublicKey.From(identityKey.Private).Id,
                                                identityKey.Algorithm);

        File.WriteAllText("DE-GEF/addGEF.json", addGEF.ToJSON().ToString());

        #endregion

        #region 3. The roaming network operator adds DE*GEF to QA

        // OCC reads what DE*GEF sent: the public halves of its keys only, a private key would be refused, signed by them.
        if (!RoamingNetworkChangeSet.TryParse(Helpers.ReadJSON("DE-GEF/addGEF.json"), out var addGEFSent, out error))
            Helpers.Fail(error);

        // Who is a member is the roaming network operator's, decided by its quorum: two of its three holders; so are its
        // registries of manufacturers.
        if (!A1.RoamingNetwork.TryCheckCoSigning(addGEFSent, out var missing, out _, out error))
            Helpers.Fail(error);

        Console.WriteLine($"Sent by DE*GEF, signed by its {identityKeys.Length} identity keys; missing: {String.Join(" ", missing)}");

        // The change set signed by the signing key of the given organization.
        RoamingNetworkChangeSet SignedBy(RoamingNetworkChangeSet ChangeSet, String Organization)

            => ChangeSet.Sign(A1.SigningKeys[Organization].Private,
                              POIPublicKey.From(A1.SigningKeys[Organization].Private).Id,
                              A1.SigningKeys[Organization].Algorithm);

        // AlphaOrg alone: refused, nothing is committed.
        var byAlphaOrg            = SignedBy(addGEFSent, "AlphaOrg");
        var refused               = A1.History.Commit(byAlphaOrg);

        Console.WriteLine($"Signed by AlphaOrg alone: {refused.Status}, {refused.ErrorResponse}");

        // BetaOrg signs it as well: 2/3 quorum reached!
        var byAlphaAndBetaOrg     = SignedBy(byAlphaOrg, "BetaOrg");
        var added                 = A1.History.Commit(byAlphaAndBetaOrg);

        if (!added.IsCommitted)
            Helpers.Fail(added.ErrorResponse ?? added.Status.ToString());

        Console.WriteLine($"Signed by AlphaOrg and BetaOrg: {added.Status}, revision {added.Commit?.Revision}, commit {added.Commit?.Id}");
        Console.WriteLine();

        // What everybody may read now: the roaming network with DE*GEF, and the commit leading to it.
        File.WriteAllText($"published/{A1.RoamingNetwork.Id}.json",         A1.RoamingNetwork.  ToJSON(IncludeETags: true).ToString());
        File.WriteAllText($"published/{A1.RoamingNetwork.Id}-commit.json",  A1.History.Head.Commit.ToJSON().                  ToString());

        #endregion

        return new (identityKeys,
                    signingKeys);

    }


    /// <summary>
    /// Checks, as DE*GEF, that it is a member of `QA` with its own keys: from what is published and the public halves of
    /// its keys it sent only.
    /// </summary>
    public static void Validate()
    {

        #region 4. Checking, as DE*GEF, that QA names it with its keys

        // a) QA as everybody checks it: from the QR code, the roaming network and its commit, now at revision 1.
        ExampleA1.Validate();

        // b) DE*GEF in it, with the keys it sent, and none other.
        var network  = Helpers.ReadJSON("published/QA.json");
        var sent     = Helpers.ReadJSON("DE-GEF/operator.json");

        if (!RoamingNetwork.TryParse(network, out var qa, out var error))
            Helpers.Fail(error);

        if (!ChargingStationOperator.TryParse(sent, out var gefSent, out error))
            Helpers.Fail(error);

        if (!qa.TryGetChargingStationOperatorById(gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{gefId}'");

        static String KeyIds(IEnumerable<POIPublicKey> Keys)
            => String.Join(", ", Keys.Select(key => key.Id).Order());

        if (KeyIds(gef.SigningKeys)  != KeyIds(gefSent.SigningKeys) ||
            KeyIds(gef.IdentityKeys) != KeyIds(gefSent.IdentityKeys))
            Helpers.Fail($"QA names '{gefId}' with other keys than it sent");

        Console.WriteLine();
        Console.WriteLine($"{gefId} is a member of QA with its keys:");

        foreach (var key in gef.SigningKeys)
            Console.WriteLine($"  signing key  {key.Id} for {String.Join(", ", key.Usages)}");

        foreach (var key in gef.IdentityKeys)
            Console.WriteLine($"  identity key {key.Id}");

        // c) The manufacturers its stations and meters will name.
        Console.WriteLine();

        foreach (var manufacturer in qa.ChargingStationManufacturers)
            Console.WriteLine($"Charging station manufacturer {manufacturer.Id}: {manufacturer.Name.FirstText()}");

        foreach (var manufacturer in qa.EnergyMeterManufacturers)
            Console.WriteLine($"Energy meter manufacturer     {manufacturer.Id}: {manufacturer.Name.FirstText()}");

        #endregion

    }

}
