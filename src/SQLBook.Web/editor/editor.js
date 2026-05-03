import { marked } from 'marked';
import { EditorView, keymap } from '@codemirror/view'
import { EditorState, Prec } from '@codemirror/state'
import { basicSetup } from 'codemirror'
import { sql, SQLite, MySQL, PostgreSQL, MSSQL } from '@codemirror/lang-sql'
import { syntaxHighlighting, HighlightStyle } from '@codemirror/language'
import { tags } from '@lezer/highlight'

const AUTOSAVE_DELAY = 800;

// VS Code Dark+ inspired syntax colours
const sqlDarkHighlight = HighlightStyle.define([
  // Keywords: SELECT, FROM, WHERE, JOIN, GROUP BY, ORDER BY, INSERT, UPDATE …
  { tag: tags.keyword,                color: '#569cd6', fontWeight: 'bold' },
  // NULL, TRUE, FALSE
  { tag: [tags.null, tags.bool],      color: '#569cd6' },
  // String literals  '…'
  { tag: tags.string,                 color: '#ce9178' },
  // Numeric literals  42, 3.14
  { tag: tags.number,                 color: '#b5cea8' },
  // Comments  -- and /* */
  { tag: [tags.lineComment, tags.blockComment], color: '#6a9955', fontStyle: 'italic' },
  // Operators  = > < + - * / != <>
  { tag: tags.operator,               color: '#d4d4d4' },
  // Punctuation  ( ) , ;
  { tag: tags.punctuation,            color: '#d4d4d4' },
  // Table / column / alias names
  { tag: tags.name,                   color: '#9cdcfe' },
  // Built-in functions  COUNT SUM AVG COALESCE CAST …
  { tag: tags.function(tags.name),    color: '#dcdcaa' },
  // Type names  INT VARCHAR DATE …
  { tag: tags.typeName,               color: '#4ec9b0' },
  // Special variables / parameters  @param :name
  { tag: tags.variableName,           color: '#9cdcfe' },
  // Brackets  [ ]
  { tag: tags.bracket,                color: '#ffd700' },
]);

// Structural dark theme (background, gutter, selection, autocomplete popup …)
const sqlDarkTheme = EditorView.theme({
  // Editor shell
  '&':                          { fontSize: '0.875rem', background: '#1e1e1e', color: '#d4d4d4', borderRadius: '0 0 4px 4px' },
  '.cm-content':                { caretColor: '#aeafad', minHeight: '90px', padding: '8px 0' },
  '.cm-focused':                { outline: 'none' },
  '.cm-cursor':                 { borderLeftColor: '#aeafad', borderLeftWidth: '2px' },

  // Gutter (line numbers)
  '.cm-gutters':                { background: '#1e1e1e', color: '#5a5a5a', border: 'none', paddingRight: '4px' },
  '.cm-activeLineGutter':       { background: '#2a2d2e', color: '#c6c6c6' },
  '.cm-lineNumbers .cm-gutterElement': { minWidth: '32px', textAlign: 'right' },

  // Active line highlight
  '.cm-activeLine':             { background: '#2a2d2e' },

  // Text selection
  '&.cm-focused .cm-selectionBackground, .cm-selectionBackground': { background: '#264f78 !important' },
  '.cm-selectionMatch':         { background: '#3a3d41' },

  // Matching brackets
  '&.cm-focused .cm-matchingBracket': { background: '#3b3b3b', outline: '1px solid #888' },

  // Search/highlight matches
  '.cm-searchMatch':            { background: '#623315', outline: '1px solid #c86320' },
  '.cm-searchMatch.cm-searchMatch-selected': { background: '#9e6a03' },

  // Fold gutter
  '.cm-foldGutter':             { color: '#5a5a5a' },
  '.cm-foldGutter:hover':       { color: '#c6c6c6' },

  // Autocomplete popup
  '.cm-tooltip':                { background: '#252526', border: '1px solid #454545', borderRadius: '4px', color: '#cccccc' },
  '.cm-tooltip-autocomplete > ul > li': { padding: '2px 8px' },
  '.cm-tooltip-autocomplete > ul > li[aria-selected]': { background: '#094771', color: '#fff' },
  '.cm-completionIcon':         { color: '#75beff', marginRight: '4px' },
  '.cm-completionLabel':        { color: '#cccccc' },
  '.cm-completionDetail':       { color: '#9d9d9d', fontStyle: 'italic', marginLeft: '8px' },

  // Scrollbar (webkit)
  '.cm-scroller::-webkit-scrollbar':       { height: '6px', width: '6px' },
  '.cm-scroller::-webkit-scrollbar-track': { background: '#1e1e1e' },
  '.cm-scroller::-webkit-scrollbar-thumb': { background: '#424242', borderRadius: '3px' },
}, { dark: true });

