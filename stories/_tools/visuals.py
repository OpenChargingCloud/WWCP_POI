#!/usr/bin/env python3
"""
The visuals of the series "Can you trust this charging station?", written as SVG.

    python visuals.py        writes ../NN-*/<name>.svg
    render.ps1               renders every SVG to a PNG next to it

Every visual is 1080 x 1350 (4:5, what LinkedIn shows largest in the feed) and shares
one frame: the series label, a title, the illustration, the footer.
"""

import hashlib
import math
import os
import random
from xml.sax.saxutils import escape

W, H = 1080, 1350
L, R = 72, 1008                                   # the side margins

C = dict(
    bg     = "#F6F4EF",  navy   = "#14263B",  ink    = "#2A3B4F",  gray   = "#6B7A88",
    muted  = "#A9B4BE",  line   = "#D8D2C5",  panel  = "#EDE8DD",  white  = "#FFFFFF",
    teal   = "#0E9F9A",  tealbg = "#E3F4F3",
    green  = "#2E9E5B",  greenbg= "#E6F4EA",
    red    = "#D9483B",  redbg  = "#FBE8E5",
    amber  = "#E39B2D",  amberdk= "#A9670A",  amberbg= "#FCF1DE",
    purple = "#7B61FF",  blue   = "#3A86FF",  bronze = "#9C7A3C",  steel  = "#8E9BA8",
)

FONT = "'Segoe UI', 'Helvetica Neue', Arial, sans-serif"
MONO = "Consolas, 'Courier New', monospace"

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)


# ---------------------------------------------------------------------------------------------
# Primitives
# ---------------------------------------------------------------------------------------------

def t(x, y, s, size=28, weight=400, fill=C["navy"], anchor="start", family=FONT, ls=None, extra=""):
    spacing = f' letter-spacing="{ls}"' if ls is not None else ""
    return (f'<text x="{x:.1f}" y="{y:.1f}" font-family="{family}" font-size="{size}" font-weight="{weight}" '
            f'fill="{fill}" text-anchor="{anchor}"{spacing}{extra}>{escape(s)}</text>')

def spans(x, y, parts, size=28, anchor="start", family=FONT):
    """One line of differently styled pieces: parts = [(text, weight, fill), …]."""
    inner = "".join(f'<tspan font-weight="{w}" fill="{f}">{escape(s)}</tspan>' for s, w, f in parts)
    return (f'<text x="{x:.1f}" y="{y:.1f}" font-family="{family}" font-size="{size}" '
            f'text-anchor="{anchor}">{inner}</text>')

def rect(x, y, w, h, rx=0, fill="none", stroke="none", sw=0, extra=""):
    return (f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" rx="{rx}" fill="{fill}" '
            f'stroke="{stroke}" stroke-width="{sw}"{extra}/>')

def circle(cx, cy, r, fill="none", stroke="none", sw=0, extra=""):
    return f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{r:.1f}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"{extra}/>'

def line(x1, y1, x2, y2, stroke=C["navy"], sw=3, extra=""):
    return (f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" stroke="{stroke}" '
            f'stroke-width="{sw}" stroke-linecap="round"{extra}/>')

def path(d, fill="none", stroke="none", sw=0, extra=""):
    return f'<path d="{d}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" stroke-linecap="round" stroke-linejoin="round"{extra}/>'

def g(content, transform=""):
    tr = f' transform="{transform}"' if transform else ""
    return f"<g{tr}>{content}</g>"

def arrow(x1, y1, x2, y2, color=C["gray"], sw=5, head=18):
    a  = math.atan2(y2 - y1, x2 - x1)
    bx, by = x2 - head * math.cos(a), y2 - head * math.sin(a)
    p1 = (bx + head * 0.55 * math.sin(a), by - head * 0.55 * math.cos(a))
    p2 = (bx - head * 0.55 * math.sin(a), by + head * 0.55 * math.cos(a))
    return (line(x1, y1, bx, by, color, sw) +
            f'<polygon points="{x2:.1f},{y2:.1f} {p1[0]:.1f},{p1[1]:.1f} {p2[0]:.1f},{p2[1]:.1f}" fill="{color}"/>')

def curved_arrow(x1, y1, cx, cy, x2, y2, color=C["gray"], sw=5, head=18):
    a  = math.atan2(y2 - cy, x2 - cx)
    bx, by = x2 - head * math.cos(a), y2 - head * math.sin(a)
    p1 = (bx + head * 0.55 * math.sin(a), by - head * 0.55 * math.cos(a))
    p2 = (bx - head * 0.55 * math.sin(a), by + head * 0.55 * math.cos(a))
    return (path(f"M{x1},{y1} Q{cx},{cy} {bx:.1f},{by:.1f}", stroke=color, sw=sw) +
            f'<polygon points="{x2:.1f},{y2:.1f} {p1[0]:.1f},{p1[1]:.1f} {p2[0]:.1f},{p2[1]:.1f}" fill="{color}"/>')


# ---------------------------------------------------------------------------------------------
# Icons
# ---------------------------------------------------------------------------------------------

def check_badge(cx, cy, r, color=C["green"]):
    d = f"M{cx - .42*r:.1f},{cy + .02*r:.1f} L{cx - .1*r:.1f},{cy + .34*r:.1f} L{cx + .46*r:.1f},{cy - .34*r:.1f}"
    return circle(cx, cy, r, color) + path(d, stroke=C["white"], sw=max(3, r * .22))

def cross_badge(cx, cy, r, color=C["red"]):
    k = .32 * r
    return (circle(cx, cy, r, color) + line(cx - k, cy - k, cx + k, cy + k, C["white"], max(3, r * .22)) +
            line(cx - k, cy + k, cx + k, cy - k, C["white"], max(3, r * .22)))

def question_badge(cx, cy, r, color=C["amber"]):
    return circle(cx, cy, r, color) + t(cx, cy + r * .42, "?", r * 1.25, 700, C["white"], "middle")

def seal(cx, cy, r, color, label="", label_size=None):
    pts = []
    for i in range(48):
        a  = 2 * math.pi * i / 48
        rr = r if i % 2 == 0 else r * .9
        pts.append(f"{cx + rr * math.cos(a):.1f},{cy + rr * math.sin(a):.1f}")
    size = label_size or r * (.62 if len(label) <= 2 else .5 if len(label) == 3 else .4)
    return (f'<polygon points="{" ".join(pts)}" fill="{color}"/>' +
            circle(cx, cy, r * .7, stroke=C["white"], sw=max(1.5, r * .06), extra=' stroke-opacity=".75"') +
            (t(cx, cy + size * .36, label, size, 700, C["white"], "middle") if label else ""))

def charger(x, y, s=1.0, body=C["navy"], screen=C["tealbg"], bolt=C["teal"]):
    """A charging station, 150 x 200 at s = 1, top left at (x, y)."""
    return g(
        rect(0, 0, 110, 180, 14, body) +
        rect(16, 18, 78, 54, 7, screen) +
        path("M58,24 L42,48 L55,48 L50,66 L68,40 L55,40 Z", bolt) +
        rect(30, 92, 50, 8, 4, C["muted"]) + rect(30, 108, 34, 8, 4, C["muted"]) +
        rect(-10, 178, 130, 16, 5, body) +
        path("M110,70 C150,78 150,130 140,160", stroke=body, sw=8) +
        rect(128, 156, 24, 34, 6, body),
        f"translate({x},{y}) scale({s})")

def phone(x, y, w, h, frame=C["navy"], screen=C["white"]):
    rx = min(40, w * .16)
    b  = max(5, min(14, w * .09))
    nw = min(80, w * .3)
    return (rect(x, y, w, h, rx, frame) + rect(x + b, y + b, w - 2 * b, h - 2 * b, max(2, rx - b / 2), screen) +
            rect(x + w / 2 - nw / 2, y + b + min(10, h * .03), nw, max(4, min(12, w * .06)), 3, frame))

