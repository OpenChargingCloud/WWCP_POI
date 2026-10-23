# Stories

LinkedIn posts that explain WWCP_POI to people who are neither cryptographers nor developers: drivers, and
the experts of the industry – operators, e-mobility providers, roaming platforms, metrology, consumer protection.

## Can you trust this charging station?

A series of eleven posts: what WWCP_POI is, why it differs from OICP, OCHP, OCPI and DATEX II, and why authentic POI
data matter under metrology law – correct kWh and correct time are worth little when the meter key, the charging
station, its operator and the tariff are not authentic, and they are an open door for attacks.

| Part | Post | Analogy | Visuals |
|---|---|---|---|
| 0 | [Can you trust this charging station?](00-overview/README.md) | a route with ten stops | `route.png` |
| 1 | [Anna's invoice](01-annas-invoice/README.md) | 39 ct at the charger, 79 ct on the invoice | `invoice.png` |
| 2 | [The meter that brings its own ID card](02-the-meters-own-id-card/README.md) | a self-printed ID card | `evidence-chain.png`, `key-swap.png` |
| 3 | [The envelope and the letter](03-the-envelope-and-the-letter/README.md) | encryption protects the envelope, not the letter | `envelope-and-letter.png` |
| 4 | [A land register for charging stations](04-a-land-register/README.md) | the land register | `register.png`, `owners.png` |
| 5 | [One fingerprint for a million charging points](05-one-fingerprint/README.md) | fingerprints in a family tree | `fingerprint.png`, `fingerprint-tree.png` |
| 6 | [A proof that fits in your pocket](06-a-proof-in-your-pocket/README.md) | one page of a sealed book | `phone-proof.png`, `trust-anchor.png` |
| 7 | [The signed price tag and the notary's stamp](07-signed-price-tags/README.md) | a receipt, a notary's stamp | `timeline-evidence.png` |
| 8 | [The atlas and the traffic report](08-the-atlas-and-the-traffic-report/README.md) | atlas vs. traffic report | `atlas-and-traffic.png` |
| 9 | [No master key, no single boss](09-no-master-key/README.md) | a vault with two of three keys, new locks | `vault.png`, `epochs.png` |
| 10 | [Bridges, not bulldozers](10-bridges-not-bulldozers/README.md) | bridges | `bridges.png`, `recap.png` |

Every post's README has the text to post, the first comment, the visuals with their alt texts, a section **For the
experts** that maps the analogies to the [specification](../SPECIFICATION.md) for answering comments, and **Check
before posting** with what to verify on the day.

Recurring figures: **Anna**, a driver (parts 1, 6, 7); **DE\*GEF**, the charging station operator, and the roaming
network **QA**, both from the [examples](../examples/README.md).

### Posting

- One part a week, or two (e.g. Tuesday and Thursday mornings); part 0 first, it names the route.
- The text goes into the post as it is; the README renders it with Markdown lists, which paste as `- ` lines.
  Each text is under 2,200 characters (LinkedIn allows 3,000), so it shows "…see more" after the first two lines –
  they are the hook.
- Links go into the first comment, not the post.
- Images are 1080 × 1350 (4:5), the largest LinkedIn shows in the feed. Add the alt text from the README.
  Parts with two images: post both, the first one is the cover.
- When a later part is out, edit the earlier one's first comment to link it.

### Visuals

The SVGs are written by [_tools/visuals.py](_tools/visuals.py), one function per visual, all in one frame; the PNGs
are rendered from them with headless Chrome or Edge, with a profile of their own.

```bash
python D:\Coding\OpenChargingCloud\RoamingHubCLI\libs\WWCP_POI\stories\_tools\visuals.py
```

```bash
powershell -ExecutionPolicy Bypass -File D:\Coding\OpenChargingCloud\RoamingHubCLI\libs\WWCP_POI\stories\_tools\render.ps1
```

Both take names to do only some, e.g. `register vault`. Edit the SVG by hand only if you won't run `visuals.py`
again; it overwrites them.