function getDialect(name) {
  switch ((name || '').toLowerCase()) {
    case 'postgres':   return PostgreSQL;
    case 'sqlserver':  return MSSQL;
    case 'mysql':      return MySQL;
    default:           return SQLite;
  }
}

// Build the schema map CodeMirror expects: { TableName: ["col1", "col2", ...] }
function buildSchemaMap(tables) {
  const map = {};
  for (const t of tables) {
    map[t.tableName] = t.columns.map(c => c.columnName);
  }
  return map;
}

// Initialise a single CodeMirror editor on a .cm-host element
function initEditor(host, schemaMap, dialect) {
  const cellId   = host.dataset.cellId;
  const content  = host.dataset.content || '';
  const hiddenInput = document.getElementById('sql-' + cellId);
  const runBtn      = document.getElementById('run-' + cellId);
  const autosaveTarget = document.getElementById('autosave-trigger-' + cellId);

  let autosaveTimer = null;

  const extensions = [
    basicSetup,
    sql({ dialect: getDialect(dialect), schema: schemaMap }),
    EditorView.updateListener.of(update => {
      if (!update.docChanged) return;
      const value = update.state.doc.toString();

      // Keep hidden input in sync for form submissions
      if (hiddenInput) hiddenInput.value = value;

      // Debounced autosave via HTMX
      clearTimeout(autosaveTimer);
      autosaveTimer = setTimeout(() => {
        if (autosaveTarget) htmx.trigger(autosaveTarget, 'sqlbook:autosave');
      }, AUTOSAVE_DELAY);
    }),
    // Ctrl+Enter / Cmd+Enter → run cell
    Prec.highest(keymap.of([{
      key: 'Ctrl-Enter',
      mac: 'Mod-Enter',
      run: () => { if (runBtn) runBtn.click(); return true; }
    }])),
    sqlDarkTheme,
    syntaxHighlighting(sqlDarkHighlight),
  ];

  const view = new EditorView({
    state: EditorState.create({ doc: content, extensions }),
    parent: host,
  });

  // Sync initial value to hidden input
  if (hiddenInput) hiddenInput.value = content;

  return view;
}

// ---------- Markdown cells ----------

marked.use({ gfm: true, breaks: true });

function initMarkdownCell(host) {
  host.setAttribute('data-md-init', '1');
  const cellId    = host.dataset.cellId;
  const editDiv   = document.getElementById('md-edit-'    + cellId);
  const previewDiv = document.getElementById('md-preview-' + cellId);
  const textarea  = editDiv?.querySelector('textarea');
  const toggleBtn = document.getElementById('md-toggle-'  + cellId);
  if (!editDiv || !previewDiv || !textarea || !toggleBtn) return;

  let isPreview = false;

  function showPreview() {
    previewDiv.innerHTML = marked.parse(textarea.value || '');
    editDiv.style.display    = 'none';
    previewDiv.style.display = 'block';
    toggleBtn.textContent    = '✏ Edit';
    toggleBtn.classList.add('active');
    isPreview = true;
  }

  function showEdit() {
    editDiv.style.display    = 'block';
    previewDiv.style.display = 'none';
    toggleBtn.textContent    = '👁 Preview';
    toggleBtn.classList.remove('active');
    isPreview = false;
    setTimeout(() => textarea.focus(), 0);
  }

  // Toggle button
  toggleBtn.addEventListener('click', () => { isPreview ? showEdit() : showPreview(); });

  // Auto-preview on blur — but not when focus moved to the toggle button itself
  textarea.addEventListener('blur', e => {
    if (e.relatedTarget === toggleBtn) return;
    if (textarea.value.trim()) showPreview();
  });

  // Click anywhere on the preview to go back to edit
  previewDiv.addEventListener('click', showEdit);

  // Start in preview when the cell already has content (e.g. loaded from file)
  if (host.dataset.hasContent === 'true') showPreview();
}

// ---------- Params cells ----------

function parseParams(text) {
  const result = {};
  for (const line of (text || '').split('\n')) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('//') || trimmed.startsWith('--')) continue;
    const m = trimmed.match(/^@([\w]+)\s*=\s*(.*)$/);
    if (m) result[m[1]] = m[2].trim();
  }
  return result;
}

function sidebarParamsKey() {
  const notebookId = document.getElementById('cells-container')?.dataset.notebookId;
  return notebookId ? `sqlbook:sidebar-params:${notebookId}` : null;
}

function getAllParams() {
  const merged = {};
  // Sidebar params are the base (lowest priority)
  const sidebarTa = document.getElementById('sidebar-params');
  if (sidebarTa) Object.assign(merged, parseParams(sidebarTa.value));
  // Params cells override sidebar params
  document.querySelectorAll('.params-host textarea').forEach(ta => {
    Object.assign(merged, parseParams(ta.value));
  });
  return merged;
}