def glyph_badge(cx, cy, r, glyph, color=C["amber"]):
    return circle(cx, cy, r, color) + t(cx, cy + r * .5, glyph, r * 1.6, 700, C["white"], "middle")

def padlock(cx, cy, s=1.0, color=C["navy"]):
    return g(path("M-12,-4 V-14 A12,12 0 0 1 12,-14 V-4", stroke=color, sw=5) +
             rect(-17, -6, 34, 26, 5, color) + circle(0, 6, 4, C["white"]), f"translate({cx},{cy}) scale({s})")

def envelope(x, y, w, h, stroke=C["navy"], lock=True):
    return (rect(x, y, w, h, 6, C["white"], stroke, 3) +
            path(f"M{x + 3},{y + 3} L{x + w / 2},{y + h * .55} L{x + w - 3},{y + 3}", stroke=stroke, sw=3) +
            (padlock(x + w - 2, y + h - 4, .8, stroke) if lock else ""))

def letter(x, y, w, h, line_colors, sealed=None):
    out = rect(x, y, w, h, 4, C["white"], C["gray"], 2)
    for i, col in enumerate(line_colors):
        ly = y + 14 + i * 11
        out += line(x + 9, ly, x + w - (16 if i % 2 else 9), ly, col, 4)
    if sealed:
        out += seal(x + w - 8, y + h - 8, 15, sealed)
    return out

def server(x, y, w=80, h=100, color=C["ink"]):
    out = ""
    for i in range(3):
        yy = y + i * (h / 3)
        out += rect(x, yy, w, h / 3 - 6, 7, color) + circle(x + 16, yy + (h / 3 - 6) / 2, 4.5, C["teal"])
        out += rect(x + 30, yy + (h / 3 - 6) / 2 - 2.5, w - 44, 5, 2.5, C["steel"])
    return out

def meter(x, y, s=1.0):
    """An energy meter, 120 x 150 at s = 1."""
    return g(rect(0, 0, 120, 150, 12, C["white"], C["navy"], 5) +
             rect(14, 18, 92, 46, 6, C["navy"]) +
             t(60, 50, "0042.7", 24, 700, "#7CF0B5", "middle", MONO) +
             t(60, 92, "kWh", 22, 700, C["navy"], "middle") +
             circle(30, 122, 7, C["teal"]) + rect(48, 117, 54, 10, 5, C["line"]),
             f"translate({x},{y}) scale({s})")

def key(x, y, s=1.0, color=C["navy"], angle=0):
    return g(circle(0, 0, 16, stroke=color, sw=8) + rect(14, -4, 52, 8, 3, color) +
             rect(50, 2, 7, 14, 2, color) + rect(60, 2, 6, 10, 2, color),
             f"translate({x},{y}) rotate({angle}) scale({s})")

def person(cx, cy, s=1.0, color=C["red"]):
    """A person, head centre at (cx, cy)."""
    return g(circle(0, 0, 24, color) + path("M-44,74 C-44,36 -22,28 0,28 C22,28 44,36 44,74 Z", color),
             f"translate({cx},{cy}) scale({s})")

def laptop(x, y, w=140, h=90, color=C["navy"]):
    return (rect(x, y, w, h, 8, color) + rect(x + 8, y + 8, w - 16, h - 16, 4, C["white"]) +
            path(f"M{x - 16},{y + h + 4} H{x + w + 16} L{x + w + 6},{y + h + 16} H{x - 6} Z", color))

def document(x, y, w, h, rows=4, stroke=C["navy"], sealed=None):
    out = rect(x, y, w, h, 6, C["white"], stroke, 3)
    for i in range(rows):
        ly = y + 22 + i * 18
        out += line(x + 14, ly, x + w - (30 if i % 2 else 14), ly, C["muted"], 5)
    if sealed:
        out += seal(x + w - 10, y + h - 10, 22, sealed)
    return out

def pencil(x, y, s=1.0, color=C["red"]):
    return g(path("M0,0 L36,-36 L46,-26 L10,10 Z", color) + path("M0,0 L10,10 L-4,14 Z", C["navy"]),
             f"translate({x},{y}) scale({s})")

def pill(x, y, label, fill, color, w=None, size=20):
    w = w or (len(label) * size * .72 + 36)
    return rect(x, y, w, 40, 20, fill) + t(x + w / 2, y + 27, label, size, 700, color, "middle", ls=2)

def fingerprint(cx, cy, s=1.0, color=C["teal"]):
    out = ""
    for i in range(6):
        r  = 10 + i * 9
        da = f"{r * 1.4:.0f} {r * .35:.0f}"
        out += (f'<ellipse cx="0" cy="{i * 1.5:.1f}" rx="{r * .82:.1f}" ry="{r:.1f}" fill="none" stroke="{color}" '
                f'stroke-width="5" stroke-linecap="round" stroke-dasharray="{da}" '
                f'transform="rotate({-12 + i * 9})"/>')
    return g(out, f"translate({cx},{cy}) scale({s})")

def node(cx, cy, w, h, label, kind):
    fill, stroke, ink = {
        "green": (C["greenbg"], C["green"], C["green"]),
        "red":   (C["redbg"],   C["red"],   C["red"]),
        "teal":  (C["teal"],    C["teal"],  C["white"]),
        "gray":  (C["panel"],   C["line"],  C["gray"]),
        "navy":  (C["white"],   C["navy"],  C["navy"]),
    }[kind]
    return (rect(cx - w / 2, cy - h / 2, w, h, 12, fill, stroke, 3) +
            t(cx, cy + h * .17, label, h * .46, 700, ink, "middle", MONO))

def short_hash(text, n=16):
    hx = hashlib.sha256(text.encode("utf-8")).hexdigest().upper()[:n]
    return " ".join(hx[i:i + 4] for i in range(0, n, 4))


# ---------------------------------------------------------------------------------------------
# The frame
# ---------------------------------------------------------------------------------------------

def frame(part, title_lines, body, subtitle=None, title_size=64, name=""):
    label = "OVERVIEW" if part == 0 else "RECAP" if part == 11 else f"PART {part} / 10"
    out  = rect(0, 0, W, H, 0, C["bg"])
    out += t(L, 96, "CAN YOU TRUST THIS CHARGING STATION?", 22, 600, C["gray"], ls=3)
    out += t(R, 96, label, 22, 700, C["teal"], "end", ls=3)
    out += rect(L, 118, 64, 6, 3, C["teal"])
    lh   = title_size * 1.18
    for i, tl in enumerate(title_lines):
        out += t(L, 200 + i * lh, tl, title_size, 700, C["navy"])
    if subtitle:
        out += t(L, 200 + (len(title_lines) - 1) * lh + 58, subtitle, 30, 400, C["ink"])
    out += body
    out += line(L, 1258, R, 1258, C["line"], 2)
    out += spans(L, 1304, [("WWCP_POI", 700, C["navy"]), ("  ·  open source", 400, C["gray"])], 24)
    out += t(R, 1304, "github.com/OpenChargingCloud/WWCP_POI", 22, 400, C["gray"], "end")
    title = " ".join(title_lines)
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">\n'
            f"<title>{escape(title)}</title>\n{out}\n</svg>\n")


# ---------------------------------------------------------------------------------------------
# 00 / 10 – the route
# ---------------------------------------------------------------------------------------------

STOPS = [
    ("Anna's invoice",              "Why a screenshot is no proof"),
    ("The meter's own ID card",     "Correct kWh, unverifiable key"),
    ("The envelope and the letter", "What encryption does not protect"),
    ("A land register",             "Every change a signed entry"),
    ("One fingerprint",             "A million records, one comparison"),
    ("A proof in your pocket",      "One station, checked offline"),
    ("Signed price tags",           "Evidence that holds for months"),
    ("The atlas & the traffic report", "Slow data vs. fast data"),
    ("No master key",               "Two of three, no single boss"),
    ("Bridges, not bulldozers",     "Works with OCPI & DATEX II"),
]

