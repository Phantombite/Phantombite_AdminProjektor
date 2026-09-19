# Phantombite AdminProjektor

Projektor-Block für Server-Admins. Ein Klick auf **„Fertig bauen“** baut die aktuelle Projektion sofort fertig und repariert
danach alle Blöcke des Grids (und mechanisch verbundener Grids) auf 100 %. Praktisch, um große Stationen und Prefabs
aufzubauen, ohne von Hand zu schweißen.

## Funktionen
- Zwei Blöcke: `AdminProjektor_Large` und `AdminProjektor_Small`
- Button „Fertig bauen“ im Terminal des Blocks
- Baut baubare Blöcke der Projektion sofort und repariert anschließend alles
- Reagiert auf die Server-Last: Bei Überlast baut und repariert er langsamer (über den Phantombite Core)

## Commands
Keine. Bedienung über das Terminal des Blocks.

## Voraussetzungen
- **Phantombite Core** (liefert das Bauteil `AdminChip`). Der Block braucht 1000× `AdminChip` und ist nicht herstellbar,
  deshalb können nur Admins ihn bauen.

Workshop-ID: 3706769805
