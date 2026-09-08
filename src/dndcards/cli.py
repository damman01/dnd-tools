"""Kommandozeilen-Interface: JSON-Kartendatei -> PDF."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from pydantic import ValidationError

from dndcards.models import CardDeck
from dndcards.pdf import render_pdf
from dndcards.renderer import render_html


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="dnd-cards",
        description="Erzeugt druckfertige DnD-Karten (PDF) aus einer JSON-Datei.",
    )
    parser.add_argument("input", type=Path, help="JSON-Datei mit dem Kartendeck")
    parser.add_argument(
        "-o",
        "--output",
        type=Path,
        help="Zieldatei (Standard: <input>.pdf bzw. <input>.html bei --html)",
    )
    parser.add_argument(
        "--html",
        action="store_true",
        help="Statt PDF nur die HTML-Vorschau schreiben",
    )
    return parser


def main(argv: list[str] | None = None) -> int:
    args = _build_parser().parse_args(argv)

    try:
        deck = CardDeck.from_file(args.input)
    except FileNotFoundError:
        print(f"Datei nicht gefunden: {args.input}", file=sys.stderr)
        return 1
    except ValidationError as exc:
        print(f"Ungültiges Kartendeck:\n{exc}", file=sys.stderr)
        return 1

    suffix = ".html" if args.html else ".pdf"
    output = args.output or args.input.with_suffix(suffix)

    if args.html:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(render_html(deck), encoding="utf-8")
    else:
        render_pdf(deck, output)

    print(f"{len(deck.cards)} Karten geschrieben nach {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