function initParamsCell(host) {
  host.setAttribute('data-params-init', '1');
}

function initSidebarParams() {
  const ta    = document.getElementById('sidebar-params');
  const clear = document.getElementById('sidebar-params-clear');
  if (!ta) return;

  const key = sidebarParamsKey();

  // Restore persisted value
  if (key) {
    const saved = localStorage.getItem(key);
    if (saved) ta.value = saved;
  }

  ta.addEventListener('input', () => {
    if (key) localStorage.setItem(key, ta.value);
  });

  clear?.addEventListener('click', () => {
    ta.value = '';
    if (key) localStorage.removeItem(key);
  });
}

// ---------- SQL + Markdown initialisation ----------

// Initialise all uninitialised .cm-host and .md-host elements on the page
function initAll(schemaMap, dialect) {
  document.querySelectorAll('.cm-host:not([data-cm-init])').forEach(host => {
    host.setAttribute('data-cm-init', '1');
    initEditor(host, schemaMap, dialect);
  });
  document.querySelectorAll('.md-host:not([data-md-init])').forEach(host => {
    initMarkdownCell(host);
  });
  document.querySelectorAll('.params-host:not([data-params-init])').forEach(host => {
    initParamsCell(host);
  });
}

// ---------- Preview mode ----------

function initPreviewMode() {
  const btn       = document.getElementById('preview-mode-btn');
  const container = document.getElementById('cells-container');
  const toolbar   = btn?.closest('.d-flex');   // the toolbar row
  if (!btn || !container) return;

  btn.addEventListener('click', () => {
    const entering = container.classList.toggle('preview-mode');

    // Swap button label and style
    btn.textContent = entering ? '✏ Edit Mode' : '👁 Preview';
    btn.classList.toggle('btn-outline-secondary', !entering);
    btn.classList.toggle('btn-secondary',          entering);

    // Hide/show the other toolbar buttons so only Exit button remains
    if (toolbar) {
      toolbar.querySelectorAll('button:not(#preview-mode-btn)').forEach(b => {
        b.style.display = entering ? 'none' : '';
      });
    }

    if (entering) {
      // Ensure every markdown cell has rendered preview content
      document.querySelectorAll('.md-host').forEach(host => {
        const cellId  = host.dataset.cellId;
        const textarea = document.getElementById('md-edit-' + cellId)?.querySelector('textarea');
        const preview  = document.getElementById('md-preview-' + cellId);
        if (textarea && preview && !preview.innerHTML.trim()) {
          preview.innerHTML = marked.parse(textarea.value || '');
        }
      });
    }
  });
}

// ---------- Cell drag-and-drop reorder ----------

function initDragReorder() {
  const container = document.getElementById('cells-container');
  if (!container) return;
  const notebookId = container.dataset.notebookId;
  let dragging = null;

  // Make a cell draggable only while the handle is held
  container.addEventListener('mousedown', e => {
    if (e.target.closest('.cell-drag-handle')) {
      const cell = e.target.closest('.nb-cell');
      if (cell) cell.setAttribute('draggable', 'true');
    }
  });

  document.addEventListener('mouseup', () => {
    if (!dragging) {
      container.querySelectorAll('.nb-cell[draggable]')
        .forEach(c => c.removeAttribute('draggable'));
    }
  });

  container.addEventListener('dragstart', e => {
    const cell = e.target.closest('.nb-cell');
    if (!cell) return;
    dragging = cell;
    e.dataTransfer.effectAllowed = 'move';
    setTimeout(() => cell.classList.add('cell-dragging'), 0);
  });

  container.addEventListener('dragend', () => {
    if (dragging) {
      dragging.classList.remove('cell-dragging');
      dragging.removeAttribute('draggable');
      dragging = null;
    }
    clearDropIndicators();
  });

  container.addEventListener('dragover', e => {
    e.preventDefault();
    const cell = e.target.closest('.nb-cell');
    if (!cell || cell === dragging) return;
    clearDropIndicators();
    const midY = cell.getBoundingClientRect().top + cell.offsetHeight / 2;
    cell.classList.add(e.clientY < midY ? 'drop-above' : 'drop-below');
  });

  container.addEventListener('dragleave', e => {
    if (!container.contains(e.relatedTarget)) clearDropIndicators();
  });

  container.addEventListener('drop', e => {
    e.preventDefault();
    const target = e.target.closest('.nb-cell');
    if (!target || !dragging || target === dragging) return;

    const midY = target.getBoundingClientRect().top + target.offsetHeight / 2;
    if (e.clientY < midY) container.insertBefore(dragging, target);
    else                   container.insertBefore(dragging, target.nextSibling);

    clearDropIndicators();
    persistOrder(container, notebookId);
  });

  function clearDropIndicators() {
    container.querySelectorAll('.drop-above, .drop-below')
      .forEach(c => c.classList.remove('drop-above', 'drop-below'));
  }

  function persistOrder(container, notebookId) {
    const ids = [...container.querySelectorAll('.nb-cell')].map(c => c.dataset.cellId);
    fetch(`/notebook/${notebookId}/cell/reorder`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(ids),
    });
  }
}

