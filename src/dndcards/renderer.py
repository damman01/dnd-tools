"""HTML-Rendering eines Kartendecks via Jinja2."""

from __future__ import annotations

from jinja2 import Environment, PackageLoader, select_autoescape

from dndcards.models import CardDeck

_env = Environment(
    loader=PackageLoader("dndcards", "templates"),
    autoescape=select_autoescape(["html", "html.j2"]),
    trim_blocks=True,
    lstrip_blocks=True,
)


def render_html(deck: CardDeck) -> str:
    """Rendert das Deck als eigenständiges HTML-Dokument."""
    styles = _env.get_template("styles.css.j2").render(page=deck.page)
    return _env.get_template("deck.html.j2").render(deck=deck, styles=styles)
