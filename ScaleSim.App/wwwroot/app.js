'use strict';

// ---- host bridge -----------------------------------------------------------------------------
// In the app, `window.chrome.webview` is WebView2. Opened in a plain browser (UI development),
// messages go to `window.__devPost` if the page defines one, and `window.__receive` feeds it back.
const webview = window.chrome && window.chrome.webview;
function post(message) {
  if (webview) webview.postMessage(message);
  else if (window.__devPost) window.__devPost(message);
}

// ---- state -----------------------------------------------------------------------------------
const state = {
  models: [],
  units: [],
  ips: [],
  order: [],            // scale ids in tab order
  scales: new Map(),    // id -> latest snapshot
  logs: new Map(),      // id -> entries
  selected: null,
  ready: false,
};
const MAX_LOG_DOM = 1200;

const $ = (id) => document.getElementById(id);
const current = () => state.scales.get(state.selected);

// ---- messages from the host --------------------------------------------------------------------
function receive(msg) {
  switch (msg.type) {
    case 'init':
      state.models = msg.models;
      state.units = msg.units;
      state.ips = msg.ips;
      $('version').textContent = 'v' + msg.version;
      fillSelect($('unit-select'), state.units);
      renderAll();
      break;
    case 'added':
      if (!state.order.includes(msg.id)) state.order.push(msg.id);
      if (!state.selected) state.selected = msg.id;
      renderTabs();
      break;
    case 'removed':
      state.order = state.order.filter((x) => x !== msg.id);
      state.scales.delete(msg.id);
      state.logs.delete(msg.id);
      if (state.selected === msg.id) state.selected = state.order[0] || null;
      renderAll();
      break;
    case 'select':
      state.selected = msg.id;
      renderAll();
      break;
    case 'snapshot':
      state.scales.set(msg.id, msg);
      if (!state.order.includes(msg.id)) state.order.push(msg.id);
      if (!state.selected) state.selected = msg.id;
      renderTabs();
      if (msg.id === state.selected) renderScale();
      break;
    case 'log': {
      const list = msg.reset ? [] : state.logs.get(msg.id) || [];
      list.push(...msg.entries);
      if (list.length > 2000) list.splice(0, list.length - 2000);
      state.logs.set(msg.id, list);
      if (msg.id === state.selected) msg.reset ? rebuildLog() : appendLog(msg.entries);
      break;
    }
    case 'beep':
      if (msg.id === state.selected) flashBeep(msg.ms);
      break;
    case 'toast':
      toast(msg.text, msg.level === 'error');
      break;
    case 'ready':
      state.ready = true;
      renderAll();
      break;
  }
}
window.__receive = receive;
if (webview) webview.addEventListener('message', (e) => receive(e.data));

// ---- rendering ---------------------------------------------------------------------------------
function renderAll() {
  const any = state.order.length > 0 && current();
  $('empty').hidden = !!any || state.order.length > 0;
  $('main').hidden = !any;
  renderTabs();
  if (any) {
    renderScale();
    rebuildLog();
  }
}

function renderTabs() {
  const tabs = $('tabs');
  tabs.textContent = '';
  for (const id of state.order) {
    const s = state.scales.get(id);
    const b = document.createElement('button');
    b.type = 'button';
    b.className = 'tab' + (id === state.selected ? ' active' : '');
    const dot = document.createElement('span');
    dot.className = 'dot ' + (s ? s.status : 'stopped');
    const name = document.createElement('span');
    name.textContent = s ? s.options.name : '…';
    const port = document.createElement('span');
    port.className = 'port';
    port.textContent = s ? ':' + s.options.port : '';
    b.append(dot, name, port);
    b.addEventListener('click', () => {
      state.selected = id;
      renderAll();
    });
    tabs.append(b);
  }
}

function fillSelect(select, values, selected) {
  const want = values.map((v) => (typeof v === 'string' ? [v, v] : [String(v.id), v.name]));
  const sig = JSON.stringify(want);
  if (select.dataset.sig !== sig) {
    select.textContent = '';
    for (const [value, label] of want) select.append(new Option(label, value));
    select.dataset.sig = sig;
  }
  if (selected !== undefined && document.activeElement !== select) select.value = String(selected);
}

