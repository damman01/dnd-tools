"""Streamlit-UI: Karten hochladen, bearbeiten und als PDF exportieren.

Start:  streamlit run src/dndcards/app.py
"""

from __future__ import annotations

import json

import streamlit as st
from pydantic import ValidationError
from streamlit.components.v1 import html as st_html

from dndcards.models import Card, CardDeck, PageSetup, Tracker, TrackerRow
from dndcards.pdf import render_pdf_bytes
from dndcards.renderer import render_html

COLORS = ["red", "blue", "green", "gold", "purple", "grey"]


def _deck() -> CardDeck:
    if "deck" not in st.session_state:
        st.session_state.deck = CardDeck()
    return st.session_state.deck


def _sidebar(deck: CardDeck) -> None:
    st.sidebar.header("Deck laden")
    uploads = st.sidebar.file_uploader(
        "JSON-Kartendateien", type="json", accept_multiple_files=True
    )
    if uploads and st.sidebar.button("Hochgeladene Dateien übernehmen"):
        cards: list[Card] = []
        title = deck.title
        page = deck.page
        for upload in uploads:
            try:
                loaded = CardDeck.from_json(upload.getvalue())
            except (ValidationError, json.JSONDecodeError) as exc:
                st.sidebar.error(f"{upload.name}: {exc}")
                return
            cards.extend(loaded.cards)
            if len(uploads) == 1:
                title, page = loaded.title, loaded.page
        st.session_state.deck = CardDeck(title=title, page=page, cards=cards)
        st.rerun()

    st.sidebar.header("Deck-Einstellungen")
    deck.title = st.sidebar.text_input("Titel", value=deck.title)
    deck.page = PageSetup(
        width=st.sidebar.text_input("Kartenbreite", value=deck.page.width),
        height=st.sidebar.text_input("Kartenhöhe", value=deck.page.height),
        margin=st.sidebar.text_input("Rand", value=deck.page.margin),
    )


def _edit_card(index: int, card: Card, deck: CardDeck) -> None:
    label = card.name or f"Karte {index + 1}"
    with st.expander(f"{index + 1}. {label}", expanded=not card.name):
        card.name = st.text_input("Name", value=card.name, key=f"name{index}")
        card.type = st.selectbox(
            "Farbe",
            COLORS,
            index=COLORS.index(card.type),
            key=f"type{index}",
        )
        col1, col2 = st.columns(2)
        card.cost = col1.text_input("Kosten", value=card.cost, key=f"cost{index}")
        card.range_ = col2.text_input(
            "Reichweite", value=card.range_, key=f"range{index}"
        )
        card.effect = st.text_area("Effekt", value=card.effect, key=f"effect{index}")
        card.fluff = st.text_area("Fluff-Text", value=card.fluff, key=f"fluff{index}")
        card.tactic = st.text_area("Taktik", value=card.tactic, key=f"tactic{index}")

        col3, col4 = st.columns([2, 1])
        tracker_label = col3.text_input(
            "Tracker-Beschriftung",
            value=card.tracker.label if card.tracker else "",
            key=f"tlabel{index}",
        )
        tracker_count = col4.number_input(
            "Kreise",
            min_value=0,
            max_value=20,
            value=card.tracker.count if card.tracker else 0,
            key=f"tcount{index}",
        )
        card.tracker = (
            Tracker(label=tracker_label, count=int(tracker_count))
            if tracker_count
            else None
        )

        rows = st.data_editor(
            [row.model_dump() for row in card.tracker_rows] or [{"label": "", "count": 0}],
            column_config={
                "label": st.column_config.TextColumn("Ressourcen-Zeile"),
                "count": st.column_config.NumberColumn("Kreise", min_value=0, max_value=20),
            },
            num_rows="dynamic",
            key=f"rows{index}",
        )
        card.tracker_rows = [
            TrackerRow(label=row["label"], count=int(row["count"] or 0))
            for row in rows
            if row.get("label")
        ]

        if st.button("Karte löschen", key=f"del{index}"):
            deck.cards.pop(index)
            st.rerun()


def main() -> None:
    st.set_page_config(page_title="DnD Kartengenerator", page_icon="🎲", layout="wide")
    st.title("DnD Aktions- & Zauberkarten-Generator")

    deck = _deck()
    _sidebar(deck)

    if st.button("Neue Karte hinzufügen"):
        deck.cards.append(Card(name=""))
        st.rerun()

    if not deck.cards:
        st.info("Lade links eine JSON-Datei hoch oder lege eine neue Karte an.")
        return

    for index, card in enumerate(list(deck.cards)):
        _edit_card(index, card, deck)

    st.divider()
    st.subheader("Export")

    col1, col2, col3 = st.columns(3)
    col1.download_button(
        "JSON herunterladen",
        data=deck.to_json(),
        file_name=f"{deck.title}.json",
        mime="application/json",
    )
    col2.download_button(
        "HTML herunterladen",
        data=render_html(deck),
        file_name=f"{deck.title}.html",
        mime="text/html",
    )
    try:
        pdf = render_pdf_bytes(deck)
    except OSError as exc:
        col3.error(f"PDF-Erzeugung fehlgeschlagen: {exc}")
    else:
        col3.download_button(
            "PDF herunterladen",
            data=pdf,
            file_name=f"{deck.title}.pdf",
            mime="application/pdf",
        )

    with st.expander("HTML-Vorschau"):
        st_html(render_html(deck), height=600, scrolling=True)


main()
