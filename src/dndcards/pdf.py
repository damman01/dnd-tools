"""PDF-Erzeugung aus einem Kartendeck."""

from __future__ import annotations

from pathlib import Path

from dndcards.models import CardDeck
from dndcards.renderer import render_html


def render_pdf_bytes(deck: CardDeck) -> bytes:
    """Rendert das Deck als PDF und gibt die Bytes zurück."""
    from weasyprint import HTML  # lazy: WeasyPrint braucht native Bibliotheken

    return HTML(string=render_html(deck)).write_pdf()


def render_pdf(deck: CardDeck, output: str | Path) -> Path:
    """Rendert das Deck und schreibt es nach ``output``."""
    path = Path(output)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(render_pdf_bytes(deck))
    return path