function modelOf(s) {
  return state.models.find((m) => m.id === s.modelId) || { modes: [], extraOptions: [], name: s.modelId, brand: '' };
}

function renderScale() {
  const s = current();
  if (!s) return;
  const model = modelOf(s);
  const o = s.options;
  const st = s.state;
  const d = s.display;
  const active = st.platforms[st.active] || st.platforms[0];

  // LCD
  const lcd = $('lcd');
  lcd.classList.toggle('unstable', d.status === 'unstable');
  lcd.classList.toggle('over', d.status === 'over');
  lcd.classList.toggle('under', d.status === 'under');
  if (d.status === 'over') { $('lcd-sign').textContent = ''; $('lcd-value').textContent = '^^^^^'; }
  else if (d.status === 'under') { $('lcd-sign').textContent = ''; $('lcd-value').textContent = 'vvvvv'; }
  else { $('lcd-sign').textContent = d.negative ? '−' : ''; $('lcd-value').textContent = d.value; }
  $('lcd-unit').textContent = d.unit;

  const stab = $('b-stable');
  const label = { stable: ['ESTABLE', 'ok'], unstable: ['INESTABLE', 'warn'], over: ['SOBRECARGA', 'err'], under: ['BAJO CERO', 'err'] }[d.status];
  stab.textContent = label[0];
  stab.className = 'badge ' + label[1];
  $('b-tare').hidden = !d.tared;
  const mode = model.modes.find((m) => m.id === st.mode);
  $('b-mode').textContent = mode ? mode.name.toUpperCase() : '';
  $('b-mode').hidden = !mode;
  $('sub-gross').textContent = d.gross + ' ' + d.unit;
  $('sub-tare').textContent = d.tare + ' ' + d.unit;
  $('sub-name').textContent = o.name + ' · ' + model.name;

  // platform selector
  $('platform-wrap').hidden = o.platforms < 2;
  fillSelect($('platform-select'), Array.from({ length: o.platforms }, (_, i) => ({ id: i, name: 'P' + (i + 1) })), st.active);

  // load
  $('base-unit-label').textContent = o.baseUnit;
  const input = $('load-input');
  if (document.activeElement !== input) input.value = active.loadText;
  const slider = $('load-slider');
  slider.max = o.maxCapacity;
  slider.step = Math.pow(10, -o.decimals);
  if (!draggingSlider) slider.value = active.load;
  const decimalsStep = Math.pow(10, -o.decimals);
  for (const b of $('step-chips').children) {
    const delta = parseFloat(b.dataset.delta);
    b.hidden = Math.abs(delta) < decimalsStep - 1e-12 || Math.abs(delta) > o.maxCapacity;
  }
  for (const b of $('preset-chips').querySelectorAll('button')) b.hidden = parseFloat(b.dataset.value) > o.maxCapacity;

  // controls
  $('stable-check').checked = st.stable;
  fillSelect($('unit-select'), state.units, st.unit);
  fillSelect($('mode-select'), model.modes, st.mode);

  // connection
  const dot = $('conn-dot');
  dot.className = 'dot ' + s.status;
  $('conn-text').textContent = { listening: 'Escuchando en el puerto ' + o.port, stopped: 'Detenido', error: 'Error de red' }[s.status];
  $('conn-error').hidden = !s.statusMessage;
  $('conn-error').textContent = s.statusMessage || '';
  $('server-btn').textContent = s.status === 'listening' ? 'Detener' : 'Iniciar';
  renderIps(o.port, s.status === 'listening');
  $('client-count').textContent = s.clients.length;
  const cl = $('client-list');
  cl.textContent = '';
  for (const c of s.clients) {
    const li = document.createElement('li');
    li.textContent = c.remote + '  (desde ' + c.since + ')';
    cl.append(li);
  }
}

