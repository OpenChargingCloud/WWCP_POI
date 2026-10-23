
// The examples, each building on the one before: what one makes, the next is given.
// Each check reads only what is published, as a charging station operator or an app would.


// The roaming network is created and the organizations behind it sign the quorum.
using var a1 = ExampleA1.Run();

ExampleA1.Validate();

Console.WriteLine();
Console.WriteLine();



// The Charging Station Operator DE*GEF (GraphDefined) adds itself via a change set to the roaming network,
// the roaming network operator checks the change set and
// finally adds DE*GEF to its list of known parties.
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
ExampleA6.Run(a1);

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