def smooth_path(pts):
    d = f"M{pts[0][0]:.1f},{pts[0][1]:.1f}"
    for i in range(len(pts) - 1):
        p0 = pts[max(i - 1, 0)]; p1 = pts[i]; p2 = pts[i + 1]; p3 = pts[min(i + 2, len(pts) - 1)]
        c1 = (p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6)
        c2 = (p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6)
        d += f" C{c1[0]:.1f},{c1[1]:.1f} {c2[0]:.1f},{c2[1]:.1f} {p2[0]:.1f},{p2[1]:.1f}"
    return d

def route(done):
    ys   = [432 + i * 82 for i in range(10)]
    xs   = [190 + 62 * math.sin(i * math.pi / 2) for i in range(10)]
    pts  = [(190, 394)] + list(zip(xs, ys)) + [(190, 1212)]
    d    = smooth_path(pts)
    body = path(d, stroke=C["line"], sw=50) + path(d, stroke="#3B4A5A", sw=34)
    body += path(d, stroke=C["white"], sw=3, extra=' stroke-dasharray="16 14"')
    for i, ((title, hint), x, y) in enumerate(zip(STOPS, xs, ys)):
        col   = C["green"] if done else C["teal"]
        body += circle(x, y, 27, col, C["white"], 5)
        body += t(x, y + 9, str(i + 1), 26, 700, C["white"], "middle")
        body += t(330, y - 2, title, 30, 600, C["navy"])
        body += t(330, y + 28, hint, 22, 400, C["gray"])
        if done:
            body += check_badge(975, y + 4, 17)
    if done:
        return frame(11, ["Ten stops.", "One idea."], body,
                     subtitle="Charging data that anyone can check.")
    return frame(0, ["Can you trust this", "charging station?"], body,
                 subtitle="A series in ten stops. No crypto degree required.")


# ---------------------------------------------------------------------------------------------
# 01 – Anna's invoice
# ---------------------------------------------------------------------------------------------

def invoice():
    b  = phone(100, 390, 330, 540)
    cx = 265
    b += t(cx, 470, "Charging", 22, 600, C["gray"], "middle", ls=2)
    b += t(cx, 528, "Rest area A9", 30, 700, C["navy"], "middle")
    b += t(cx, 562, "Fast charger 3 · CCS", 22, 400, C["gray"], "middle")
    b += rect(140, 594, 250, 140, 18, C["tealbg"])
    b += t(cx, 676, "39 ct", 64, 700, C["teal"], "middle")
    b += t(cx, 712, "per kWh", 22, 600, C["teal"], "middle")
    b += t(cx, 790, "40.0 kWh charged", 24, 600, C["navy"], "middle")
    b += rect(150, 826, 230, 56, 28, C["teal"])
    b += t(cx, 862, "Session ended", 22, 600, C["white"], "middle")
    b += t(cx, 978, "At the charger", 26, 700, C["ink"], "middle")
    b += t(cx, 1010, "12 Oct, 12:03", 22, 400, C["gray"], "middle")

    inv  = rect(8, 10, 330, 480, 6, "#000000", extra=' opacity=".07"')
    inv += rect(0, 0, 330, 480, 6, C["white"], C["line"], 2)
    inv += t(30, 64, "INVOICE", 30, 700, C["navy"], ls=3)
    inv += t(30, 96, "No. 2026-11-0417", 20, 400, C["gray"])
    inv += line(30, 124, 300, 124, C["line"], 2)
    inv += t(30, 172, "Charging session", 24, 600, C["navy"])
    inv += t(30, 204, "12 Oct · Rest area A9", 20, 400, C["gray"])
    inv += spans(30, 268, [("40.0 kWh × ", 400, C["navy"]), ("0.79 €", 700, C["red"])], 26)
    inv += line(30, 312, 300, 312, C["line"], 2)
    inv += t(30, 364, "Total", 26, 600, C["navy"])
    inv += t(300, 364, "31.60 €", 34, 700, C["red"], "end")
    inv += rect(30, 410, 150, 12, 6, C["panel"]) + rect(30, 434, 210, 12, 6, C["panel"])
    b   += g(inv, "translate(650,400) rotate(4)")
    b   += t(815, 978, "Six weeks later", 26, 700, C["ink"], "middle")
    b   += t(815, 1010, "23 Nov", 22, 400, C["gray"], "middle")

    b += question_badge(538, 660, 48)

    b += rect(L, 1050, R - L, 176, 22, C["white"], C["line"], 2)
    b += t(104, 1096, "What Anna can prove", 26, 700, C["navy"])
    rows = [((122, 1146), check_badge, "kWh", "calibrated meter"),
            ((122, 1196), check_badge, "Time", "signed by the meter"),
            ((572, 1146), question_badge, "Station", "“the one at the rest area”"),
            ((572, 1196), question_badge, "Price", "a screenshot, maybe")]
    for (x, y), badge, what, how in rows:
        b += badge(x, y - 8, 17)
        b += t(x + 30, y, what, 24, 700, C["navy"])
        b += t(x + 130, y, how, 24, 400, C["ink"])
    return frame(1, ["39 ct at the charger.", "79 ct on the invoice."], b,
                 subtitle="Which one is true – and who can prove it?")


# ---------------------------------------------------------------------------------------------
# 02 – the meter's own ID card
# ---------------------------------------------------------------------------------------------

def evidence_chain():
    cards = [("Energy",           "kWh measured by a calibrated meter", True),
             ("Time",             "timestamp signed by the meter",      True),
             ("Meter key",        "often sent along with the data",     False),
             ("Charging station", "is it the one it claims to be?",     False),
             ("Operator",         "who is responsible for it?",         False),
             ("Tariff",           "which price applied?",               False)]
    x, w, h, step, top = L, 608, 96, 132, 400
    b = ""
    for i in range(len(cards) - 1):
        y   = top + i * step
        col = C["red"] if i == 1 else C["steel"]
        b  += rect(x + w / 2 - 11, y + h - 18, 22, step - h + 36, 11, stroke=col, sw=6)
        if i == 1:
            my = y + h + (step - h) / 2
            b += g(rect(-26, -5, 52, 10, 0, C["bg"]), f"translate({x + w / 2},{my}) rotate(-24)")
    for i, (title, desc, ok) in enumerate(cards):
        y  = top + i * step
        b += rect(x, y, w, h, 18, C["white"], "#BFE3CC" if ok else "#F1D3A1", 3)
        b += (check_badge if ok else question_badge)(x + 48, y + h / 2, 22)
        b += t(x + 92, y + 42, title, 30, 700, C["navy"])
        b += t(x + 92, y + 76, desc, 22, 400, C["gray"])

    def bracket(y1, y2, color):
        return path(f"M712,{y1} q16,0 16,16 V{y2 - 16} q0,16 -16,16", stroke=color, sw=4)
    b += bracket(top, top + step + h, C["green"])
    b += t(752, 506, "Covered by", 26, 700, C["green"])
    b += t(752, 540, "metrology law", 26, 700, C["green"])
    b += t(752, 572, "MID · Eichrecht", 20, 400, C["gray"])
    y1 = top + 2 * step
    b += bracket(y1, top + 5 * step + h, C["amber"])
    b += t(752, 870, "Needs", 26, 700, C["amberdk"])
    b += t(752, 904, "authentic", 26, 700, C["amberdk"])
    b += t(752, 938, "POI data", 26, 700, C["amberdk"])
    return frame(2, ["Metrology secures", "two links of the chain."], b,
                 subtitle="A proof is only as strong as its weakest link.")