let renderedIps = '';
function renderIps(port, live) {
  const sig = JSON.stringify([state.ips, port, live]);
  if (sig === renderedIps) return;
  renderedIps = sig;
  const ul = $('ip-list');
  ul.textContent = '';
  for (const { ip, name } of state.ips) {
    const li = document.createElement('li');
    const code = document.createElement('code');
    code.textContent = ip + ':' + port;
    const nic = document.createElement('span');
    nic.className = 'nic';
    nic.textContent = name;
    const copy = document.createElement('button');
    copy.type = 'button';
    copy.className = 'btn small ghost';
    copy.textContent = 'Copiar';
    copy.addEventListener('click', () => copyText(ip + ':' + port, 'Dirección copiada'));
    li.append(code, nic, copy);
    ul.append(li);
  }
}

// ---- console -----------------------------------------------------------------------------------
function visible(entry) {
  return !entry.cont || $('show-cont').checked;
}

function lineElement(e) {
  const row = document.createElement('div');
  row.className = 'log-line ' + e.kind;
  const t = document.createElement('span');
  t.className = 't';
  t.textContent = e.time;
  const k = document.createElement('span');
  k.className = 'k';
  k.textContent = { rx: 'RX', tx: 'TX', info: '·', error: '!' }[e.kind];
  const body = document.createElement('span');
  // Control characters are made visible: protocols are line-oriented and CR/LF matter.
  const parts = e.text.split(/(\r|\n)/);
  for (const p of parts) {
    if (p === '\r' || p === '\n') {
      const c = document.createElement('span');
      c.className = 'ctl';
      c.textContent = p === '\r' ? '\\r' : '\\n';
      body.append(c);
    } else if (p) {
      body.append(document.createTextNode(p));
    }
  }
  row.append(t, k, body);
  if (e.client && (e.kind === 'info' || e.kind === 'error')) {
    const c = document.createElement('span');
    c.className = 'cl';
    c.textContent = '[' + e.client + ']';
    row.append(c);
  }
  return row;
}

function rebuildLog() {
  const log = $('log');
  log.textContent = '';
  const entries = (state.logs.get(state.selected) || []).filter(visible);
  appendRows(entries.slice(-MAX_LOG_DOM));
  if (!log.childElementCount) showEmptyLog();
}

function showEmptyLog() {
  const empty = document.createElement('div');
  empty.className = 'log-empty';
  empty.textContent = 'Aún no hay tráfico. Envía un comando (por ejemplo S) a la dirección de arriba.';
  $('log').append(empty);
}

function appendLog(entries) {
  const shown = entries.filter(visible);
  if (!shown.length) return;
  const log = $('log');
  log.querySelector('.log-empty')?.remove();
  const stick = $('autoscroll').checked;
  appendRows(shown);
  while (log.childElementCount > MAX_LOG_DOM) log.firstElementChild.remove();
  if (stick) log.scrollTop = log.scrollHeight;
}

function appendRows(entries) {
  const frag = document.createDocumentFragment();
  for (const e of entries) frag.append(lineElement(e));
  $('log').append(frag);
  if ($('autoscroll').checked) $('log').scrollTop = $('log').scrollHeight;
}

// ---- helpers -----------------------------------------------------------------------------------
let toastTimer;
function toast(text, isError) {
  const el = $('toast');
  el.textContent = text;
  el.className = 'toast' + (isError ? ' error' : '');
  el.hidden = false;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => (el.hidden = true), isError ? 4500 : 2200);
}

async function copyText(text, message) {
  try {
    await navigator.clipboard.writeText(text);
    toast(message);
  } catch {
    toast('No se pudo copiar al portapapeles', true);
  }
}

function flashBeep(ms) {
  const lcd = $('lcd');
  lcd.classList.add('beep');
  setTimeout(() => lcd.classList.remove('beep'), Math.min(ms, 1500));
}

function send(action, extra) {
  if (!state.selected) return;
  post({ action, id: state.selected, ...extra });
}

// ---- controls ----------------------------------------------------------------------------------
let draggingSlider = false;
let lastLoadSent = 0;

function sendLoad(value) {
  if (!Number.isFinite(value)) return;
  const now = performance.now();
  if (now - lastLoadSent < 40) return;     // slider drags fire faster than anyone can read
  lastLoadSent = now;
  send('setLoad', { value: Math.max(0, value) });
}

