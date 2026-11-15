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
| 1 | `AlphaOrg`, `BetaOrg` | 2 of 2 | decided at hub B and at hub A |
| 2 | `AlphaOrg`, `BetaOrg`, `GammaOrg` | 2 of 3 | decided at hub C, at hub A and at hub B |

The messages below are what one run of three hubs on one machine answered, `…` for elided digests. Somebody decides
once at each hub - to ask, to admit - and what follows from that the hubs do by themselves, as soon as the hub where
`QA` is made tells them that something waits. [What is missing](#what-is-missing) for three hubs to do it from their
configuration is at the end.


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

Three steps by somebody: hub B follows `QA` and asks, hub A admits it. The rest hub B does by itself.

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
This hub, 'BetaOrg', asked to hold the keys of the operator 'OCC' of the roaming network 'QA' as well, 2 of 2 deciding. It waits for those who hold them to admit it; this hub goes on by itself then, as soon as it is told.
```

**Hub A admits it.** On the page of `QA` at hub A, **Keys of the operator** now shows what waits, `BetaOrg` asking,
with **Admit**. Hub A signs the key statement, makes the one change set of it - `RoamingNetwork.Admit` of example A1 -
signs it with its identity key and its signing key, and lets it wait:

```
The change set '488689df…' admitting 'BetaOrg' is made and signed by this hub. It waits for signatures: Change 0: UpdateProperty of the RoamingNetworkOperator 'OCC' ('identityKeys') needs a signature by the new identity key '8a777de4…' of the roaming network operator 'OCC', as the proof that it holds it!
```

**Hub B signs it, by itself.** It asked, so nobody says it again: hub A tells those following `QA` that a change
set waits, a notice over the WebSocket of its history. Hub B looks at what waits at hub A, makes the change set
again from its own copy, sees that it is exactly the one waiting, signs it with its identity key and sends the
signature to hub A, where it is committed. Its log, a quarter of a second after `admit` in this run:

```
By itself: The change set 'e46c35c9…' admitting 'BetaOrg' is signed by this hub, and committed at 'http://127.0.0.1:2356/poi/RNs/QA'.
```

**Hub B signs the commit, by itself.** Revision 1 is signed by hub A, which made it. Hub B signed its change set, so
it signs its commit as well as soon as its copy has it, another quarter of a second later: with both signatures whoever checks `QA` takes it,
as two decide now, and hub B attests that head from then on.

```
The commit '277bde98…' of the roaming network 'QA', revision 1, was signed as 'BetaOrg', one of the holders of the keys of its operator 'OCC'.
```

| Step | At | At the prompt | Over HTTP, as somebody with `poi:edit` |
|---|---|---|---|
| follow | hub B | `roamingNetworks follow <trust anchor>` | `POST /api/v1/poi/followed`, `{"trustAnchor": "…"}` |
| ask | hub B | `roamingNetworks QA join` | `POST /api/v1/poi/shareholders/QA/join` |
| admit | hub A | `roamingNetworks QA admit BetaOrg` | `POST /api/v1/poi/shareholders/QA/admit/BetaOrg` |
| sign the change set | hub B | by itself; at once with `roamingNetworks QA join` | `POST /api/v1/poi/shareholders/QA/join` |
| sign the commit | hub B | by itself; at once with `roamingNetworks QA sign` | `POST /api/v1/poi/followed/QA/signatures` |

Where a notice got lost, each hub looks every minute as well; and **Ask to hold the keys** and **Sign its head
commit** at hub B do the same when pressed.


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
| ask | hub C | **Ask to hold the keys** | `This hub, 'GammaOrg', asked to hold the keys … 2 of 3 deciding. It waits for those who hold them to admit it; this hub goes on by itself then, …` |
| admit | hub A | **Admit** | `The key statement admitting 'GammaOrg' is signed by this hub. It waits for the other holders: … Each signs it with 'roamingNetworks QA admit GammaOrg' at its hub.` |
| admit | hub B | **Admit** | `The key statement admitting 'GammaOrg' is signed by this hub, and its signature was taken at '…'. The change set '97691255…' admitting 'GammaOrg' is signed by this hub. It waits for signatures: … needs a signature by the new identity key '41ebe195…' …` |
| its proof | hub C | by itself | its log: `By itself: The change set '97691255…' admitting 'GammaOrg' is signed by this hub, and committed at '…'.` |
| the commit | hub B, hub C | by itself | their logs: `The commit '…' of the roaming network 'QA', revision 2, was signed as 'BetaOrg', …` |

Three decisions: to ask, at hub C, and to admit, at hub A and at hub B. The key statement comes first, signed by
every holder, and only then the change set of it: a key statement is content of the change set that enters it, so it
is complete before that change set is made
([section 4](../exampleA1.md#4-gammaorg-joins-two-of-three-revision-2) of example A1). With the signature of hub B the
key statement is complete, so hub A makes the change set of it at once, which it signs, having signed the key
statement; hub B signs it in the same step, for the same reason. Hub C, told, added its proof a tenth of a second
later in this run, and both signed the commit within a third of a second after. `admit` at the prompt and over HTTP as in step 3, with
`GammaOrg`.

Hub C was given a first copy of `QA` because hub B had signed the head of revision 1: a first copy is anchored in
the attestations of as many holders as decide, two by then.

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
A, then **Agree** at hub B:

| At | At the prompt | What it answers |
|---|---|---|
| hub A | `roamingNetworks QA dismiss GammaOrg` | `The key statement dismissing 'GammaOrg' is signed by this hub. It waits for the other holders: …` |
| hub B | `roamingNetworks QA dismiss GammaOrg` | `The key statement dismissing 'GammaOrg' is signed by this hub, and its signature was taken at '…'. The change set '3162cc4b…' dismissing 'GammaOrg' is signed by this hub, and committed at '…'.` |

Revision 3: `AlphaOrg` and `BetaOrg`, two of two; hub B signs its commit by itself, at once. Hub C goes on
following `QA`, as a copy.


## What differs from example A1

- **`QA` names no URLs of its copies.** In example A1 `QA` names `alpha.example`, `beta.example` and `gamma.example`
  as its `urls`. The hubs of this run did not say where they are reached: the trust anchor names the URL it was
  asked at, and the URL of a copy is taken into `QA` only where a hub joining says one over `https`, in *Where the
  copy of this hub is*. A hub whose configuration says it, `"poi": { "url": "https://beta.example/poi" }`, names it
  by itself: in the roaming networks it makes, in every trust anchor it serves, and when it asks to hold keys.
- **The identification of the operator** is `OCC` because the configuration of hub A says so; without it, it were
  `RNO-` and eight digits of its identity key.
- **The revisions are the same**: 0, 1 and 2. Example A2 goes on from there, at hub A, where a party added waits for
  the signatures of two holders now: **Change sets waiting for signatures** on the page of `QA`, **Approve** at a
  second hub.


## What is missing

All of the above was decided by three people, each at its own hub: to ask, and to admit. The hubs did the rest, at
once, told by the hub where `QA` is made. For three hubs to do all of it, two things are missing.

**Not all of it is configuration.** A configuration says who a hub is, once, at its first start, and of what it takes
part in what follows from the hub alone:

- *Which roaming networks it follows*, with their trust anchors, and where: `"poi": { "follow": [ { "trustAnchor":
  "…" } ] }`, done at every start; or said by a person, as above.
- *Which of them it wants to hold keys of*: `"hold": true` beside the trust anchor, asked once; or `join`.
- *Where it is reached*: `"poi": { "url": "…" }`, as no hub knows its own public URL.

So hub B and hub C of this example start from their configuration, and what is left to somebody is to admit them, at
hub A and at hub B. Not in a configuration:

- *Whom it admits*: a list of the holders an organization agreed to - their names, or their identity keys - by which
  `Admit` were pressed by the hub itself.
- *Whether it signs the commits of the others*: a hub signs the commit of a change set it signed by itself, and every
  other commit when it is told, one by one.

**The hubs tell each other that something waits, and not more.** A commit is pushed: every hub following gets it over
the WebSocket of the history at once, and connects again when that ended. What comes before a commit is told as a
notice:

- *A request, a key statement and a change set waiting* are kept at the hub where `QA` is made, and shown to whoever
  asks, `GET /api/v1/poi/shareholders/QA` and `…/pending`. The hubs following are told that one of them is another
  thing now, ask, and do what follows from what was decided at them; each asks every minute as well, for a notice
  that got lost. The page of `QA` asks again by itself every few seconds.
- *What is to be decided is said in the log, and on the page of `QA`*: that `GammaOrg` asks, and waits for somebody
  at hub B, somebody at hub B learns from its log, from the page of `QA`, or from somebody at hub A. Nobody is sent
  a message.
- *Everything goes through one hub.* The others follow the hub where `QA` is made, and send their signatures there;
  while it is not reached, nothing is decided, and no copy takes its place.
- *A new head is attested by one holder only* until the others signed its commit, by themselves at once where they
  signed its change set, or when told: until then a hub following anew gets no first copy.

What a hub may do by itself is a decision of its own:
[SHAREHOLDERS.md](https://github.com/OpenChargingCloud/RoamingHub/blob/master/SHAREHOLDERS.md) of the hub decided
that it signs nothing for another by itself, and that what follows from a decision made at it is none - its phase 6,
with the notices between the hubs and the configuration as they are designed. A configuration naming whom to admit
and what to sign would be how an organization says otherwise, once, instead of every time.
