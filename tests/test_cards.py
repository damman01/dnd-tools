import json

from dndcards.models import CardDeck
from dndcards.renderer import render_html

LEGACY = [
    {
        "name": "Hasensprung",
        "type": "blue",
        "cost": "Bonusaktion",
        "range": "4,5 Meter",
        "effect": "Du springst blitzschnell weg.",
        "trackers": 3,
        "tracker_label": "Pro Tag:",
    }
]


def test_example_deck_loads():
    deck = CardDeck.from_file("examples/wiwaka.json")
    assert len(deck.cards) == 7
    assert deck.cards[1].tracker.count == 4
    assert deck.cards[-1].tracker_rows[0].count == 4


def test_legacy_notebook_format():
    deck = CardDeck.from_json(json.dumps(LEGACY))
    assert deck.cards[0].tracker.label == "Pro Tag:"
    assert deck.cards[0].tracker.count == 3
    assert deck.cards[0].range_ == "4,5 Meter"


def test_render_html_contains_cards_and_page_size():
    deck = CardDeck.from_file("examples/wiwaka.json")
    html = render_html(deck)
    assert html.count('class="card ') == 7
    assert "size: 70mm 120mm" in html
    assert html.count('<div class="circle">') == 4 + 3 + 4 + 3 + 2 + 2


def test_html_escapes_user_input():
    deck = CardDeck.model_validate({"cards": [{"name": "<script>x</script>"}]})
    assert "<script>" not in render_html(deck)
