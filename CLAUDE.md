# CLAUDE.md

Richtlinien für Claude Code bei der Arbeit an **WaveMP – a nostalgic media player**.

## Projekt

WaveMP ist ein selbst gehosteter Media Player für Musik und Musikvideos, ausgeliefert als Docker-Image. Die Optik orientiert sich an den klassischen Desktop-Media-Playern der Frutiger-Aero-Ära (Glas, Glanz, Hellblau, weiche Verläufe). Der Server scannt einen eingebundenen Medienordner, speichert die Bibliothek in einer Datenbank und stellt sie einer Web-App bereit. Native Clients (Android, iOS, Windows, macOS, evtl. Apple TV) folgen später und nutzen dieselbe API.

## Tech-Stack

| Bereich | Technologie |
|---|---|
| Backend | .NET 10, ASP.NET Core (REST-API) |
| Datenbank | PostgreSQL 18, EF Core mit Npgsql |
| Frontend | Vue 3, TypeScript, Vite, Pinia, Vue Router |
| Medien | ffmpeg / ffprobe (Scan, Transcoding, HLS), TagLib# (Tags) |
| Hintergrundjobs | Quartz.NET (In-Memory), Laufverlauf in eigener Tabelle |
| Visualisierung | butterchurn (Milkdrop-Presets), eigene WebGL-Styles |
| Auslieferung | Docker-Image (GHCR + Docker Hub), docker compose |
| CI | GitHub Actions |

## Repository-Struktur

```
/assets                        Grafiken, Icons, Sounds und sonstige Dateien, die im Projekt verwendet werden
/source
  WaveMP.Service/              ASP.NET-Core-Host, Controller, DI-Setup
  WaveMP.Core.<Bereich>/       Geschäftslogik pro Bereich (z. B. Core.Library, Core.Scanning, Core.Users, Core.Streaming)
  WaveMP.Data/                 EF Core: Entities, DbContext, Migrations, Repositories
  WaveMP.Web/                  Vue-3-Frontend
/tests
  WaveMP.<Projekt>.Tests/      Unit-Tests, ein Testprojekt pro Quellprojekt
  WaveMP.E2E/                  End-to-End-Tests
WaveMP.slnx                    Solution
Dockerfile
docker-compose.yml
README.md
THEMING.md                     Theme-API für Theme-Autoren (ab Schritt 3)
THIRD-PARTY-NOTICES.md         Lizenzen der Abhängigkeiten (ab Release)
LICENSE
CLAUDE.md
```

Neue Projekte und Ordner fügen sich in diese Struktur ein. Abweichungen nur, wenn es nicht anders geht, und dann in der Abschlussmeldung begründen. Es gibt keinen `/docs`-Ordner: Nutzerdokumentation steht in der README bzw. in den genannten Dateien im Root, Hinweise für den Maintainer kommen in die Abschlussmeldung des Schritts.

## Konventionen

### Code-Stil
- Im Repo-Root liegt eine `.editorconfig` des Maintainers. Sie ist verbindlich für allen Code (C#, TypeScript, Vue, JSON, YAML …).
- Die `.editorconfig` wird **nicht verändert**. Ist eine Regel im Weg oder fehlt eine, wird das in der Abschlussmeldung als Vorschlag genannt. Generierter Code (z. B. EF-Core-Migrationen) wird über `<Compile>`-Einstellungen oder `[GeneratedCode]`-Kennzeichnung von den Stilregeln ausgenommen, nicht über Änderungen an der `.editorconfig`.
- Vor jedem Commit `dotnet format` ausführen; im Frontend die Formatierung an der `.editorconfig` ausrichten.

### .NET
- Projekt-Suffixe: `.Service` für den REST-Host, `.Data` für EF Core und Datenzugriff, `.Core.<Bereich>` für Geschäftslogik, aufgeteilt nach Aufgaben. Kein `.Application`.
- Klassen, die nur Extension-Methoden enthalten, heißen `*Extensions.cs` (Plural), z. B. `ServiceCollectionExtensions.cs`.
- Ordnernamen immer im Plural, außer Ordner, die Projektnamen abbilden. Die Regel gilt für die .NET-Projekte; im Frontend gelten die üblichen Vue-Konventionen.
- Tests: NUnit, AwesomeAssertions und NSubstitute. Dazu erlaubt ist die nötige Infrastruktur: `Microsoft.NET.Test.Sdk`, `NUnit3TestAdapter`, `NUnit.Analyzers` und `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory` für API-Integrationstests, der offizielle Weg). Keine weiteren Test-Frameworks, Assertion- oder Mocking-Bibliotheken (z. B. kein Testcontainers, kein xUnit, kein Moq, kein FluentAssertions).
- API-Integrationstests laufen gegen eine echte PostgreSQL-Instanz, die lokal per docker compose und in der CI als Service-Container bereitgestellt wird.
- Frontend-Unit-Tests mit Vitest, abgelegt neben dem Code unter `source/WaveMP.Web/src/**/__tests__` (Vite-Standard). E2E-Tests der Web-App mit Playwright (npm) unter `/tests/WaveMP.E2E`.
- Etablierte, verbreitete NuGet-Pakete mit permissiver Lizenz sind Eigenbauten vorzuziehen.

