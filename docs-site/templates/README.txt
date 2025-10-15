SQLiteM DocFX Theme (sqlitem)
=============================

Struktur:
- templates/sqlitem/partials/head.tmpl.partial      -> bindet theme.css und Theme-Color ein
- templates/sqlitem/partials/logo.tmpl.partial      -> custom Logo/Titel
- templates/sqlitem/partials/footer.tmpl.partial    -> Footer-Zeile
- templates/sqlitem/styles/theme.css                 -> Farbschema & Layout
- templates/sqlitem/assets/sqlitem-logo.svg          -> Logo

Einbindung in docfx.json:
{
  "template": [
    "default",
    "templates/sqlitem"
  ],
  "globalMetadata": {
    "_appName": "SQLiteM",
    "title": "SQLiteM Dokumentation"
  }
}

Optional:
- Eigenes Logo: Ersetze assets/sqlitem-logo.svg.
- Farben: Passe die CSS-Variablen in :root an (z. B. --primary, --accent).

Hinweis:
- Die Templates überschreiben nur Teile des Default-Themes. Du kannst jederzeit weitere Partials ergänzen.