def key_swap():
    b  = rect(L, 336, R - L, 420, 24, C["white"], C["line"], 2)
    b += pill(104, 364, "TODAY", C["panel"], C["gray"])
    b += meter(110, 450, .9)
    b += arrow(232, 518, 300, 518)
    b += document(312, 452, 100, 128, 4, C["navy"])
    b += key(368, 566, .55, C["navy"], -20)
    b += arrow(430, 518, 492, 518)
    b += person(548, 486, 1.0, C["red"])
    b += pencil(588, 470, .55)
    b += arrow(604, 518, 664, 518)
    b += laptop(684, 470)
    b += check_badge(754, 515, 26)
    b += t(170, 640, "Meter signs", 22, 600, C["ink"], "middle")
    b += t(362, 640, "values + key", 22, 600, C["ink"], "middle")
    b += t(548, 640, "changes values,", 22, 600, C["red"], "middle")
    b += t(548, 668, "swaps key, re-signs", 22, 600, C["red"], "middle")
    b += t(754, 640, "Transparency", 22, 600, C["ink"], "middle")
    b += t(754, 668, "software", 22, 600, C["ink"], "middle")
    b += t(912, 510, "Valid.", 30, 700, C["green"], "middle")
    b += t(912, 546, "But forged.", 26, 700, C["red"], "middle")
    b += t(104, 726, "Values and key travel together – whoever changes one can change both.", 22, 400, C["ink"])

    b += rect(L, 784, R - L, 446, 24, C["white"], C["line"], 2)
    b += pill(104, 812, "WITH WWCP_POI", C["teal"], C["white"])
    b += meter(118, 878, .75)
    b += arrow(222, 935, 300, 935)
    b += document(312, 880, 96, 112, 4, C["navy"])
    b += arrow(424, 935, 492, 935)
    b += person(548, 906, .9, C["red"])
    b += pencil(584, 892, .5)
    b += arrow(600, 950, 672, 1000)
    b += document(312, 1040, 96, 112, 4, C["teal"], sealed=C["teal"])
    b += key(338, 1128, .45, C["teal"], -20)
    b += t(290, 1088, "Station record,", 22, 600, C["ink"], "end")
    b += t(290, 1116, "signed by", 20, 400, C["gray"], "end")
    b += t(290, 1140, "the operator", 20, 400, C["gray"], "end")
    b += curved_arrow(430, 1096, 560, 1110, 672, 1060, C["teal"])
    b += laptop(684, 990)
    b += cross_badge(754, 1035, 26)
    b += t(912, 1012, "Key", 28, 700, C["red"], "middle")
    b += t(912, 1046, "mismatch.", 28, 700, C["red"], "middle")
    b += t(912, 1080, "Rejected.", 24, 600, C["ink"], "middle")
    b += t(104, 1200, "The key comes from the signed register, on a separate path.", 22, 400, C["ink"])
    return frame(2, ["A seal is only as good as", "where its key comes from."], b, title_size=58)


# ---------------------------------------------------------------------------------------------
# 03 – the envelope and the letter
# ---------------------------------------------------------------------------------------------

def envelope_and_letter():
    xs     = [150, 400, 650, 900]
    labels = ["Operator", "Roaming platform", "Aggregator", "Your app"]

    def row(top, sealed):
        cy  = top + 120
        out = charger(xs[0] - 42, cy - 62, .58)
        out += server(xs[1] - 40, cy - 50) + server(xs[2] - 40, cy - 50)
        out += phone(xs[3] - 36, cy - 64, 72, 128, C["navy"], C["white"])
        for i in range(3):
            mx = (xs[i] + xs[i + 1]) / 2
            out += envelope(mx - 38, cy - 4, 76, 50, C["teal"] if sealed else C["navy"])
            if sealed:
                lines = [C["navy"]] * 4
            else:
                lines = [C["navy"]] * 4 if i == 0 else \
                        [C["navy"], C["red"], C["navy"], C["navy"]] if i == 1 else \
                        [C["red"], C["red"], C["navy"], C["red"]]
            out += letter(mx - 25, cy - 82, 50, 62, lines, C["teal"] if sealed else None)
        if not sealed:
            out += pencil(xs[1] + 30, cy - 52, .6) + pencil(xs[2] + 30, cy - 52, .6)
            out += question_badge(xs[3] + 38, cy - 58, 22)
        else:
            out += seal(xs[3], cy - 14, 22, C["teal"])
            out += check_badge(xs[3] + 38, cy - 58, 22)
        for x, lab in zip(xs, labels):
            out += t(x, cy + 108, lab, 22, 600, C["ink"], "middle")
        return out

    b  = pill(L, 380, "TODAY", C["panel"], C["gray"])
    b += t(R, 408, "Who wrote this?", 24, 700, C["amberdk"], "end")
    b += row(400, False)
    b += t(L, 690, "Every hop is encrypted – and every hop can open and rewrite the letter.", 22, 400, C["ink"])
    b += line(L, 736, R, 736, C["line"], 2)
    b += pill(L, 770, "WITH WWCP_POI", C["teal"], C["white"])
    b += t(R, 798, "Sealed by the operator", 24, 700, C["green"], "end")
    b += row(790, True)
    b += t(L, 1080, "Platforms pass the sealed letter on. Anyone can check the seal.", 22, 400, C["ink"])
    b += rect(L, 1114, R - L, 112, 20, C["panel"])
    b += t(104, 1160, "OICP, OCHP, OCPI, DATEX II: every hop is secured,", 24, 600, C["ink"])
    b += t(104, 1196, "but the data itself carries no seal of its author.", 24, 600, C["ink"])
    return frame(3, ["Encryption protects the envelope.", "A signature protects the letter."], b,
                 subtitle="What a roaming chain protects – and what it doesn't.", title_size=52)


# ---------------------------------------------------------------------------------------------
# 04 – a land register
# ---------------------------------------------------------------------------------------------

def register():
    top, bottom = 400, 1226
    b  = '<clipPath id="page"><rect x="72" y="400" width="936" height="826" rx="20"/></clipPath>'
    b += rect(L, top, R - L, bottom - top, 20, C["white"], C["line"], 2)
    b += f'<g clip-path="url(#page)">{rect(L, top, 22, bottom - top, 0, C["navy"])}</g>'
    b += t(120, 452, "NO.", 18, 700, C["gray"], ls=2)
    b += t(214, 452, "CHANGE", 18, 700, C["gray"], ls=2)
    b += t(930, 452, "SEAL", 18, 700, C["gray"], "middle", ls=2)
    rows = [("#41", "12 Oct", "Add charging location “Jena-Nord”", "by DE*GEF · charging station operator", C["teal"], "GEF"),
            ("#42", "14 Oct", "Grid connection point: 400 kVA",     "by the grid operator",                C["amber"], "GO"),
            ("#43", "20 Oct", "New tariff from 1 Nov: 0.49 €/kWh",  "by DE*GEF · charging station operator", C["teal"], "GEF"),
            ("#44", "21 Oct", "Offer for its customers: −10 %",     "by an e-mobility provider",           C["purple"], "EMP"),
            ("#45", "22 Oct", "Change DE*GEF's tariff to 0.79 €/kWh", "by a roaming platform – no seal of DE*GEF", None, "")]
    for i, (no, date, change, by, col, lab) in enumerate(rows):
        y  = 470 + i * 150
        b += line(104, y, 976, y, C["line"], 2)
        refused = col is None
        b += t(120, y + 62, no, 26, 700, C["gray"] if not refused else C["muted"], family=MONO)
        b += t(120, y + 94, date, 20, 400, C["gray"] if not refused else C["muted"])
        b += t(214, y + 62, change, 28, 600, C["navy"] if not refused else C["muted"])
        b += t(214, y + 98, by, 22, 400, C["gray"] if not refused else C["red"])
        if refused:
            b += line(214, y + 52, 690, y + 52, C["muted"], 2)
            b += g(rect(-106, -34, 212, 68, 10, C["white"], C["red"], 5) +
                   t(0, 13, "REFUSED", 36, 800, C["red"], "middle", ls=4),
                   f"translate(866,{y + 72}) rotate(-8)")
        else:
            b += seal(930, y + 72, 40, col, lab)
    return frame(4, ["Every change, an entry.", "Every entry, a seal."], b,
                 subtitle="Like a land register: nothing is overwritten.")

