
// The examples, each building on the one before: what one makes, the next is given.
// Each check reads only what is published, as a charging station operator or an app would.


// What the last run wrote is removed first: its keys, what it published and its history would not fit the new keys.
foreach (var directory in new[] { "AlphaOrg", "BetaOrg", "GammaOrg", "DE-GEF", "GraphDefined", "SAALE", "KERNBERG", "PTB", "EM-LAB", "published", "trusted", "driver", "poi", "witnesses" })
    if (Directory.Exists(directory))
        Directory.Delete(directory, recursive: true);


// The roaming network is created and the organizations behind it sign the quorum.
using var a1 = ExampleA1.Run();

ExampleA1.Validate();

Console.WriteLine();
Console.WriteLine();



// The Charging Station Operator DE*GEF (GraphDefined) adds itself and the manufacturers of its stations and meters via a
// change set to the roaming network, the roaming network operator checks the change set and finally adds them to its list
// of known parties; the manufacturers then state their models themselves.
var a2 = ExampleA2.Run(a1);

ExampleA2.Validate();

Console.WriteLine();
Console.WriteLine();



// DE*GEF adds its templates, two pools of a DC and an AC station each, and the groups of its stations and EVSEs,
// with change sets signed by its signing key for its infrastructure.
ExampleA3.Run(a1, a2);

ExampleA3.Validate();

Console.WriteLine();
Console.WriteLine();



// The roaming network operator makes the first checkpoint of QA; DE*GEF adds two pools more, their AC stations recorded
// with 44 kW and EVSEs of 22 kW; a server or an app joins QA from the checkpoint.
ExampleA4.Run(a1, a2);

ExampleA4.Validate();

Console.WriteLine();
Console.WriteLine();



// The next checkpoint, the correction of these AC stations to 22 kW with EVSEs of 11 kW, and the checkpoint after;
// the AC stations checked at every checkpoint.
ExampleA5.Run(a1, a2);

ExampleA5.Validate();

Console.WriteLine();
Console.WriteLine();



// Two witnesses, each with its own copy of the history of QA since the genesis, check and sign the three checkpoints;
// a verifier then requires the roaming network operator and both witnesses.
var a6 = ExampleA6.Run(a1);

ExampleA6.Validate();

Console.WriteLine();
Console.WriteLine();



// The roaming network operator compacts the history of QA after its checkpoints, archiving what it drops;
// the archive is checked against the checkpoints.
ExampleA7.Run(a1);

ExampleA7.Validate();

Console.WriteLine();
Console.WriteLine();



// DE*GEF adds its charging tariffs and states ad hoc prices of its DC EVSEs, signed price statements; checkpoint 4 names
// them, and they are checked against it.
ExampleA8.Run(a1, a2);

ExampleA8.Validate();


Console.WriteLine();
Console.WriteLine();



// The witnesses of example A6 go on: they take the commits of example A8 and the checkpoints 3 and 4, check the price
// statements checkpoint 4 names, and sign them; the proof of a price statement is then witnessed evidence.
ExampleA9.Run(a1, a6);

ExampleA9.Validate();

Console.WriteLine();
Console.WriteLine();



// A driver charges at DE*GEF*E1*1: the app checks the proof of the station and the price statement of the EVSE offline,
// keeps them, and later the proofs of both in checkpoints the witnesses signed; months later, they are evidence of the
// price that applied when charging began.
ExampleA10.Run(a1, a2, a6);

ExampleA10.Validate();

Console.WriteLine();
Console.WriteLine();



// Cryptography ages: QA moves into epoch 2, of SHA-512 and ML-DSA. The new keys first, next to the old ones; the final
// checkpoint 6 closes epoch 1; the first commit of epoch 2 takes the old keys out, its first checkpoint is signed with
// both, so a verifier trusting only the old ones follows. Epoch 1 stays readable.
using var a11 = ExampleA11.Run(a1, a2, a6);

ExampleA11.Validate();

Console.WriteLine();
Console.WriteLine();



// The atlas and the traffic report: OCC serves QA, a mirror follows it, a driver's app subscribes there; DE*GEF reports the
// status, the energy status and the price of an EVSE live, pushed within moments and kept in no commit; its next change of
// a tariff is pushed as a signed commit.
await ExampleA12.Run(a11);

ExampleA12.Validate();

Console.WriteLine();
Console.WriteLine();



// Who checks the meter: the transparency software manufacturer GraphDefined states its release of Chargy, the PTB, notified
// body for the MID, certifies it for a station model, each signing itself; the BSI, for the CRA, does not take part yet.
// DE*GEF names both for the energy meters of its DC stations, and the station proof gives them.
ExampleA13.Run(a11);

ExampleA13.Validate();

Console.WriteLine();
Console.WriteLine();



// The papers of a meter, digital: SAALE declares the conformity of its meter model SM-3, the PTB states its EU-type
// examination certificate, a calibration laboratory the calibration of the meter of DE*GEF*E2*1; each signs its own,
// and anybody checks them against QA.
ExampleA14.Run(a11);

ExampleA14.Validate();

Console.WriteLine();
Console.WriteLine();



// EV Taler: DE*GEF accepts GNU Taler at Jena Paradies, to be paid step by step. It names its merchant at a GNU Taler
// merchant backend and the exchanges it accepts, signed by a key for payments alone; the station proof gives them, and
// the app shows "EV Taler (provider)" for the exchanges in the currency of the EVSE's tariff.
ExampleA15.Run(a11);

ExampleA15.Validate();
