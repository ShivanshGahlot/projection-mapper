// IndexedDB-backed project and asset storage (raw API, small promise wrapper).

import type { Project } from '../state/types';
import type { AssetStorage, ProjectStorage, ProjectSummary } from './api';

const DB_NAME = 'projection-mapper';
const DB_VERSION = 1;
const LAST_OPENED_KEY = 'projection-mapper:lastOpenedId';

interface ProjectRecord {
  id: string;
  project: Project;
  summary: ProjectSummary;
}

interface AssetRecord {
  id: string;
  blob: Blob;
  fileName: string;
  mimeType: string;
}

let dbPromise: Promise<IDBDatabase> | null = null;

function openDb(): Promise<IDBDatabase> {
  if (!dbPromise) {
    dbPromise = new Promise((resolve, reject) => {
      const req = indexedDB.open(DB_NAME, DB_VERSION);
      req.onupgradeneeded = () => {
        const db = req.result;
        db.createObjectStore('projects', { keyPath: 'id' });
        db.createObjectStore('assets', { keyPath: 'id' });
      };
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    });
    dbPromise.catch(() => {
      dbPromise = null;
    });
  }
  return dbPromise;
}

/** Runs fn inside a transaction and resolves with its request's result once the transaction completes. */
async function run<T>(store: string, mode: IDBTransactionMode, fn: (s: IDBObjectStore) => IDBRequest<T>): Promise<T> {
  const db = await openDb();
  return new Promise<T>((resolve, reject) => {
    const tx = db.transaction(store, mode);
    const req = fn(tx.objectStore(store));
    tx.oncomplete = () => resolve(req.result);
    tx.onerror = () => reject(tx.error ?? req.error);
    tx.onabort = () => reject(tx.error ?? new Error('IndexedDB transaction aborted'));
  });
}

export const projectStorage: ProjectStorage = {
  async list() {
    const records = await run<ProjectRecord[]>('projects', 'readonly', (s) => s.getAll());
    return records.map((r) => r.summary).sort((a, b) => b.updatedAt - a.updatedAt);
  },
  async load(id) {
    const rec = await run<ProjectRecord | undefined>('projects', 'readonly', (s) => s.get(id));
    return rec ? rec.project : null;
  },
  async save(project) {
    const rec: ProjectRecord = {
      id: project.id,
      project,
      summary: { id: project.id, name: project.name, updatedAt: Date.now() },
    };
    await run('projects', 'readwrite', (s) => s.put(rec));
  },
  async remove(id) {
    await run('projects', 'readwrite', (s) => s.delete(id));
    try {
      if (localStorage.getItem(LAST_OPENED_KEY) === id) localStorage.removeItem(LAST_OPENED_KEY);
    } catch {
      // storage unavailable
    }
  },
  lastOpenedId() {
    try {
      return localStorage.getItem(LAST_OPENED_KEY);
    } catch {
      return null;
    }
  },
  setLastOpenedId(id) {
    try {
      localStorage.setItem(LAST_OPENED_KEY, id);
    } catch {
      // storage unavailable
    }
  },
};

const urlCache = new Map<string, string>();

export const assetStorage: AssetStorage = {
  async put(file, fileName) {
    const id = crypto.randomUUID();
    const rec: AssetRecord = { id, blob: file, fileName, mimeType: file.type };
    await run('assets', 'readwrite', (s) => s.put(rec));
    return id;
  },
  async getAssetUrl(assetId) {
    const cached = urlCache.get(assetId);
    if (cached) return cached;
    const rec = await run<AssetRecord | undefined>('assets', 'readonly', (s) => s.get(assetId));
    if (!rec) return null;
    const existing = urlCache.get(assetId);
    if (existing) return existing;
    const url = URL.createObjectURL(rec.blob);
    urlCache.set(assetId, url);
    return url;
  },
  async get(assetId) {
    const rec = await run<AssetRecord | undefined>('assets', 'readonly', (s) => s.get(assetId));
    return rec ? { blob: rec.blob, fileName: rec.fileName } : null;
  },
};
