
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

