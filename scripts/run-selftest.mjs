// Runs selftest.html in headless Chrome over the DevTools protocol, prints a summary of
// the results JSON, saves a screenshot of the contact sheet and exits nonzero on any failure.
// Usage: node --experimental-websocket scripts/run-selftest.mjs [url] [--angle=metal|swiftshader] [--out=dir]

import { spawn } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';

const CHROME = process.env.CHROME ?? '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const PORT = 9333;
const TIMEOUT_MS = 15 * 60 * 1000;

const args = process.argv.slice(2);
const flag = (name, fallback) => args.find((a) => a.startsWith(`--${name}=`))?.split('=')[1] ?? fallback;
const url = args.find((a) => !a.startsWith('--')) ?? 'http://localhost:5175/selftest.html';
const angle = flag('angle', 'metal');
const outDir = resolve(flag('out', 'tmp'));

if (typeof WebSocket === 'undefined') {
  console.error('No global WebSocket. Run with: node --experimental-websocket scripts/run-selftest.mjs');
  process.exit(2);
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

const profile = mkdtempSync(join(tmpdir(), 'selftest-chrome-'));
const chromeArgs = [
  '--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${profile}`,
  `--use-angle=${angle}`, '--enable-gpu', '--window-size=1400,1000', '--hide-scrollbars',
  '--no-first-run', '--no-default-browser-check',
];
if (angle === 'swiftshader') chromeArgs.push('--enable-unsafe-swiftshader');
chromeArgs.push('about:blank');
const chrome = spawn(CHROME, chromeArgs, { stdio: 'ignore' });

function cleanup() {
  chrome.kill('SIGKILL');
  try { rmSync(profile, { recursive: true, force: true }); } catch { /* best effort */ }
}

async function pageSocketUrl() {
  for (let i = 0; i < 100; i++) {
    try {
      const list = await (await fetch(`http://127.0.0.1:${PORT}/json/list`)).json();
      const page = list.find((t) => t.type === 'page');
      if (page) return page.webSocketDebuggerUrl;
    } catch { /* Chrome still starting */ }
    await sleep(200);
  }
  throw new Error('Chrome did not expose a DevTools page');
}

function cdpClient(ws) {
  let id = 0;
  const pending = new Map();
  ws.addEventListener('message', (ev) => {
    const msg = JSON.parse(ev.data);
    const p = msg.id && pending.get(msg.id);
    if (!p) return;
    pending.delete(msg.id);
    if (msg.error) p.reject(new Error(`${msg.error.message}`));
    else p.resolve(msg.result);
  });
  return (method, params = {}) =>
    new Promise((resolve, reject) => {
      pending.set(++id, { resolve, reject });
      ws.send(JSON.stringify({ id, method, params }));
    });
}

async function main() {
  const ws = new WebSocket(await pageSocketUrl());
  await new Promise((res, rej) => {
    ws.addEventListener('open', res);
    ws.addEventListener('error', () => rej(new Error('DevTools socket failed')));
  });
  const send = cdpClient(ws);
  const evaluate = async (expression) => (await send('Runtime.evaluate', { expression, returnByValue: true })).result.value;

  await send('Page.enable');
  await send('Page.navigate', { url });

  const start = Date.now();
  while (Date.now() - start < TIMEOUT_MS) {
    await sleep(1000);
    if ((await evaluate('document.title')) === 'SELFTEST DONE') break;
  }
  if ((await evaluate('document.title')) !== 'SELFTEST DONE') throw new Error('Timed out waiting for the selftest');

  const results = JSON.parse(await evaluate("document.getElementById('results').textContent"));

  const size = await evaluate('JSON.stringify([document.documentElement.scrollWidth, document.documentElement.scrollHeight])');
  const [width, height] = JSON.parse(size);
  await send('Emulation.setDeviceMetricsOverride', { width: Math.max(width, 1400), height, deviceScaleFactor: 1, mobile: false });
  const shot = await send('Page.captureScreenshot', { format: 'png' });
  mkdirSync(outDir, { recursive: true });
  const shotPath = join(outDir, 'selftest-contact-sheet.png');
  writeFileSync(shotPath, Buffer.from(shot.data, 'base64'));
  const jsonPath = join(outDir, 'selftest-results.json');
  writeFileSync(jsonPath, JSON.stringify(results, null, 2));
  ws.close();
  return { results, shotPath, jsonPath };
}

try {
  const { results, shotPath, jsonPath } = await main();
  if (results.fatal) {
    console.error(`FATAL: ${results.fatal}`);
    process.exitCode = 1;
  } else {
    console.log(`Renderer: ${results.renderer} (ANGLE ${angle}), ${results.seconds}s`);
    for (const s of results.shaders) {
      const failing = s.controls.filter((c) => !c.pass);
      const anim = s.animates ? `${s.animates.nativePct}%/${s.animates.squarePct}%` : 'n/a';
      console.log(`${s.pass ? 'PASS' : 'FAIL'}  ${s.fileName}  compile=${s.compiled ? 'ok' : 'ERR'}  animates=${anim}  controls=${s.controls.length}  failing=${failing.length}`);
      if (s.error) console.log(`      compile error: ${s.error}`);
      for (const c of failing) {
        console.log(`      FAIL control "${c.label}" (${c.name}, ${c.type}): max ${c.maxChangedPixels}px = ${c.maxChangedPct}% (best ctx ${c.context}, alt ${c.bestAlternative})`);
      }
      for (const c of s.controls.filter((x) => x.pass && x.context !== 'default')) {
        console.log(`      note: "${c.label}" only passes in context ${c.context} (${c.maxChangedPixels}px)`);
      }
    }
    console.log(`Broken shader: ${results.broken.pass ? 'PASS' : 'FAIL'} (expected line ${results.broken.expectedLine}, reported ${results.broken.reportedLines.join(',')})`);
    console.log(results.broken.log);
    console.log(`Screenshot: ${shotPath}\nResults:    ${jsonPath}`);
    console.log(results.pass ? 'SELFTEST PASSED' : 'SELFTEST FAILED');
    if (!results.pass) process.exitCode = 1;
  }
} catch (e) {
  console.error(e.message);
  process.exitCode = 1;
} finally {
  cleanup();
}
