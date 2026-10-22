
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