def owners():
    cx, cy = 540, 790
    cards = [((L, 400),   C["teal"],   "CSO", ["Charging station", "operator"],
              ["stations & charging points", "tariffs & prices", "meters & their keys"]),
             ((608, 400), C["amber"],  "GO",  ["Grid", "operator"],
              ["grid connection points", "contracted power", "meters at the grid"]),
             ((L, 950),   C["purple"], "EMP", ["E-mobility", "provider"],
              ["offers for its customers", "groups of stations", "its own keys"]),
             ((608, 950), C["blue"],   "PO",  ["Parking", "operator"],
              ["garages & spaces", "parking sensors", "parking products"])]
    b = ""
    for (x, y), *_ in cards:
        b += line(cx, cy, x + 200, y + 115, C["muted"], 3, ' stroke-dasharray="10 10"')
    for (x, y), col, lab, title, items in cards:
        b += rect(x, y, 400, 230, 22, C["white"], C["line"], 2)
        b += seal(x + 62, y + 66, 38, col, lab)
        b += t(x + 120, y + 58, title[0], 26, 700, C["navy"])
        b += t(x + 120, y + 90, title[1], 26, 700, C["navy"])
        for j, it in enumerate(items):
            b += circle(x + 40, y + 143 + j * 32, 5, col)
            b += t(x + 56, y + 150 + j * 32, it, 22, 400, C["ink"])
    b += circle(cx, cy, 124, C["navy"])
    b += seal(cx + 88, cy - 88, 30, C["navy"], "RN")
    b += t(cx, cy - 18, "Roaming", 32, 700, C["white"], "middle")
    b += t(cx, cy + 20, "network", 32, 700, C["white"], "middle")
    b += t(cx, cy + 58, "confirms the whole", 20, 400, "#C9D3DD", "middle")
    b += t(540, 1226, "A change counts only with the seal of whoever owns the data.", 24, 600, C["ink"], "middle")
    return frame(4, ["Everyone signs", "only their own pages."], b)


# ---------------------------------------------------------------------------------------------
# 05 – one fingerprint
# ---------------------------------------------------------------------------------------------

def fingerprint_card():
    a = "Charging point DE*GEF*E1*1\nCCS · 150 kW\n0.39 €/kWh"      # the card's three lines, as shown
    z = "Charging point DE*GEF*E1*1\nCCS · 150 kW\n0.38 €/kWh"
    b = ""
    for i, (price, text, col, bg) in enumerate([(("0.39", ""), a, C["teal"], C["tealbg"]),
                                                 (("0.3", "8"), z, C["red"], C["redbg"])]):
        top = 410 + i * 330
        b += rect(L, top, 440, 250, 22, C["white"], C["line"], 2)
        b += t(104, top + 56, "Charging point DE*GEF*E1*1", 24, 700, C["navy"])
        b += t(104, top + 98, "CCS · 150 kW", 26, 400, C["ink"])
        parts = [(price[0], 700, C["navy"])]
        if price[1]:
            parts.append((price[1], 700, C["red"]))
        parts.append((" €/kWh", 700, C["navy"]))
        b += spans(104, top + 196, parts, 56)
        if price[1]:
            b += t(104, top + 232, "one digit changed", 20, 600, C["red"])
        b += arrow(528, top + 125, 596, top + 125, C["gray"])
        b += rect(608, top, 400, 250, 22, bg)
        b += fingerprint(690, top + 125, 1.0, col)
        hx = short_hash(text).split(" ")
        b += t(780, top + 104, f"{hx[0]} {hx[1]}", 34, 700, col, family=MONO)
        b += t(780, top + 150, f"{hx[2]} {hx[3]}", 34, 700, col, family=MONO)
        b += t(780, top + 194, "…", 34, 700, col, family=MONO)
    b += check_badge(104, 1100, 22)
    b += t(142, 1110, "Same data → same fingerprint. Always.", 28, 600, C["navy"])
    b += glyph_badge(104, 1160, 22, "≠", C["red"])
    b += t(142, 1170, "One digit changed → a completely different one.", 28, 600, C["navy"])
    b += t(L, 1226, "The fingerprints shown are real: SHA-256 of the card's three lines, shortened.", 20, 400, C["gray"])
    return frame(5, ["Change one digit,", "get a new fingerprint."], b,
                 subtitle="Same data, same fingerprint – on every computer.")

def fingerprint_tree():
    def code(s): return short_hash(s, 4)
    b  = rect(L, 380, R - L, 160, 22, C["white"], C["line"], 2)
    b += t(110, 428, "Your copy", 22, 600, C["gray"])
    b += node(250, 482, 280, 64, "7F3A 91C2", "teal")
    b += t(450, 498, "=", 54, 700, C["navy"], "middle")
    b += t(520, 428, "My copy", 22, 600, C["gray"])
    b += node(660, 482, 280, 64, "7F3A 91C2", "teal")
    b += check_badge(856, 482, 24)
    b += t(892, 492, "Same.", 28, 700, C["green"])
    b += t(L, 606, "Different? Follow the red branch.", 32, 700, C["navy"])

    levels = [(690, "Network"), (830, "Operators"), (970, "Locations"), (1110, "Stations")]
    for y, lab in levels:
        b += t(L, y + 8, lab, 20, 600, C["gray"])
    w, h = 140, 56
    root = (620, 690)
    ops  = [(390, 830, "green"), (620, 830, "red"), (850, 830, "green")]
    locs = [(450, 970, "green"), (620, 970, "green"), (790, 970, "red")]
    sts  = [(700, 1110, "green"), (880, 1110, "red")]
    edges = [(root, o) for o in ops] + [((620, 830), l) for l in locs] + [((790, 970), s) for s in sts]
    for (x1, y1), (x2, y2, kind) in edges:
        red = kind == "red"
        b += line(x1, y1 + h / 2, x2, y2 - h / 2, C["red"] if red else C["muted"], 6 if red else 3)
    b += node(*root, w + 20, h, "A1C9", "red")
    b += t(710, 699, "≠ 7F3A", 24, 700, C["red"], family=MONO)
    for i, (x, y, kind) in enumerate(ops + locs + sts):
        b += node(x, y, w, h, code(f"{x}{y}{i}"), kind)
    b += t(880, 1182, "Found: one station", 22, 700, C["red"], "middle")
    b += t(250, 1182, "A few steps, not a million records", 22, 400, C["gray"])
    return frame(5, ["A million records?", "Compare one code."], b)


# ---------------------------------------------------------------------------------------------
# 06 – a proof in your pocket
# ---------------------------------------------------------------------------------------------

