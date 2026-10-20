// Project-level actions: new, duplicate, open, save, export/import, delete.

import { PROJECT_FILE_EXTENSION, downloadBlob, exportProjectFile, importProjectFile } from '../persist/projectFile';
import { assetStorage, projectStorage } from '../persist/db';
import type { Autosave } from '../persist/autosave';
import { createProject, newId } from '../state/defaults';
import type { Project } from '../state/types';
import type { Ctx, ProjectActions } from './ctx';
import { createDemoProject } from './demo';
import { h } from './dom';
import { iconButton } from './widgets/rows';
import { confirmDialog, openModal, pickFile } from './widgets/overlays';

export function createProjectActions(base: Omit<Ctx, 'projects'>, getAutosave: () => Autosave): ProjectActions {
  const { store, ui } = base;

  async function open(project: Project): Promise<void> {
    await getAutosave().flush();
    await projectStorage.save(project);
    projectStorage.setLastOpenedId(project.id);
    store.replace(project);
    ui.select(null);
  }

  const toast = (msg: string) => {
    const t = h('div', 'toast', msg);
    document.body.append(t);
    setTimeout(() => t.remove(), 3500);
  };

  const actions: ProjectActions = {
    async newProject() {
      await open(createProject());
    },
    async duplicateProject() {
      await getAutosave().flush();
      const copy = structuredClone(store.state);
      copy.id = newId();
      copy.name = `${copy.name} copy`;
      await open(copy);
    },
    openProjectsDialog() {
      void showProjects();
    },
    async save() {
      await getAutosave().flush();
    },
    async exportProject() {
      try {
        const blob = await exportProjectFile(store.state, assetStorage);
        downloadBlob(blob, `${store.state.name}${PROJECT_FILE_EXTENSION}`);
      } catch (e) {
        toast(`Could not export: ${(e as Error).message}`);
      }
    },
    importProject() {
      void (async () => {
        const file = await pickFile('.json,application/json');
        if (!file) return;
        try {
          const project = await importProjectFile(file, assetStorage);
          project.id = newId();
          await open(project);
        } catch (e) {
          toast(`Could not import: ${(e as Error).message}`);
        }
      })();
    },
    async deleteProject() {
      const p = store.state;
      if (!(await confirmDialog('Delete project', `Delete "${p.name}"? This cannot be undone.`, 'Delete', true))) return;
      await getAutosave().flush();
      await projectStorage.remove(p.id);
      const rest = (await projectStorage.list()).sort((a, b) => b.updatedAt - a.updatedAt);
      const next = rest[0] ? await projectStorage.load(rest[0].id) : null;
      await open(next ?? createDemoProject());
    },
    renameProject(name) {
      store.update((p) => { p.name = name; });
    },
  };

  async function showProjects(): Promise<void> {
    await getAutosave().flush();
    const body = h('div', 'modal-body project-list');
    let close = () => {};
    const render = async () => {
      const list = (await projectStorage.list()).sort((a, b) => b.updatedAt - a.updatedAt);
      body.replaceChildren(
        ...list.map((s) => {
          const current = s.id === store.state.id;
          const name = h('span', 'proj-name', s.name + (current ? '  (open)' : ''));
          const when = h('span', 'proj-time', `Updated ${new Date(s.updatedAt).toLocaleString()}`);
          const openBtn = h('button', 'btn small', 'Open');
          openBtn.type = 'button';
          openBtn.disabled = current;
          openBtn.addEventListener('click', async () => {
            const p = await projectStorage.load(s.id);
            if (p) {
              close();
              await open(p);
            }
          });
          const rename = iconButton('text', 'Rename', async () => {
            const n = window.prompt('Project name', s.name)?.trim();
            if (!n) return;
            if (current) actions.renameProject(n);
            else {
              const p = await projectStorage.load(s.id);
              if (p) {
                p.name = n;
                await projectStorage.save(p);
              }
            }
            await getAutosave().flush();
            await render();
          });
          const del = iconButton('trash', 'Delete', async () => {
            if (current) {
              close();
              await actions.deleteProject();
              return;
            }
            if (!(await confirmDialog('Delete project', `Delete "${s.name}"? This cannot be undone.`, 'Delete', true))) return;
            await projectStorage.remove(s.id);
            showProjects().catch(() => {});
          });
          del.classList.add('danger');
          return h('div', 'proj-row', h('div', 'proj-info', name, when), h('div', 'proj-actions', openBtn, rename, del));
        }),
      );
    };
    close = openModal('Projects', body, 520);
    await render();
  }

  return actions;
}
