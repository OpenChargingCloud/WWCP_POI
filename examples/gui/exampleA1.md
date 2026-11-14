# Example A1 with three hubs: `AlphaOrg` begins `QA`, `BetaOrg` and `GammaOrg` join it

[Example A1](../exampleA1.md) plays three organizations in one program. Here each of them runs a
[RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI) of its own, and somebody at each hub does its part
in the web interface: `AlphaOrg` makes the roaming network `QA`, `BetaOrg` follows it, asks to hold the keys of its
operator `OCC` as well and is admitted, two of two deciding, then `GammaOrg` the same way, two of three. What example
A1 does with `RoamingShareholder`, `Admit` and `Dismiss`, the hubs do among themselves over the POI API; every step
is also a command at the prompt and a request of the JSON API, named below each step.

| Revision | Who holds the keys of `OCC` | Quorum | Done at |
|---|---|---|---|
| 0, the genesis | `AlphaOrg` | 1 of 1 | hub A |
| 1 | `AlphaOrg`, `BetaOrg` | 2 of 2 | hub B, hub A, hub B |
| 2 | `AlphaOrg`, `BetaOrg`, `GammaOrg` | 2 of 3 | hub C, hub A, hub B, hub A, hub B, hub C |

The messages below are what one run of three hubs on one machine answered, `…` for elided digests. Every step is done
by a person, at the right hub, in the right order: [what is missing](#what-is-missing) for three hubs to do it from
their configuration, and to tell each other what waits, is at the end.


## 1. Three hubs

Each hub has a directory of its own, with its configuration, its accounts, its keys and its roaming networks. Its
`configuration.json` says who it is before its first start: the roaming network operator it is, and the holder its
keys name, the organization that counts as one voice among those who decide.

| | Directory | `configuration.json` | Port |
|---|---|---|---|
| hub A | `AlphaOrg/` | `{ "roamingNetworkOperator": { "@id": "OCC", "name": "Open Charging Cloud", "holder": "AlphaOrg" } }` | 2356 |
| hub B | `BetaOrg/` | `{ "roamingNetworkOperator": { "@id": "BetaOrg", "name": "BetaOrg", "holder": "BetaOrg" } }` | 2357 |
| hub C | `GammaOrg/` | `{ "roamingNetworkOperator": { "@id": "GammaOrg", "name": "GammaOrg", "holder": "GammaOrg" } }` | 2358 |

Hub A is `OCC`, as it makes `QA`. The other two are roaming network operators of their own for the roaming networks
they make themselves; in `QA` they are holders of the keys of `OCC`, counted by their `holder`. Each is started in its
directory:

```
dotnet …/RoamingHubCLI.dll --port 2356 --no-ssh --config configuration.json --accounts accounts --certificates certificates --log-file logs
```

At its first start a hub makes its identity key and its signing key, naming their holder, and keeps them in
`roamingNetworkOperator/` beside its configuration, and it makes up the account `root`, whose password it prints once:

```
This hub is the roaming network operator 'OCC' now: its signing key 6efade3a… and its identity key 24c14776… were made in '…\AlphaOrg\roamingNetworkOperator'. Keep a copy of them where nobody else can read it: …
```

Sign in at each as `root`, at `http://127.0.0.1:2356/`, `:2357/` and `:2358/`. A browser keeps one sign-in per host,
whatever the port: three hubs on one machine take three browser profiles, or private windows.

These are the two kinds of keys of [section 1](../exampleA1.md#1-two-kinds-of-keys-and-who-decides) of example A1.
Where the example writes them to `AlphaOrg/identity-privateKey.pem`, a hub keeps them itself and shows the public
ones: **Points of interest**, the card **This hub as a roaming network operator**; `roamingNetworkOperator` at the
prompt; `GET /api/v1/poi/roamingNetworkOperator`.


## 2. `AlphaOrg` alone: revision 0

At hub A, **Points of interest**, the card **New roaming network**: *Identification* `QA`, its name, **Make the
roaming network**.

```
The roaming network 'QA' was made, at revision 0, commit 8c4f4c20…, operated by this hub, 'OCC'. Its parties are added now, with change sets this hub signs.
```

The hub does what [section 2](../exampleA1.md#2-alphaorg-alone-revision-0) of example A1 does by hand: the key
statement of its identity key, a quorum of one, the certificate of its signing key, `OCC` and `QA`, and the genesis
commit of the history, signed by it. The page of `QA` shows its trust anchor, as a QR code and as a URL; it is what
`AlphaOrg` hands to `BetaOrg`:

```
http://127.0.0.1:2356/poi/RNs/QA?keys=24c14776f9fa6986df8adf3528c3f0ff8b767f2b4adc6a26e14ddacd07d7b183
```

At the prompt `roamingNetworks add QA Quality assurance`; `POST /api/v1/poi/RNs` with `{"@id": "QA", "name": {"en":
"Quality assurance"}}`; the trust anchor is `GET /poi/RNs/QA/trustAnchor`, for everybody.


## 3. `BetaOrg` joins: two of two, revision 1

Three steps, at hub B, at hub A, and at hub B again. None follows from the one before by itself.

**Hub B follows `QA`.** **Points of interest**, the card **Roaming networks followed**: the trust anchor of step 2 in
*Trust anchor*, **Follow the roaming network**. Hub B reads `QA` at hub A, checks it against the trust anchor, and
keeps a copy of it current from then on, over the WebSocket of its history there:

```
The roaming network 'QA' is followed at 'http://127.0.0.1:2356/poi/RNs/QA', at revision 0, commit 8c4f4c20…. Its copy is served here, and changed from here by nobody.
```

**Hub B asks.** On the page of `QA` at hub B, the card **Keys of the operator**, **Ask to hold the keys**. Hub B
sends hub A its public identity key, the certificate of its signing key as a holder of the keys of `OCC`, and the key
statement of both identity keys with a quorum of two, signed with its identity key: the proof that it holds it.

```
This hub, 'BetaOrg', asked to hold the keys of the operator 'OCC' of the roaming network 'QA' as well, 2 of 2 deciding. It waits for those who hold them to admit it; 'roamingNetworks QA join' goes on then.
```

**Hub A admits it.** On the page of `QA` at hub A, **Keys of the operator** now shows what waits, `BetaOrg` asking,
with **Admit**. Hub A signs the key statement, makes the one change set of it - `RoamingNetwork.Admit` of example A1 -
signs it with its identity key and its signing key, and lets it wait:

```
The change set '488689df…' admitting 'BetaOrg' is made and signed by this hub. It waits for signatures: Change 0: UpdateProperty of the RoamingNetworkOperator 'OCC' ('identityKeys') needs a signature by the new identity key '8a777de4…' of the roaming network operator 'OCC', as the proof that it holds it!
```

**Hub B signs it.** At hub B, **Ask to hold the keys** again. It makes the change set again from its own copy, sees
that it is exactly the one waiting, signs it with its identity key and sends the signature to hub A, where it is
committed:

```
The change set '488689df…' admitting 'BetaOrg' is signed by this hub, and committed at 'http://127.0.0.1:2356/poi/RNs/QA'.
```

**Hub B signs the commit.** Revision 1 is signed by hub A, which made it. At hub B, **Sign its head commit**: with
both signatures whoever checks `QA` takes it, as two decide now, and hub B attests that head from then on.

```
The commit '2329853e…' of the roaming network 'QA', revision 1, is signed as 'BetaOrg', one of the holders of the keys of its operator, and the signature was taken at 'http://127.0.0.1:2356/poi/RNs/QA'.
```

| Step | At | At the prompt | Over HTTP, as somebody with `poi:edit` |
|---|---|---|---|
| follow | hub B | `roamingNetworks follow <trust anchor>` | `POST /api/v1/poi/followed`, `{"trustAnchor": "…"}` |
| ask | hub B | `roamingNetworks QA join` | `POST /api/v1/poi/shareholders/QA/join` |
| admit | hub A | `roamingNetworks QA admit BetaOrg` | `POST /api/v1/poi/shareholders/QA/admit/BetaOrg` |
| sign the change set | hub B | `roamingNetworks QA join` | `POST /api/v1/poi/shareholders/QA/join` |
| sign the commit | hub B | `roamingNetworks QA sign` | `POST /api/v1/poi/followed/QA/signatures` |


## 4. `GammaOrg` joins: two of three, revision 2

The same, with one holder more to sign. The trust anchor hub C follows `QA` by is the one of today, of both holders:
a hub starts with the current version of `QA`, and takes it only where the trust anchor it was given names the keys
that signed it. With the trust anchor of step 2, of `AlphaOrg` alone, hub C is refused:

```
Following 'ws://127.0.0.1:2356/poi/RNs/QA/history/ws' failed: The snapshot of the other server is not anchored in the trust anchor of the roaming network 'QA': The key statement is signed by trusted identity keys of 1 holder, not of 2!
```

So `AlphaOrg` or `BetaOrg` hands `GammaOrg` the trust anchor of revision 1:

```
http://127.0.0.1:2356/poi/RNs/QA?keys=24c14776…,8a777de4…&minConsensus=2
```

| Step | At | In the web interface | What it answers |
|---|---|---|---|
| follow | hub C | **Follow the roaming network** | `The roaming network 'QA' is followed at '…', at revision 1, …` |
| ask | hub C | **Ask to hold the keys** | `This hub, 'GammaOrg', asked to hold the keys … 2 of 3 deciding. It waits for those who hold them to admit it; …` |
| the key statement | hub A | **Admit** | `The key statement admitting 'GammaOrg' is signed by this hub. It waits for the other holders: … Each signs it with 'roamingNetworks QA admit GammaOrg' at its hub.` |
| the key statement | hub B | **Admit** | `The key statement admitting 'GammaOrg' is signed by this hub, and its signature was taken at '…'. Its change set is made there next.` |
| the change set | hub A | **Admit** | `The change set '2a2ca4f3…' admitting 'GammaOrg' is made and signed by this hub. It waits for signatures: … needs signatures by identity keys of 2 holders … needs a signature by the new identity key '63dfb2da…' …` |
| the change set | hub B | **Admit** | `The change set '2a2ca4f3…' admitting 'GammaOrg' is signed by this hub. It waits for signatures: … needs a signature by the new identity key '63dfb2da…' …` |
| the change set | hub C | **Ask to hold the keys** | `The change set '2a2ca4f3…' admitting 'GammaOrg' is signed by this hub, and committed at '…'.` |
| the commit | hub B, hub C | **Sign its head commit** | `The commit '4d21d32b…' of the roaming network 'QA', revision 2, is signed as 'BetaOrg', …` |

The key statement first, by every holder, and only then the change set of it, by every holder again: a key statement
is content of the change set that enters it, so it is complete before that change set is made
([section 4](../exampleA1.md#4-gammaorg-joins-two-of-three-revision-2) of example A1). `admit` at the prompt and over
HTTP as in step 3, with `GammaOrg`.

Afterwards all three say the same of `QA`. **Keys of the operator** at each, `roamingNetworks QA shareholders`,
`GET /api/v1/poi/shareholders/QA`, here of hub C:

```json
{
  "roamingNetworkOperator": "OCC",
  "minConsensus":           2,
  "holders":                [ { "holder": "AlphaOrg", … }, { "holder": "BetaOrg", … }, { "holder": "GammaOrg", … } ],
  "changes":                [],
  "followed":               true,
  "thisHub":                "GammaOrg"
}
```


## 5. Checking `QA`

Each hub serves `QA` below `/poi`, for everybody: `GET /poi/RNs/QA`, its head `GET /poi/RNs/QA/history/head`, its
trust anchor `GET /poi/RNs/QA/trustAnchor`. Whoever has the trust anchor checks what it reads from any of the three,
as `ExampleA1.Validate()` does with what the example publishes
([section 7](../exampleA1.md#7-checking-qa-as-a-charging-station-operator-or-an-app)), and a hub following by an older
trust anchor follows the key statements to the keys of today
([section 8](../exampleA1.md#8-from-an-older-trust-anchor-to-the-keys-of-today)): hub B, which followed `QA` when
`AlphaOrg` was alone, went on through both revisions.


## 6. If `GammaOrg` left again

Example A1 only tries it ([section 9](../exampleA1.md#9-if-gammaorg-left-again-two-of-two)). Between hubs it is done:
two of three dismiss the third without it. It begins where `QA` is made, **Dismiss** in the row of `GammaOrg` at hub
A, then **Agree** at hub B, each twice, the key statement first and then its change set:

| At | At the prompt | What it answers |
|---|---|---|
| hub A | `roamingNetworks QA dismiss GammaOrg` | `The key statement dismissing 'GammaOrg' is signed by this hub. It waits for the other holders: …` |
| hub B | `roamingNetworks QA dismiss GammaOrg` | `The key statement dismissing 'GammaOrg' is signed by this hub, and its signature was taken at '…'. Its change set is made there next.` |
| hub A | `roamingNetworks QA dismiss GammaOrg` | `The change set '4e2b0895…' dismissing 'GammaOrg' is made and signed by this hub. It waits for signatures: …` |
| hub B | `roamingNetworks QA dismiss GammaOrg` | `The change set '4e2b0895…' dismissing 'GammaOrg' is signed by this hub, and committed at '…'.` |

Revision 3: `AlphaOrg` and `BetaOrg`, two of two. Hub C goes on following `QA`, as a copy.


## What differs from example A1

- **`QA` names no URLs of its copies.** In example A1 `QA` names `alpha.example`, `beta.example` and `gamma.example`
  as its `urls`. A hub does not say where it is reached: the trust anchor names the URL it was asked at, and the URL
  of a copy is taken into `QA` only where a hub joining says one over `https`, in *Where the copy of this hub is*.
- **The identification of the operator** is `OCC` because the configuration of hub A says so; without it, it were
  `RNO-` and eight digits of its identity key.
- **The revisions are the same**: 0, 1 and 2. Example A2 goes on from there, at hub A, where a party added waits for
  the signatures of two holders now: **Change sets waiting for signatures** on the page of `QA`, **Approve** at a
  second hub.


## What is missing

All of the above was done by three people, each at its own hub, telling each other what to do next. For three hubs to
do it by themselves two things are missing.

**It is not configuration.** A configuration says who a hub is, once, at its first start. It does not say what the hub
takes part in:

- *Which roaming networks it follows*, with their trust anchors, and where: said by a person, kept beside the copy
  from then on.
- *Which of them it wants to hold keys of*, and under which name it is reached: said by a person, `join`.
- *Whom it admits*: a list of the holders an organization agreed to - their names, or their identity keys - by which
  `Admit` were pressed by the hub itself.
- *Whether it signs the commits of the others*, which every holder is told one by one today.
- *Where it is reached*: no hub knows its own public URL, so `QA` names no copies, and nobody following it learns of
  the other two.

**The hubs do not tell each other what waits.** A commit is pushed: every hub following gets it over the WebSocket of
the history at once, and connects again when that ended. Everything before a commit is not:

- *A request, a key statement and a change set waiting* are kept at the hub where `QA` is made, and shown to whoever
  asks, `GET /api/v1/poi/shareholders/QA` and `…/pending`. No hub is told, and none asks by itself: a page shows
  what was there when it was opened, and somebody at hub B learns from somebody at hub A that there is something to
  admit.
- *Nothing goes on by itself.* After the last holder signed a key statement, the hub where `QA` is made is asked
  again to make its change set; after the holders signed that, the hub joining is asked again to sign it; after a
  commit, each other holder is asked to sign it. Each of these is a step a hub could do when it sees that it is its
  turn - by being told over the WebSocket it has open anyway, or by asking every few seconds.
- *What waits is forgotten at a restart* of the hub where `QA` is made, unless its change set was made already.
- *Everything goes through one hub.* The others follow the hub where `QA` is made, and send their signatures there;
  while it is not reached, nothing is decided, and no copy takes its place.
- *A new head is attested by one holder only* until the others signed its commit: until then a hub following anew
  gets no first copy.

So the order of the steps above is not a rule of the roaming network, which takes signatures in any order, but what
people have to keep to while the hubs do not do it. What a hub may do by itself is a decision of its own:
[SHAREHOLDERS.md](https://github.com/OpenChargingCloud/RoamingHub/blob/master/SHAREHOLDERS.md) of the hub decided
that it signs nothing for another by itself, and a configuration naming whom to admit and what to sign would be how
an organization says otherwise, once, instead of every time.