### Frontend
- Composition API mit `<script setup lang="ts">`.
- Mobil zuerst: Jede Ansicht funktioniert auf dem Smartphone. Die Web-App ist als PWA installierbar.
- **Themes:** Der DOM ist generisch und wird über stabile `data-part`-Attribute beschrieben (Theme-API, dokumentiert in `THEMING.md`). Ein Theme ist genau eine CSS-Datei plus Grafiken und darf das Layout komplett umgestalten. Komponenten enthalten keine theme-spezifischen Styles. Icons und Beschriftungen stecken immer in eigenen Kind-Elementen (`data-part="icon"`, `data-part="label"`), damit `::before` und `::after` der Bereiche und Buttons frei für Theme-Effekte wie Glanz und Leuchten bleiben. Neue Bereiche bekommen eigene Parts; Änderungen an bestehenden Parts erhöhen die Version der Theme-API.
- Mitgelieferte Themes tragen eigene Namen (z. B. "Midnight"), keine Microsoft-Marken, und nutzen nur eigene Grafiken.
- **Icons:** SVG-Icons aus Phosphor Icons (MIT). Der Stil richtet sich nach dem Bereich: "fill" in dunklen Bereichen und Toolbars, "duotone" in der Sidebar, "regular" für kleine Hinweise im Inhaltsbereich. Icons werden als `mask-image` eingebunden, damit das Theme sie per `background` beliebig füllen kann, auch mit Farbverläufen, dazu Glanz und Leuchten per `filter: drop-shadow()`. Die Farbe kommt nie aus der SVG-Datei. Welche Icons ein Theme nutzt, steuern CSS-Variablen. Jede Fremdquelle wird mit Lizenz in `/assets/LICENSES.md` eingetragen. Das Projekt-Icon (App-Logo) ist ein Platzhalter, bis der Maintainer ein eigenes liefert.
- API-Zugriffe laufen ausschließlich über einen mit Microsoft Kiota aus dem OpenAPI-Dokument generierten Client, nie direkt per `fetch` in Komponenten. Der generierte Code wird nicht von Hand bearbeitet.
- **Mehrsprachigkeit:** Alle UI-Texte laufen über vue-i18n (eine JSON-Datei pro Sprache, Weblate-kompatibel). Sprachen: Englisch (Standard) und Deutsch. Die Sprache wird pro Benutzer gespeichert. Keine festen Texte in Komponenten.
- **Keine Texte im Backend:** i18n findet nur in den Clients statt. Die API liefert keine anzeigbaren Texte, sondern stabile, maschinenlesbare Codes (z. B. `code: "track.not_writable"` in den Problem Details, Statuswerte als Enums). Die Clients übersetzen diese Codes. Exception- und Log-Meldungen sind interne Texte auf Englisch und werden nie in der UI angezeigt. Alle Codes stehen als Enum bzw. Konstanten im OpenAPI-Dokument.