// ---------- Cell collapse ----------

function setCellCollapsed(cell, collapsed) {
  cell.classList.toggle('collapsed', collapsed);
  const btn = cell.querySelector('.cell-collapse-btn');
  if (btn) {
    btn.textContent = collapsed ? '⊕' : '⊖';
    btn.title       = collapsed ? 'Expand' : 'Collapse';
  }
}

function initCollapse() {
  // Per-cell toggle — event delegation so newly added cells work too
  document.getElementById('cells-container')?.addEventListener('click', e => {
    const btn = e.target.closest('.cell-collapse-btn');
    if (!btn) return;
    const cell = btn.closest('.nb-cell');
    if (cell) setCellCollapsed(cell, !cell.classList.contains('collapsed'));
  });

  // Root toggle: collapses all when any are expanded, expands all otherwise
  const rootBtn = document.getElementById('collapse-all-btn');
  if (!rootBtn) return;

  rootBtn.addEventListener('click', () => {
    const cells = [...document.querySelectorAll('#cells-container .nb-cell')];
    const shouldCollapse = cells.some(c => !c.classList.contains('collapsed'));
    cells.forEach(c => setCellCollapsed(c, shouldCollapse));
    rootBtn.textContent = shouldCollapse ? '⊞ Expand All' : '⊟ Collapse All';
  });
}

// ---------- Export PDF ----------

function initExportPdf() {
  const btn       = document.getElementById('export-pdf-btn');
  const container = document.getElementById('cells-container');
  if (!btn || !container) return;

  btn.addEventListener('click', () => {
    const wasPreview = container.classList.contains('preview-mode');

    // Enter preview mode so markdown is rendered and editor chrome is hidden
    if (!wasPreview) document.getElementById('preview-mode-btn')?.click();

    // After printing (or cancel), restore previous state
    const restore = () => {
      if (!wasPreview) document.getElementById('preview-mode-btn')?.click();
      window.removeEventListener('afterprint', restore);
    };
    window.addEventListener('afterprint', restore);

    // Two rAF ticks to let preview-mode DOM changes flush before the print dialog opens
    requestAnimationFrame(() => requestAnimationFrame(() => window.print()));
  });
}

// ---------- Run All ----------

function initRunAll() {
  const btn = document.getElementById('run-all-btn');
  if (!btn) return;

  btn.addEventListener('click', async () => {
    const runBtns = [...document.querySelectorAll('#cells-container .btn-run')];
    if (runBtns.length === 0) return;

    btn.disabled = true;
    const original = btn.innerHTML;
    btn.innerHTML = '⏳ Running…';

    for (const runBtn of runBtns) {
      await new Promise(resolve => {
        const handler = e => {
          if (e.detail.elt === runBtn || e.target === runBtn) {
            document.removeEventListener('htmx:afterRequest', handler);
            resolve();
          }
        };
        document.addEventListener('htmx:afterRequest', handler);
        runBtn.click();
      });
    }

    btn.disabled = false;
    btn.innerHTML = original;
  });
}

// Fetch schema from /api/schema and boot all editors
async function boot() {
  let schemaMap = {};
  let dialect = document.body.dataset.dbProvider || 'sqlite';

  try {
    const resp = await fetch('/api/schema');
    if (resp.ok) {
      const tables = await resp.json();
      schemaMap = buildSchemaMap(tables);
    }
  } catch (_) { /* schema autocomplete unavailable — editor still works */ }

  initAll(schemaMap, dialect);
  initSidebarParams();
  initPreviewMode();
  initDragReorder();
  initCollapse();
  initRunAll();
  initExportPdf();

  // Inject @params into every SQL run request
  document.body.addEventListener('htmx:configRequest', e => {
    if (!e.detail.elt.classList.contains('btn-run')) return;
    const params = getAllParams();
    for (const [k, v] of Object.entries(params)) {
      e.detail.parameters[`param_${k}`] = v;
    }
  });

  // Re-init editors injected by HTMX after initial load
  document.body.addEventListener('htmx:afterSwap', () => {
    initAll(schemaMap, dialect);
  });
}

document.addEventListener('DOMContentLoaded', boot);