def phone_proof():
    b  = phone(84, 390, 380, 790)
    cx = 274
    b += t(cx, 470, "Rest area A9", 30, 700, C["navy"], "middle")
    b += t(cx, 504, "DE*GEF · charging point 1", 22, 400, C["gray"], "middle")
    b += line(126, 530, 422, 530, C["line"], 2)
    b += t(130, 580, "CCS · 150 kW", 28, 600, C["navy"])
    b += t(130, 640, "39 ct/kWh", 44, 700, C["teal"])
    b += t(130, 684, "Meter key  a1f3 … 09c2", 20, 400, C["gray"], family=MONO)
    b += rect(118, 716, 312, 232, 20, C["greenbg"], C["green"], 2)
    b += check_badge(168, 770, 26)
    b += t(208, 781, "Verified", 32, 700, C["green"])
    b += t(140, 832, "Data of DE*GEF,", 22, 400, C["ink"])
    b += t(140, 862, "confirmed by the", 22, 400, C["ink"])
    b += t(140, 892, "roaming network.", 22, 400, C["ink"])
    b += t(140, 926, "Checked offline.", 22, 700, C["ink"])
    for i, hgt in enumerate([10, 18, 26, 34]):
        b += rect(236 + i * 12, 1030 - hgt, 8, hgt, 2, C["muted"])
    b += line(230, 1034, 286, 990, C["red"], 4)
    b += t(274, 1060, "no signal needed", 20, 600, C["gray"], "middle")
    b += rect(144, 1074, 260, 56, 28, C["teal"])
    b += t(cx, 1110, "Start charging", 22, 600, C["white"], "middle")

    root = (770, 440)
    lvl1 = [(610, 590), (770, 590), (930, 590)]
    lvl2 = [(650, 740), (790, 740), (930, 740)]
    lvl3 = [(580, 890), (720, 890)]
    path_nodes = [root, lvl1[1], lvl2[0], lvl3[0]]
    def e(a, bb, on):
        return line(a[0], a[1] + 26, bb[0], bb[1] - 24, C["teal"] if on else C["muted"], 7 if on else 3)
    for n in lvl1: b += e(root, n, n == lvl1[1])
    for n in lvl2: b += e(lvl1[1], n, n == lvl2[0])
    for n in lvl3: b += e(lvl2[0], n, n == lvl3[0])
    b += node(root[0], root[1], 200, 60, "7F3A 91C2", "navy")
    b += seal(root[0] + 104, root[1] - 26, 30, C["navy"], "RN")
    for n in lvl1 + lvl2 + lvl3:
        on = n in path_nodes
        b += node(n[0], n[1], 112 if not on else 120, 48, "····" if not on else "data", "teal" if on else "gray")
    b += t(580, 952, "this station", 20, 700, C["teal"], "middle")

    lx, ly = 520, 1020
    b += rect(lx, ly - 18, 24, 24, 6, C["teal"])
    b += t(lx + 38, ly + 2, "In the proof: the data itself", 22, 400, C["ink"])
    b += rect(lx, ly + 30, 24, 24, 6, C["panel"], C["line"], 2)
    b += t(lx + 38, ly + 50, "Elsewhere: only fingerprints", 22, 400, C["ink"])
    b += seal(lx + 12, ly + 90, 14, C["navy"])
    b += t(lx + 38, ly + 98, "Top: signed by the roaming network", 22, 400, C["ink"])
    b += t(lx, ly + 150, "Offline · milliseconds · a network", 22, 600, C["navy"])
    b += t(lx, ly + 180, "twice as large: ~1 step more", 22, 600, C["navy"])
    return frame(6, ["One station.", "One small proof."], b,
                 subtitle="Checked on the phone – offline, in milliseconds.")

def qr(x, y, n=25, m=11, seed=7, color=C["navy"]):
    rnd = random.Random(seed)
    out = ""
    def finder(fx, fy):
        return (rect(x + fx * m, y + fy * m, 7 * m, 7 * m, 0, color) +
                rect(x + (fx + 1) * m, y + (fy + 1) * m, 5 * m, 5 * m, 0, C["white"]) +
                rect(x + (fx + 2) * m, y + (fy + 2) * m, 3 * m, 3 * m, 0, color))
    reserved = lambda i, j: (i < 8 and j < 8) or (i >= n - 8 and j < 8) or (i < 8 and j >= n - 8)
    for i in range(n):
        for j in range(n):
            if reserved(i, j):
                continue
            on = (i == 6 or j == 6) and (i + j) % 2 == 0 or rnd.random() < .46
            if on:
                out += rect(x + i * m, y + j * m, m, m, 0, color)
    return out + finder(0, 0) + finder(n - 7, 0) + finder(0, n - 7)

def trust_anchor():
    b  = '<clipPath id="card"><rect x="230" y="380" width="620" height="540" rx="28"/></clipPath>'
    b += rect(230, 380, 620, 540, 28, C["white"], C["line"], 2)
    b += f'<g clip-path="url(#card)">{rect(230, 380, 620, 74, 0, C["teal"])}</g>'
    b += t(540, 428, "TRUST ANCHOR", 24, 700, C["white"], "middle", ls=6)
    b += qr(540 - 25 * 11 / 2, 480, 25, 11)
    b += t(540, 816, "Roaming network QA", 32, 700, C["navy"], "middle")
    b += t(540, 854, "2 of 3 key holders · keys pinned", 22, 400, C["gray"], "middle")
    b += t(540, 888, "illustration", 16, 400, C["muted"], "middle")
    b += t(540, 990, "Scan once. Then your app checks every station of the network:", 24, 600, C["ink"], "middle")
    srcs = [("via a roaming", "platform", True), ("via a map", "service", True), ("via an", "attacker", False)]
    for i, (a, z, ok) in enumerate(srcs):
        x = L + i * 320
        b += rect(x, 1030, 296, 170, 22, C["white"], C["line"], 2)
        b += t(x + 28, 1076, "Station data", 20, 600, C["gray"])
        b += t(x + 28, 1116, a, 26, 700, C["navy"])
        b += t(x + 28, 1150, z, 26, 700, C["navy"])
        b += (check_badge if ok else cross_badge)(x + 240, 1130, 30)
    return frame(6, ["Trust one QR code.", "Check every station."], b)


# ---------------------------------------------------------------------------------------------
# 07 – signed price tags
# ---------------------------------------------------------------------------------------------

def price_tag(x, y):
    """A price tag, about 230 x 130, its hole at the left."""
    return g(path("M0,65 L48,0 H230 V130 H48 Z", C["white"], C["teal"], 4) +
             circle(40, 65, 9, C["bg"], C["teal"], 3) +
             t(140, 58, "39 ct", 42, 700, C["teal"], "middle") +
             t(140, 86, "per kWh", 18, 600, C["gray"], "middle") +
             t(140, 112, "12:00–12:15", 18, 600, C["gray"], "middle") +
             seal(228, 4, 24, C["teal"], "GEF"), f"translate({x},{y})")