### API-Design
- REST, ressourcenorientiert, Substantive im Plural: `/api/v1/tracks`, `/api/v1/albums/{id}/tracks`.
- Korrekte HTTP-Verben (GET, POST, PUT, PATCH, DELETE) und Statuscodes (200, 201 + `Location`, 204, 400, 404, 409 …).
- Fehler als Problem Details nach RFC 9457.
- Paging, Sortierung und Filter über Query-Parameter; Collections sind immer gepaged.
- Versioniert unter `/api/v1`. Breaking Changes nur in einer neuen Version.
- Das OpenAPI-Dokument wird generiert und aktuell gehalten. Es ist der **einzige Vertrag** zwischen Server und Clients.
- Clients (Web-App und spätere native Apps mit .NET MAUI, Swift, Kotlin o. ä.) erhalten **keine Shared Libraries** aus diesem Projekt. Sie generieren ihre API-Schnittstellen aus dem OpenAPI-Dokument, bevorzugt mit Microsoft Kiota.
- Das OpenAPI-Dokument muss deshalb vollständig und generatorfreundlich sein: eindeutige `operationId`s, benannte Schemas, korrekt beschriebene Response-Typen und Statuscodes, Enums als benannte Typen.
- DTOs liegen im `.Service`-Projekt und werden nicht mit den Entities aus `.Data` vermischt.
- Medien-Streaming unterstützt HTTP-Range-Requests; transcodierte Inhalte laufen über HLS.
- Authentifizierung ist von Anfang an tokenbasiert, obwohl es keinen Login gibt: Die Auswahl eines Profils erzeugt ein Token. Native Clients nutzen das später genauso.
- Die Web-App darf nichts voraussetzen, was die API nicht bereitstellt.

## Produktregeln

- **Einfache Einrichtung nach Standard:** App und PostgreSQL laufen als zwei Container per docker compose, wie bei vergleichbaren Self-Hosting-Projekten üblich. Einzige Pflichtangabe ist `DB_PASSWORD` (per `.env`), die beide Container nutzen. PostgreSQL ist nur innerhalb des Docker-Netzwerks erreichbar. Alles andere hat sinnvolle Standardwerte.
- **Hintergrundjobs:** Quartz.NET nur im In-Memory-Modus, keine Quartz-Tabellen. Zeitpläne entstehen beim Start aus Code und Settings. Der Laufverlauf liegt in einer eigenen EF-Core-Tabelle. Alles, was einen Neustart überstehen muss (z. B. ausstehende Scrobbles), ist eine eigene Warteschlange in unserer Datenbank, nicht Teil von Quartz.
- **Eine Oberfläche:** Es gibt keine zusätzlichen UIs neben der App (keine Fremd-Dashboards für Jobs, Datenbank o. ä.). Alles, was ein Admin sehen oder steuern soll, wird in der Web-App über die API umgesetzt. API-Dokumentations-UIs (z. B. Scalar) nur im Entwicklungsmodus.
- **PWA:** Die Web-App unterstützt technisch immer die Installation als PWA. Da Browser dafür HTTPS verlangen, wird das im README nur als kurzer Hinweis erwähnt.
- **Persistente Daten:** Alles, was erhalten bleiben muss (Konfiguration, Uploads wie Avatare und Playlist-Cover, Cache), liegt unter `/data`; die Datenbank in ihrem eigenen Volume. Die App exportiert die Datenbank täglich nach `/data/backups`; die externe Sicherung übernimmt der Nutzer über die gemounteten Ordner.
- **Standards zuerst:** Wenn eine Vorgabe vom üblichen bzw. offiziell empfohlenen Vorgehen abweicht oder es eine bessere Lösung gibt, vor der Umsetzung darauf hinweisen und die Alternative nennen.
- **Benutzer:** kein Login, Profilauswahl ähnlich wie bei Streamingdiensten. Ein fest eingebautes Admin-Profil, das weniger prominent dargestellt wird, sobald es weitere Benutzer gibt. Optionale PIN später, per Umgebungsvariable (`ADMIN_PIN`) setzbar.
- **Datei-Gruppierung:** Alle Dateien eines Titels tragen denselben Basisnamen im selben Ordner (`Song.mp3`, `Song.mkv`, `Song.jpg`). Die Formate pro Kategorie dürfen abweichen. Verschiedene Ordnerstrukturen müssen unterstützt werden.
- **Duplikate:** Gibt es mehrere Audio- (oder Video-)Dateien mit gleichem Basisnamen, gewinnt die höchste Qualität. Eine Admin-Auswahl pro Titel kommt später.
- **Cover:** Standardmäßig Album-Cover (Ebene des Album-Ordners) vor Titel-Cover; später global, pro Album und pro Titel umstellbar.
- **Metadaten:** zuerst aus Tags, dann aus Ordner-/Dateistruktur, sonst bestmögliches Ergebnis. Änderungen durch den Admin werden **nur in Audiodateien** zurückgeschrieben, nie in Videos. Der Editor ist nur aktiv, wenn die Datei beschreibbar ist und eine Audiodatei existiert.
- **Wiedergabe:** Direct Play, wenn der Client das Format unterstützt, sonst Transcoding mit ffmpeg. Software-Transcoding muss immer funktionieren; Hardware-Beschleunigung (Intel Quick Sync über `/dev/dri`) ist optional.
- **Visualisierungen:** Die eigenen Styles heißen **A-Force** und **BlackCap**. Fremdprodukte, die als Vorbild dienen, werden nirgends genannt: nicht im Code, nicht in Kommentaren, Commits oder der Dokumentation.
- **Design:** nur eigene Grafiken. Keine Logos, Icons, Sounds oder sonstigen Assets von Microsoft.

