# Rationale

*Why this repository records more than code, and what the additional records mean.*

This file explains the purpose of three things you will find in this
repository that are not source code: provenance trailers on commits, the
decision log in `DECISIONS.md`, and the generated "Use of generative AI"
section in `README.md`. It is written for contributors, for users who want
to judge whether this project is worth depending on, and for the reviewers
at [NLnet](https://nlnet.nl), who fund this work.

The short version: in a world where source text is cheap to produce, the
text is no longer the contribution. The contribution is the knowledge of
*why* the text is the way it is, and *why it is not something else*. This
repository tries to record that knowledge with the same care it applies to
the code, and to be honest about where generative AI tools were involved.
There are three independent reasons for doing so.

---

## 1. The legal reason: what may be published as free software at all

This project is released under a free and open source licence. A licence
is a grant of rights by a copyright holder, and copyright in the EU arises
only from a human author's own intellectual creation. A study commissioned
by the European Parliament's Committee on Legal Affairs concludes that
purely AI-generated outputs — produced without substantial human
intervention — are not eligible for copyright protection in the EU and
fall into the public domain. [1] Text with no author cannot be licensed by
anyone; it can be used by anyone, but it also carries no copyleft, no
attribution requirement, and no warranty disclaimer that binds.

There is a second, more practical risk: a code assistant may reproduce
material from its training data under a licence that is incompatible with
this project's. The responsibility for checking that lies with us, not
with the tool. [2]

**What follows from this.** For every piece of text in this repository we
want to be able to show either that a human originated it, or that a human
substantially transformed or deliberately selected it. That is the purpose
of the *provenance layer*: commit trailers that name the tool and model
involved and the role it played, and the rule that AI-originated content
is committed raw, in its own commit, before a human edits it. The record
is kept in git itself, because git already tracks every change over the
life of the project and already knows which commit last touched any line.

We are not lawyers and this file is not legal advice. The references at
the end point to sources that discuss the question in more depth.

---

## 2. The engineering reason: a program is a theory, not a text

In 1985 Peter Naur argued that programming is not primarily the production
of source text but the building, in the minds of the programmers, of a
*theory* of the problem: what it is, why the solution has this shape, what
would break it, and how it should be extended. [3] The source text is a
lossy projection of that theory. A team that holds the theory can revive a
codebase that has been dead for years; a team that holds only the text
cannot, and will slowly destroy it with changes that are each locally
reasonable and globally wrong.

For forty years this was a philosophical point, because writing the text
was expensive enough to be mistaken for the work. Generative AI has
removed that confusion. Text is now close to free. What remains scarce is
exactly what Naur described: the knowledge of why — and, more importantly,
the knowledge of *why not*. The most expensive decisions in a project are
the features that were deliberately not built, the designs that were
benchmarked and discarded, and the suggestions that were rejected because
they would violate a constraint that is not visible in the code. None of
these leave a trace in a diff.

**What follows from this.** The *decision log* in `DECISIONS.md` records
decisions that constrain this codebase: things a future contributor — or a
future AI tool — would plausibly propose to undo. It records what was
decided, what was rejected, and what each decision rests on. It is
append-only and dated, so that the reasoning of the project can be read
in order, like the code's history. Where a decision has a natural home in
the commit that implemented it, the reasoning lives in that commit's
message instead, and the log links to it.

This is the part of the repository that distinguishes an open source
project from a code dump. A dump is forkable in law and unforkable in
practice. A project that transmits its theory can be taken over, argued
with, and improved by people who were never in the room.

---

## 3. The funding reason: accountability to NLnet

This project is funded by NLnet. NLnet's policy on generative AI [4] rests
on three principles that predate the technology and that we share:

1. **Free and open licensing.** Everything delivered must be publishable
   under a recognised free licence — which brings us back to section 1.
2. **No misrepresentation.** Grantees must not present generated content
   as their own human-authored work, just as they must not present
   someone else's work as their own.
3. **Project quality.** Tools may assist, but human contributors remain
   responsible for correctness, clarity and reproducibility, and are
   expected to understand and be able to explain their design and code
   decisions — which brings us back to section 2.

The policy asks for a broad description of how generative AI is used,
typically in the README, and for provenance of generated content per
contribution, typically in commit messages. It explicitly accepts
alternative methods with equivalent results.

**What follows from this.** The generated section in `README.md` is our
broad description. It is produced by a script from the commit trailers,
so it is a snapshot of the repository as it is now rather than a
statement that was once true. It reports how much of the current code was
last touched by commits with and without AI involvement, in which roles,
and in which areas no AI tool was used at all. At each funding milestone
we add a few hand-written notes: the decisions that required domain
knowledge, and the AI suggestions that were rejected and why. Those notes
are the evidence for principle 3; the trailers are the evidence for
principles 1 and 2.

We do more than the policy's minimum because the minimum — one sentence
saying that AI was used for documentation and tests — would tell nobody
anything they could check.

---

## What we do not claim

**There is no clean boundary between human and AI work.** A feature may be
designed and implemented by hand, reviewed by two different models for
conflicts and scaling problems, and shipped with tests that a model
scaffolded from the specification and a human verified line by line. We
record the *roles* that tools played, not a fiction of separable
authorship.

**We do not measure "influence".** A model that finds a rare bug with a
throwaway implementation that is never committed has influenced the
result significantly and left no text behind. We note such uses in the
commit they informed, and we count them, but we do not pretend that a
percentage can be attached to them.

**Model identifiers are as reported by the tools.** Models change during
a project, are sometimes selected by the tool rather than the user, and
several providers may be consulted for second and third opinions on the
same question. We record what the tool reported at the time. Where a tool
did not report a model, only the tool is named. Exact identity matters
most where generated text was committed verbatim, and in those cases it
is always known.

---

## The one rule for every "why"

A rationale is only worth recording if it rests on something outside the
conversation in which it was written: a clause of a standard, a
measurement, a field incident, a discussion at a standards body, a
counterexample. "Rejected because the specification requires the meter's
timestamp to be authoritative" can be checked by anyone. "Rejected for
better maintainability" cannot, regardless of whether a human or a model
wrote it. Entries that do not name their grounds are removed.

This rule is what makes the distinction between human domain knowledge
and generated plausibility visible without anyone having to assert it. A
model can write a confident rationale for anything. It cannot cite the
meeting where the constraint was agreed, or the deployment where the
alternative failed.

---

## Where to find each record

| Record | Location | Maintained by |
|---|---|---|
| Provenance of committed text | Commit trailers (`AI-Tool`, `AI-Model`, `AI-Role`, …) and raw commits for generated content | Whoever writes the commit; enforced by a `commit-msg` hook |
| Reasoning behind a specific change | Body of the commit that made it | The author |
| Decisions that constrain the codebase, including what was not built | `DECISIONS.md` | Maintainers, append-only |
| Broad description of AI use, current snapshot | `README.md`, section "Use of generative AI" | Generated in CI from the trailers |
| Milestone notes: domain decisions, rejected suggestions | `README.md`, same section, hand-written part | Maintainers, per milestone |
| Rules for contributors | `CONTRIBUTING.md` | Maintainers |

---

## References

[1] European Parliament, Committee on Legal Affairs, *Generative AI and
Copyright*, 2025, p. 93 — the finding that purely AI-generated outputs are
not eligible for copyright protection in the EU.
https://www.europarl.europa.eu/RegData/etudes/STUD/2025/774095/IUST_STU(2025)774095_EN.pdf

[2] Free Software Foundation Europe, *Copyrightability of LLM-generated
code*, Legal Column, August 2026.
https://fsfe.org/news/2026/news-20260825-01.html

[3] Peter Naur, "Programming as Theory Building", *Microprocessing and
Microprogramming* 15 (1985), pp. 253–261. Reprinted in *Computing: A
Human Activity* (1992).

[4] NLnet Foundation, *Policy on the use of Generative Artificial
Intelligence for NLnet-funded projects*, version 1.1, January 2026.
https://nlnet.nl/foundation/policies/generativeAI/