def timeline_evidence():
    ly = 610
    b  = arrow(100, ly, 1000, ly, C["navy"], 5, 22)
    b += price_tag(76, 432)
    b += circle(176, ly, 12, C["teal"], C["white"], 4)
    b += t(176, ly + 50, "12 Oct, 12:03", 22, 700, C["navy"], "middle")
    b += t(176, ly + 80, "charging starts,", 20, 400, C["gray"], "middle")
    b += t(176, ly + 106, "price statement kept", 20, 400, C["gray"], "middle")
    for i, x in enumerate([400, 570, 740]):
        b += g(rect(-52, -40, 104, 80, 10, C["white"], C["navy"], 4) +
               t(0, -6, "CHECK-", 15, 700, C["navy"], "middle", ls=1) +
               t(0, 14, "POINT", 15, 700, C["navy"], "middle", ls=1) +
               t(0, 34, str(41 + i), 15, 700, C["gray"], "middle"),
               f"translate({x},{500}) rotate({-4 + i * 4})")
        b += seal(x - 30, 562, 14, C["navy"], "N", 14)
        b += seal(x, 562, 14, C["bronze"], "C", 14)
        b += seal(x + 30, 562, 14, C["blue"], "A", 14)
        b += circle(x, ly, 10, C["navy"], C["white"], 4)
        b += t(x, ly + 50, f"Checkpoint {41 + i}", 20, 600, C["ink"], "middle")
    b += g(rect(0, 0, 120, 150, 6, C["white"], C["line"], 2) +
           t(16, 34, "INVOICE", 18, 700, C["navy"], ls=2) +
           t(16, 90, "79 ct", 34, 700, C["red"]) +
           rect(16, 108, 80, 9, 4, C["panel"]) + rect(16, 124, 60, 9, 4, C["panel"]),
           "translate(866,410) rotate(4)")
    b += cross_badge(976, 420, 22)
    b += circle(924, ly, 12, C["red"], C["white"], 4)
    b += t(924, ly + 50, "23 Nov", 22, 700, C["navy"], "middle")
    b += t(924, ly + 80, "invoice disputed", 20, 400, C["gray"], "middle")

    leg = [(210, "N", C["navy"], "roaming network"), (420, "C", C["bronze"], "calibration authority"),
           (660, "A", C["blue"], "operators' association")]
    b += t(L, 788, "Sealed by", 20, 600, C["gray"])
    for x, lab, col, text in leg:
        b += seal(x, 781, 14, col, lab, 14)
        b += t(x + 22, 788, text, 20, 400, C["gray"])

    b += rect(L, 818, 300, 64, 16, C["teal"])
    b += t(L + 28, 857, "Anna's evidence", 24, 700, C["white"])
    b += rect(L, 868, R - L, 358, 22, C["white"], C["line"], 2)
    rows = [("Measurement",     "kWh and time, signed by the meter"),
            ("Station proof",   "this meter belongs to this station"),
            ("Price statement", "39 ct/kWh, signed by DE*GEF for 12:00–12:15"),
            ("Checkpoint",      "witnessed: the history everyone saw")]
    for i, (what, how) in enumerate(rows):
        y  = 936 + i * 62
        b += check_badge(118, y - 8, 20)
        b += t(156, y, what, 26, 700, C["navy"])
        b += t(388, y, how, 23, 400, C["ink"])
    b += t(104, 1196, "No screenshot. Nothing anyone in between could have changed.", 22, 700, C["teal"])
    return frame(7, ["A receipt that still holds", "six weeks later."], b, title_size=60)


# ---------------------------------------------------------------------------------------------
# 08 – the atlas and the traffic report
# ---------------------------------------------------------------------------------------------

def folded_map(x, y):
    out  = path(f"M{x},{y + 10} L{x + 50},{y} L{x + 100},{y + 10} L{x + 150},{y} V{y + 100} "
                f"L{x + 100},{y + 110} L{x + 50},{y + 100} L{x},{y + 110} Z", C["tealbg"], C["teal"], 4)
    out += line(x + 50, y, x + 50, y + 100, C["teal"], 3) + line(x + 100, y + 10, x + 100, y + 110, C["teal"], 3)
    out += path(f"M{x + 10},{y + 80} C{x + 40},{y + 40} {x + 90},{y + 90} {x + 140},{y + 40}",
                stroke=C["white"], sw=6)
    for px, py in [(x + 36, y + 52), (x + 112, y + 42)]:
        out += path(f"M{px},{py} c-12,-14 -12,-30 0,-30 c12,0 12,16 0,30 Z", C["navy"]) + circle(px, py - 20, 4, C["white"])
    return out

def live_waves(cx, cy):
    out = circle(cx, cy, 14, C["red"])
    for i, r in enumerate([34, 56, 78]):
        op = 1 - i * .25
        for sgn in (1, -1):
            a0, a1 = (-40, 40) if sgn == 1 else (140, 220)
            p0 = (cx + r * math.cos(math.radians(a0)), cy + r * math.sin(math.radians(a0)))
            p1 = (cx + r * math.cos(math.radians(a1)), cy + r * math.sin(math.radians(a1)))
            out += path(f"M{p0[0]:.1f},{p0[1]:.1f} A{r},{r} 0 0 1 {p1[0]:.1f},{p1[1]:.1f}",
                        stroke=C["red"], sw=6, extra=f' opacity="{op}"')
    return out

def atlas_and_traffic():
    b = ""
    cols = [(L, "SLOW DATA", "the atlas", C["teal"],
             ["Where the station is", "Connectors & power", "Meters & their keys", "Who operates it", "General tariffs"],
             "signed · versioned · provable", "changes rarely, must be exact"),
            (552, "FAST DATA", "the traffic report", C["red"],
             ["Free or occupied?", "Power available now", "Ad hoc price this minute", "Energy delivered so far"],
             "streamed · live · prices signed", "changes all the time, must be fast")]
    for x, kind, name, col, items, foot, note in cols:
        b += rect(x, 400, 456, 826, 24, C["white"], C["line"], 2)
        if kind == "SLOW DATA":
            b += folded_map(x + 40, 444)
        else:
            b += live_waves(x + 120, 500)
            b += rect(x + 220, 482, 82, 34, 17, C["red"]) + t(x + 261, 506, "LIVE", 18, 700, C["white"], "middle", ls=2)
        b += t(x + 40, 618, kind, 22, 700, C["gray"], ls=3)
        b += t(x + 40, 664, name, 38, 700, C["navy"])
        b += t(x + 40, 702, note, 20, 400, C["gray"])
        for j, it in enumerate(items):
            y  = 778 + j * 66
            b += circle(x + 50, y - 9, 8, col)
            b += t(x + 72, y, it, 26, 600, C["ink"])
        b += line(x + 40, 1134, x + 416, 1134, C["line"], 2)
        b += t(x + 40, 1180, foot, 22, 700, col)
    return frame(8, ["The atlas and", "the traffic report."], b,
                 subtitle="Slow data and fast data need different tools.")


# ---------------------------------------------------------------------------------------------
# 09 – no master key
# ---------------------------------------------------------------------------------------------

def vault():
    cx, cy, r = 540, 716, 210
    b  = circle(cx, cy, r + 14, "#B7C1CB")
    b += circle(cx, cy, r, "#CBD3DB", C["steel"], 8)
    for i in range(24):
        a  = 2 * math.pi * i / 24
        b += circle(cx + (r - 18) * math.cos(a), cy + (r - 18) * math.sin(a), 5, C["steel"])
    b += circle(cx, cy, r - 40, "#DCE2E8")
    holes = [(-90, C["teal"], True), (150, C["purple"], True), (30, None, False)]
    spots = {}
    for ang, col, on in holes:
        hx = cx + 122 * math.cos(math.radians(ang))
        hy = cy + 122 * math.sin(math.radians(ang))
        spots[ang] = (hx, hy)
        if on:
            b += circle(hx, hy, 30, col)
            b += key(hx - 2, hy, .75, C["white"], ang + 180)
        else:
            b += circle(hx, hy, 30, C["white"], C["steel"], 3)
            b += circle(hx, hy - 5, 8, C["navy"]) + path(f"M{hx - 6},{hy} L{hx + 6},{hy} L{hx + 4},{hy + 14} L{hx - 4},{hy + 14} Z", C["navy"])
    b += circle(cx, cy, 66, C["navy"])
    b += t(cx, cy + 12, "2 of 3", 32, 700, C["white"], "middle")

    orgs = [((L, 372, 260), "Organisation A", "key turned", C["teal"], True, (L + 260, 420), spots[-90]),
            ((L, 948, 300), "Organisation B", "key turned", C["purple"], True, (L + 150, 948), spots[150]),
            ((708, 948, 300), "Organisation C", "not needed", C["muted"], False, (858, 948), spots[30])]
    for (x, y, w), name, state, col, on, (fx, fy), (tx, ty) in orgs:
        d   = math.hypot(tx - fx, ty - fy)
        b  += line(fx, fy, tx - (tx - fx) * 36 / d, ty - (ty - fy) * 36 / d, col, 3, ' stroke-dasharray="8 8"')
    for (x, y, w), name, state, col, on, *_ in orgs:
        b += rect(x, y, w, 96, 18, C["white"], col if on else C["line"], 3)
        b += (check_badge(x + 40, y + 48, 18, col) if on else circle(x + 40, y + 48, 18, C["panel"], C["line"], 2))
        b += t(x + 72, y + 42, name, 24, 700, C["navy"] if on else C["gray"])
        b += t(x + 72, y + 72, state, 20, 400, C["gray"])
    b += t(R, 412, "Everything the network signs", 22, 600, C["ink"], "end")
    b += t(R, 442, "needs the quorum – even the", 22, 600, C["ink"], "end")
    b += t(R, 472, "list of its own keys.", 22, 600, C["ink"], "end")

    b += line(L, 1082, R, 1082, C["line"], 2)
    b += t(L, 1146, "No central server either:", 24, 700, C["navy"])
    b += t(L, 1180, "every server takes signed changes", 22, 400, C["ink"])
    b += t(L, 1208, "and merges them the same way.", 22, 400, C["ink"])
    pts = [(620, 1144), (760, 1124), (900, 1144), (690, 1212), (840, 1212)]
    for i in range(len(pts)):
        for j in range(i + 1, len(pts)):
            b += line(*pts[i], *pts[j], C["muted"], 2)
    for x, y in pts:
        b += server(x - 22, y - 26, 44, 54, C["ink"])
    return frame(9, ["No master key.", "Two of three must agree."], b)