## Lizenzen

- Das Projekt steht unter der MIT-Lizenz.
- Nur Abhängigkeiten mit MIT-kompatiblen Lizenzen (MIT, BSD, Apache-2.0, ISC, MPL-2.0, LGPL bei dynamischer Einbindung). **Keine GPL- oder AGPL-Abhängigkeiten** in NuGet oder npm.
- Vor dem Hinzufügen eines Pakets die Lizenz prüfen und in der Commit-Beschreibung nennen.
- ffmpeg wird als externer Prozess aufgerufen, nie gelinkt. Der Lizenzhinweis wird mit dem Image ausgeliefert.
- **Testmedien** (Audio, Video, Cover) werden nicht aus fremden Quellen übernommen, sondern zur Testlaufzeit synthetisch mit ffmpeg erzeugt (Sinustöne, Testbilder, generierte Cover, Tags per `-metadata`). Dateinamen und Ordnerstrukturen legen die Tests selbst an. Ausnahmen nur mit Medien unter CC0 und mit Herkunftsangabe in `/tests/assets/LICENSES.md`.

## Git

- Es gibt keine Feature-Branches. Gearbeitet, committet und gepusht wird direkt auf `main`.
- Commit-Nachrichten auf Englisch im Format `WMP-{Roadmap-Punkt}: {Nachricht}`, z. B. `WMP-5: add scan logic`. Die Nummer entspricht dem Roadmap-Schritt, an dem gerade gearbeitet wird.
- Gepusht wird einmal pro abgeschlossenem Roadmap-Schritt, und nur wenn Build und alle Tests lokal grün sind. Schlagen Tests fehl, wird nicht gepusht und der Schritt gilt als nicht abgeschlossen.
- Nach dem Push auf das Ergebnis der CI warten (`gh run watch`). Schlägt die CI fehl, den Fehler beheben, als neuen Commit pushen und erst danach mit dem nächsten Schritt weitermachen.
- Niemals `push --force`, niemals die Historie von `main` umschreiben (kein `rebase`, kein `reset` auf bereits gepushte Commits).
- Am Ende eines Schritts eine Abschlussmeldung mit: Liste der Commits, kurze Zusammenfassung der Änderungen, wie man das Ergebnis testet, und offene Punkte.
- `ROADMAP.md` existiert nur lokal und steht in der `.gitignore`. Niemals committen.

## Arbeitsweise

- Immer nur an einem Roadmap-Schritt arbeiten. Keine Features aus späteren Schritten umsetzen.
- Spätere Features (native Clients, PIN, Overrides) nur dort vorbereiten, wo es teure Umbauten vermeidet, und das ausdrücklich erwähnen.
- Jedes Feature enthält Tests für seine Geschäftslogik.
- `README.md` aktuell halten, wenn sich Setup oder Bedienung ändern.
- Keine Emojis in Dokumentation (README, CLAUDE.md, ROADMAP, THEMING.md, CHANGELOG), Commit-Nachrichten und Code-Kommentaren.
- **Selbstständig entscheiden:** Bei Unklarheiten nicht nachfragen, sondern die bestmögliche Entscheidung nach dieser CLAUDE.md, der Roadmap und dem üblichen Vorgehen treffen und weiterarbeiten. Jede solche Entscheidung wird in der Abschlussmeldung unter "Annahmen" aufgeführt (Frage, Entscheidung, kurze Begründung), damit der Maintainer sie nachträglich korrigieren kann.
- Ausnahme: Ein Roadmap-Prompt verlangt ausdrücklich eine Entscheidung des Maintainers. Dann vor der Umsetzung anhalten.