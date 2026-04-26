# SQLBook

A web-based SQL notebook platform — write queries, document findings, export a PDF. No Python. No build pipeline. Just SQL.

Built with **ASP.NET MVC (.NET 9) + HTMX** — server-rendered, zero SPA framework, zero client-side routing.

---

## What it is

SQLBook gives developers, analysts, and DBAs a Jupyter-style notebook interface for SQL. Each notebook is a sequence of **SQL cells**, **Markdown cells**, and (coming soon) **parameter cells** — executed interactively, documented inline, and exportable as PDF reports.

| Pain point | SQLBook answer |
|---|---|
| Jupyter requires Python/conda for SQL work | Browser only — open and query |
| VS Code SQL plugins are query runners, not documents | Notebooks persist queries + results + docs together |
| BI tools abstract away raw SQL | You write raw SQL; results stay with the notebook |
| No portable way to share query logic + context | `.sqlnb` JSON files — versionable, shareable, importable |

---

## Tech stack

| Layer | Choice |
|---|---|
| Backend | ASP.NET MVC (.NET 9) |
| Frontend interaction | HTMX 2.x — partial DOM swaps, no SPA |
| SQL editor | CodeMirror 6 with `@codemirror/lang-sql` + schema-aware autocomplete |
| Data grid | Tabulator.js 6.x *(Phase 2)* |
| PDF export | QuestPDF Community *(Phase 3)* |
| DB access | Dapper |
| App database | SQLite (dev) → SQL Server / PostgreSQL (prod) |
| Notebook storage | JSON files → `.sqlnb` format *(Phase 4)* |
| Deployment | Docker + k3s single-node Kubernetes, Traefik ingress |

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Node.js 18+ (for building the CodeMirror editor bundle — one-time step)

---

## Getting started

### 1. Clone and restore

```bash
git clone https://github.com/<your-username>/SQLBook.git
cd SQLBook
dotnet restore
```

### 2. Build the editor bundle

```bash
cd src/SQLBook.Web/editor
npm install
npm run build
cd ../../..
```

This produces `src/SQLBook.Web/wwwroot/js/editor.bundle.js`. Re-run whenever you change `editor/editor.js`.

### 3. Configure a database connection (optional)

By default the app creates a SQLite file at `~/sqlbook/default.db`. To point it at a real database, create `src/SQLBook.Web/appsettings.Development.json`:

```json
{
  "DefaultConnection": {
    "Provider": "sqlite",
    "ConnectionString": "Data Source=/Users/yourname/sqlbook/chinook.db"
  }
}
```

Supported providers: `sqlite`, `postgres`, `sqlserver`.

> **Tip:** [Chinook](https://github.com/lerocha/chinook-database/releases) is a great SQLite sample database for testing — 11 tables, realistic music store data.

### 4. Run

```bash
dotnet run --project src/SQLBook.Web
```

Open [http://localhost:5000](http://localhost:5000) — you'll land on the notebook library.

---

## Project structure

```
SQLBook/
├── src/
│   └── SQLBook.Web/
│       ├── Controllers/          # NotebookController, QueryController, SchemaController, LibraryController
│       ├── Data/                 # AppDb (SQLite connection factory, schema init)
│       ├── Models/               # Notebook, Cell, CellResult, NotebookIndex
│       ├── Services/             # NotebookService, QueryService, SchemaService
│       ├── Views/
│       │   ├── Notebook/         # Index.cshtml, _Cell.cshtml, _ResultTable.cshtml
│       │   ├── Library/          # Index.cshtml
│       │   └── Schema/           # _SchemaSidebar.cshtml
│       ├── editor/               # CodeMirror 6 source (editor.js) + package.json
│       └── wwwroot/
│           ├── js/               # htmx.min.js, editor.bundle.js (built)
│           └── css/              # app.css
└── tests/
    └── SQLBook.Tests/
```

Notebooks are stored as JSON files in `~/sqlbook/notebooks/`. The app database (SQLite) lives at `~/sqlbook/sqlbook.db` and indexes notebook metadata for fast library search.

---

## How autosave works

Every SQL and Markdown cell debounces saves 800ms after you stop typing. The notebook title saves on the same debounce. HTMX fires a `POST /notebook/{id}/autosave` with the changed `cellId` + `content` (or just `title`). No manual save button needed.

---

## Keyboard shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+Enter` / `Cmd+Enter` | Run the focused SQL cell |
| `Tab` | Indent in the SQL editor |
| `Esc` | Close autocomplete popup |

---

## Delivery roadmap

| Phase | Weeks | Scope |
|---|---|---|
| ✅ **1 — Core** | 1–4 | Notebook CRUD · SQL + Markdown cells · CodeMirror editor · query execution · result table · schema sidebar · autosave |
| 🔄 **2 — Editor** | 5–8 | Tabulator.js grid · Markdown preview · cell drag reorder · params cell |
| ⏳ **3 — Export** | 9–11 | QuestPDF PDF export · CSV export · Redis result cache |
| ⏳ **4 — Library** | 12–15 | `.sqlnb` file format · version snapshots · import/export · search/filter |
| ⏳ **5 — Connections** | 16–18 | Named DB connections UI · per-notebook connection hints |
| ⏳ **6 — Hardening** | 19–20 | ASP.NET Identity auth · Docker · k3s deployment · CI/CD |

---

## Third-party licences

| Library | Licence |
|---|---|
| HTMX 2.x | MIT |
| CodeMirror 6 | MIT |
| Tabulator.js 6.x | MIT |
| QuestPDF Community | MIT (free under $1M revenue) |
| Dapper | Apache 2.0 |
| Markdig | BSD-2 |

---

## Contributing

This is a solo project in active development. The PRD lives at `docs/SQLBook_PRD_v1.0.docx`.