def epochs():
    b  = path("M110,600 L540,410 L970,600 V1090 H110 Z", C["white"], C["navy"], 6)
    b += path("M110,600 H970", stroke=C["navy"], sw=4)
    b += t(540, 548, "one history", 26, 600, C["gray"], "middle")
    b += rect(116, 606, 418, 478, 0, "#F4EEE3")
    b += rect(546, 606, 418, 478, 0, C["tealbg"])
    b += line(540, 606, 540, 950, C["navy"], 4, ' stroke-dasharray="12 10"')
    b += line(540, 1066, 540, 1084, C["navy"], 4)
    b += t(325, 666, "EPOCH 1", 22, 700, C["bronze"], "middle", ls=4)
    b += t(325, 708, "SHA-256 · ECDSA", 28, 600, C["ink"], "middle")
    b += t(755, 666, "EPOCH 2", 22, 700, C["teal"], "middle", ls=4)
    b += t(755, 708, "SHA-512 · ML-DSA", 28, 600, C["ink"], "middle")
    b += t(755, 740, "post-quantum signatures", 20, 400, C["gray"], "middle")
    b += padlock(325, 840, 3.2, C["bronze"])
    b += padlock(755, 840, 3.2, C["teal"])
    ly = 990
    b += line(150, ly, 930, ly, C["navy"], 4)
    for i in range(6):
        b += circle(170 + i * 64, ly, 12, C["bronze"], C["white"], 3)
    for i in range(5):
        b += circle(650 + i * 64, ly, 12, C["teal"], C["white"], 3)
    b += seal(540, ly, 30, C["navy"], "F")
    b += t(540, ly + 62, "final checkpoint", 20, 600, C["gray"], "middle")
    b += t(540, 1150, "Old entries stay readable and checkable.", 28, 600, C["ink"], "middle")
    b += t(540, 1192, "New ones use the new locks.", 28, 600, C["ink"], "middle")
    return frame(9, ["New locks,", "same house."], b,
                 subtitle="When cryptography ages, the history moves on.")


# ---------------------------------------------------------------------------------------------
# 10 – bridges, not bulldozers
# ---------------------------------------------------------------------------------------------

def bridges():
    b  = rect(200, 400, 680, 116, 22, C["panel"])
    b += t(540, 446, "WHAT YOU RUN TODAY", 20, 700, C["gray"], "middle", ls=3)
    b += t(540, 490, "OCPI · your backend · your platform", 30, 600, C["navy"], "middle")
    b += arrow(540, 524, 540, 600, C["teal"], 6, 22)
    b += t(566, 572, "imported and signed by the owner", 22, 600, C["teal"])
    cx, cy = 540, 724
    b += circle(cx, cy, 118, C["navy"])
    b += t(cx, cy - 6, "WWCP_POI", 34, 700, C["white"], "middle")
    b += t(cx, cy + 30, "signed · versioned", 19, 400, "#C9D3DD", "middle")
    b += t(cx, cy + 54, "provable", 19, 400, "#C9D3DD", "middle")
    b += seal(cx + 92, cy + 84, 30, C["teal"])
    outs = [("OCPI 2.3",     ["your roaming", "partners"]),
            ("DATEX II 3.7", ["National Access", "Points (AFIR)"]),
            ("Driver apps",  ["proofs of each", "station"]),
            ("Metrology",    ["meter keys", "and evidence"])]
    w, gap = 222, 16
    for i, (title, lines_) in enumerate(outs):
        x  = L + i * (w + gap)
        b += arrow(cx + (i - 1.5) * 40, cy + 118, x + w / 2, 920, C["gray"], 4, 18)
        b += rect(x, 930, w, 250, 20, C["white"], C["line"], 2)
        ix, iy = x + w / 2, 990
        if i == 0:
            b += arrow(ix - 34, iy - 10, ix + 34, iy - 10, C["teal"], 5, 14) + arrow(ix + 34, iy + 12, ix - 34, iy + 12, C["teal"], 5, 14)
        elif i == 1:
            b += document(ix - 26, iy - 36, 52, 66, 3, C["teal"])
        elif i == 2:
            b += phone(ix - 22, iy - 40, 44, 80, C["teal"], C["white"]) + check_badge(ix + 22, iy - 34, 12)
        else:
            b += meter(ix - 30, iy - 38, .5)
        b += t(x + 24, 1080, title, 26, 700, C["navy"])
        b += t(x + 24, 1116, lines_[0], 21, 400, C["ink"])
        b += t(x + 24, 1144, lines_[1], 21, 400, C["ink"])
    b += t(540, 1226, "Open source (AGPL) · partly funded by NLnet's NGI Zero Commons Fund", 20, 400, C["gray"], "middle")
    return frame(10, ["Bridges,", "not bulldozers."], b, subtitle="Start next to what you run today.")


# ---------------------------------------------------------------------------------------------

VISUALS = {
    "00-overview/route.svg":                                 lambda: route(False),
    "01-annas-invoice/invoice.svg":                          invoice,
    "02-the-meters-own-id-card/evidence-chain.svg":          evidence_chain,
    "02-the-meters-own-id-card/key-swap.svg":                key_swap,
    "03-the-envelope-and-the-letter/envelope-and-letter.svg": envelope_and_letter,
    "04-a-land-register/register.svg":                       register,
    "04-a-land-register/owners.svg":                         owners,
    "05-one-fingerprint/fingerprint.svg":                    fingerprint_card,
    "05-one-fingerprint/fingerprint-tree.svg":               fingerprint_tree,
    "06-a-proof-in-your-pocket/phone-proof.svg":             phone_proof,
    "06-a-proof-in-your-pocket/trust-anchor.svg":            trust_anchor,
    "07-signed-price-tags/timeline-evidence.svg":            timeline_evidence,
    "08-the-atlas-and-the-traffic-report/atlas-and-traffic.svg": atlas_and_traffic,
    "09-no-master-key/vault.svg":                            vault,
    "09-no-master-key/epochs.svg":                           epochs,
    "10-bridges-not-bulldozers/bridges.svg":                 bridges,
    "10-bridges-not-bulldozers/recap.svg":                   lambda: route(True),
}

if __name__ == "__main__":
    import sys
    only = sys.argv[1:]
    for rel, make in VISUALS.items():
        if only and not any(o in rel for o in only):
            continue
        target = os.path.join(ROOT, rel)
        os.makedirs(os.path.dirname(target), exist_ok=True)
        with open(target, "w", encoding="utf-8", newline="\n") as f:
            f.write(make())
        print(rel)
