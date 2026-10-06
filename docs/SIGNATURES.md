# Signatures and trust

[Repository overview](../README.md) · [Architecture](ARCHITECTURE.md) · [ChangeSets](CHANGESETS.md)

## Implemented today

`RoamingNetworkChangeSetSignature` stores three strings:

| Field | Intended use |
| --- | --- |
| `Algorithm` | Algorithm/profile agreed by the communicating applications |
| `KeyId` | Identifier used by an application to resolve a verification key |
| `Value` | Signature representation agreed by those applications |

These fields are an envelope. The repository currently does not prescribe an encoding for
`Value`, validate the envelope's string contents, look up keys, generate signatures or execute
a cryptographic signature algorithm.

The applier implements this control flow:

| Batch | Behavior |
| --- | --- |
| No signature | Accepted without calling the signature verifier, subject to ordinary validation |
| Signature, no verifier | Rejected |
| Signature, verifier returns false | Rejected |
| Signature, verifier throws | Rejected; verifier failure is wrapped with batch context |
| Signature, verifier returns true | Continue with operation/domain validation |

Target-network and revision checks happen before the verifier. Signature verification happens
before any ChangeSet operation. A valid signature does not bypass field, ownership, old-value or
tariff-reference validation.

## Integrating a real verifier

The callback receives the complete `RoamingNetworkChangeSet`. The application must define what
is signed, resolve a trusted key and verify the signature against that exact batch.

This wrapper enforces a signed-only policy at an application boundary:

```csharp
using cloud.charging.open.protocols.WWCP.POI;

static RoamingNetwork ApplySigned(
    RoamingNetwork source,
    RoamingNetworkChangeSet changeSet,
    Func<RoamingNetworkChangeSet, bool> verify)
{
    if (changeSet.Signature is null)
        throw new InvalidOperationException("This endpoint requires a signed ChangeSet.");

    return source.ApplyChangeSet(changeSet, VerifySignature: verify);
}
```

Here `verify` must be supplied by the application's actual verification implementation.
A callback returning true unconditionally only satisfies the hook; it establishes no authenticity.

The core applier accepts unsigned batches. Passing a verifier alone does not enforce that all
batches are signed, because the callback is not invoked for an unsigned batch.

## Why normal JSON serialization is insufficient as a signing contract

The repository has two JSON layers: Newtonsoft.Json POI documents and System.Text.Json ChangeSet
documents. Serializer options can change naming, escaping, whitespace and other representations.

Snapshot export orders properties and child groups for reproducible output in this implementation,
but it is not a specified canonical signing format. Equal domain IDs can have different wire
spellings, and legacy input representations can differ from current output.

A future signing profile needs an unambiguous specification of:

1. The signed content: network identity, base version, batch identity, timestamp and the complete
   ordered operation list, including old/new values and parent scopes.
2. A stable encoding of JSON numbers, strings, timestamps, property names and object ordering.
3. The distinction between absent values and explicit JSON null.
4. How the signature envelope is excluded from the signed payload, and how relevant
   algorithm/key/profile information is bound to that payload.
5. How all senders and receivers obtain the same bytes before verification.

Until such a profile exists, applications using the hook must agree on their own signing contract.
The repository does not provide interoperable canonical signing bytes.

## Authenticity, authorization and domain validation

These are separate checks:

- Signature verification establishes that the agreed payload verifies with a particular key.
- Authorization decides whether that key may change this network, operator, entity or property.
- Domain validation decides whether the resulting infrastructure data is structurally and
  semantically acceptable to the model.

The library supplies the third category and the hook for the first. It does not implement a
signer-to-operator permission model. Applications can enforce trust and authorization before
application or within their verification boundary.

Transparency-software certificate fields are domain metadata. Parsing those strings and validity
intervals does not validate a certificate chain or authenticate a ChangeSet signature.

## Revisions, replay and branches

`BaseRevision` rejects a batch targeting a different numeric version of the supplied snapshot.
It does not provide a cryptographic predecessor identity, a durable replay ledger or a global
ordering across replicas.

`Id` is not deduplicated by the applier. `CreatedAt` has no automatic freshness policy.
`AppliedChangeSetId` records the most recently applied batch only. A validly signed batch can
still be reapplied to the same original snapshot to derive another successor.

For a future distributed commit model, the remaining design work includes:

- Cryptographic identity of the predecessor and resulting commit, distinguishing equal-numbered branches.
- A versioned signing/canonicalization profile and concrete signing/verification implementation.
- Key resolution, trust policy, revocation and signer authorization.
- Replay/history persistence and publication of the current head.
- Conflict/merge rules, auditability and transport behavior.

These are architectural next steps. They are not implemented guarantees of the current library.