$('load-input').addEventListener('input', (e) => {
  const v = parseFloat(e.target.value);
  if (Number.isFinite(v)) { lastLoadSent = 0; sendLoad(v); }
});
$('load-input').addEventListener('change', (e) => {
  const v = parseFloat(e.target.value);
  if (Number.isFinite(v)) { lastLoadSent = 0; sendLoad(v); }
});
$('load-slider').addEventListener('pointerdown', () => (draggingSlider = true));
window.addEventListener('pointerup', () => {
  if (draggingSlider) {
    draggingSlider = false;
    lastLoadSent = 0;
    sendLoad(parseFloat($('load-slider').value));
  }
});
$('load-slider').addEventListener('input', (e) => {
  const v = parseFloat(e.target.value);
  $('load-input').value = v;
  sendLoad(v);
});
$('step-chips').addEventListener('click', (e) => {
  const b = e.target.closest('button');
  if (b) send('addLoad', { delta: parseFloat(b.dataset.delta) });
});
$('preset-chips').addEventListener('click', (e) => {
  const b = e.target.closest('button[data-value]');
  if (b) { lastLoadSent = 0; sendLoad(parseFloat(b.dataset.value)); }
});
$('empty-btn').addEventListener('click', () => { lastLoadSent = 0; sendLoad(0); });
$('random-btn').addEventListener('click', () => {
  const s = current();
  if (!s) return;
  const top = Math.min(s.options.maxCapacity, 1500);
  const value = Number((1 + Math.random() * (top - 1)).toFixed(s.options.decimals));
  lastLoadSent = 0;
  sendLoad(value);
});
$('zero-btn').addEventListener('click', () => send('zero'));
$('tare-btn').addEventListener('click', () => send('tare'));
$('clear-tare-btn').addEventListener('click', () => send('clearTare'));
$('stable-check').addEventListener('change', (e) => send('setStable', { value: e.target.checked }));
$('unit-select').addEventListener('change', (e) => send('setUnit', { value: e.target.value }));
$('mode-select').addEventListener('change', (e) => send('setMode', { value: parseInt(e.target.value, 10) }));
$('platform-select').addEventListener('change', (e) => send('setPlatform', { value: parseInt(e.target.value, 10) }));
$('server-btn').addEventListener('click', () => send('toggleServer'));
$('clear-log').addEventListener('click', () => send('clearLog'));
$('copy-log').addEventListener('click', () => {
  const lines = (state.logs.get(state.selected) || []).filter(visible)
    .map((e) => `${e.time} ${e.kind.toUpperCase().padEnd(5)} ${e.text.replace(/\r/g, '\\r').replace(/\n/g, '\\n')}`);
  copyText(lines.join('\n'), 'Consola copiada');
});
$('show-cont').addEventListener('change', rebuildLog);

// ---- theme -------------------------------------------------------------------------------------
function applyTheme(theme) {
  document.documentElement.dataset.theme = theme;
}
(function initTheme() {
  let saved = null;
  try { saved = localStorage.getItem('theme'); } catch { /* storage unavailable */ }
  applyTheme(saved || (window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark'));
})();
$('theme-btn').addEventListener('click', () => {
  const next = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
  applyTheme(next);
  try { localStorage.setItem('theme', next); } catch { /* storage unavailable */ }
});

// ---- add scale dialog ----------------------------------------------------------------------------
const addDialog = $('add-dialog');
function openAdd() {
  const list = $('model-list');
  list.textContent = '';
  state.models.forEach((m, i) => {
    const label = document.createElement('label');
    label.className = 'model-option';
    const radio = document.createElement('input');
    radio.type = 'radio';
    radio.name = 'model';
    radio.value = m.id;
    radio.checked = i === 0;
    const text = document.createElement('div');
    const b = document.createElement('b');
    b.textContent = m.brand + ' · ' + m.name;
    const d = document.createElement('span');
    d.textContent = m.description;
    text.append(b, d);
    label.append(radio, text);
    list.append(label);
  });
  $('add-name').value = '';
  addDialog.showModal();
}
$('add-btn').addEventListener('click', openAdd);
$('empty-add').addEventListener('click', openAdd);
$('add-cancel').addEventListener('click', () => addDialog.close());
$('add-form').addEventListener('submit', () => {
  const picked = document.querySelector('#model-list input:checked');
  if (picked) post({ action: 'addScale', modelId: picked.value, name: $('add-name').value.trim() || null });
});

// ---- settings dialog -----------------------------------------------------------------------------
const settingsDialog = $('settings-dialog');
const settingsForm = $('settings-form');

function openSettings() {
  const s = current();
  if (!s) return;
  const model = modelOf(s);
  const o = s.options;
  $('s-model').textContent = model.brand + ' · ' + model.name;

  const unitSel = settingsForm.elements.baseUnit;
  unitSel.textContent = '';
  for (const u of state.units) unitSel.append(new Option(u, u));

  for (const key of ['name', 'port', 'baseUnit', 'maxCapacity', 'decimals', 'platforms', 'serialNumber',
    'stabilizationMs', 'tareMs', 'zeroMs', 'stabilityTimeoutMs', 'continuousIntervalMs']) {
    settingsForm.elements[key].value = o[key];
  }
  settingsForm.elements.unavailable.checked = o.unavailable;

  const extra = $('s-extra-fields');
  extra.textContent = '';
  $('s-extra').hidden = model.extraOptions.length === 0;
  for (const def of model.extraOptions) {
    const value = o.extra[def.key] ?? def.default;
    const label = document.createElement('label');
    let field;
    if (def.type === 'bool') {
      label.className = 'switch-line';
      field = document.createElement('input');
      field.type = 'checkbox';
      field.checked = String(value).toLowerCase() === 'true';
      const span = document.createElement('span');
      span.textContent = def.label;
      label.append(field, span);
    } else if (def.type === 'choice') {
      field = document.createElement('select');
      for (const c of def.choices) field.append(new Option(c, c));
      field.value = value;
      label.append(def.label, field);
    } else {
      field = document.createElement('input');
      field.type = 'text';
      field.value = value;
      label.append(def.label, field);
    }
    field.dataset.extraKey = def.key;
    field.dataset.extraType = def.type;
    if (def.help) label.title = def.help;
    extra.append(label);
    if (def.help) {
      const help = document.createElement('div');
      help.className = 'muted small';
      help.textContent = def.help;
      extra.append(help);
    }
  }
  settingsDialog.showModal();
}

settingsForm.addEventListener('submit', () => {
  const f = settingsForm.elements;
  const num = (n) => parseFloat(f[n].value);
  const extra = {};
  for (const el of $('s-extra-fields').querySelectorAll('[data-extra-key]')) {
    extra[el.dataset.extraKey] = el.dataset.extraType === 'bool' ? String(el.checked) : el.value;
  }
  post({
    action: 'saveOptions',
    id: state.selected,
    options: {
      name: f.name.value, port: Math.round(num('port')), baseUnit: f.baseUnit.value, maxCapacity: num('maxCapacity'),
      decimals: Math.round(num('decimals')), platforms: Math.round(num('platforms')), serialNumber: f.serialNumber.value,
      stabilizationMs: Math.round(num('stabilizationMs')), tareMs: Math.round(num('tareMs')), zeroMs: Math.round(num('zeroMs')),
      stabilityTimeoutMs: Math.round(num('stabilityTimeoutMs')), continuousIntervalMs: Math.round(num('continuousIntervalMs')),
      unavailable: f.unavailable.checked, extra,
    },
  });
});
$('s-cancel').addEventListener('click', () => settingsDialog.close());
$('s-delete').addEventListener('click', () => {
  const s = current();
  if (s && confirm('¿Eliminar la báscula «' + s.options.name + '»?')) {
    settingsDialog.close();
    send('removeScale');
  }
});
$('settings-btn').addEventListener('click', openSettings);

// Refresh the address list when the window regains focus (the PC may have changed networks).
window.addEventListener('focus', () => post({ action: 'refreshNetwork' }));

post({ action: 'ready' });
