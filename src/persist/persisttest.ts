// Browser test for persist/: reports PASS/FAIL in document.title and #out.

import { activeScene, createContentLayer, createProject } from '../state/defaults';
import { startAutosave } from './autosave';
import { assetStorage, projectStorage } from './db';
import { exportProjectFile, importProjectFile } from './projectFile';

const lines: string[] = [];
let failed = 0;
function check(name: string, ok: boolean): void {
  if (!ok) failed++;
  lines.push(`${ok ? 'PASS' : 'FAIL'} ${name}`);
}

async function main(): Promise<void> {
  const project = createProject('Persist Test');
  const assetId = await assetStorage.put(new Blob(['hello assets'], { type: 'image/png' }), 'a.png');
  activeScene(project).layers.push(
    createContentLayer('Img', { type: 'surface', fill: { type: 'image', assetId, fileName: 'a.png' } }, project.canvas),
  );

  await projectStorage.save(project);
  const list = await projectStorage.list();
  check('list contains project', list.some((s) => s.id === project.id && s.name === 'Persist Test'));
  const loaded = await projectStorage.load(project.id);
  check('load roundtrip', JSON.stringify(loaded) === JSON.stringify(project));
  projectStorage.setLastOpenedId(project.id);
  check('lastOpenedId', projectStorage.lastOpenedId() === project.id);

  const url = await assetStorage.getAssetUrl(assetId);
  check('asset url cached', !!url && url === (await assetStorage.getAssetUrl(assetId)));
  check('missing asset null', (await assetStorage.getAssetUrl('nope')) === null);

  const blob = await exportProjectFile(project, assetStorage);
  const file = new File([blob], 'p.mapping.json');
  const imported = await importProjectFile(file, assetStorage);
  const fill = (activeScene(imported).layers[0] as { content: { fill: { assetId: string } } }).content.fill;
  check('import new project id', imported.id !== project.id && imported.name === 'Persist Test');
  check('import new asset id', fill.assetId !== assetId);
  const a = await assetStorage.get(fill.assetId);
  check('imported asset retrievable', !!a && a.fileName === 'a.png' && (await a.blob.text()) === 'hello assets');
  let rejected = false;
  try {
    await importProjectFile(new File(['{"format":"x"}'], 'bad.json'), assetStorage);
  } catch {
    rejected = true;
  }
  check('bad file rejected', rejected);

  let cb: () => void = () => {};
  const auto = startAutosave(() => imported, (c) => {
    cb = c;
    return () => {};
  });
  cb();
  await auto.flush();
  check('autosave flush', (await projectStorage.load(imported.id)) !== null);
  auto.stop();

  await projectStorage.remove(project.id);
  await projectStorage.remove(imported.id);
  check('remove', (await projectStorage.load(project.id)) === null);
}

main()
  .catch((e) => check(`exception ${String(e)}`, false))
  .finally(() => {
    document.title = failed ? 'FAIL' : 'PASS';
    document.getElementById('out')!.textContent = `${document.title}\n${lines.join('\n')}`;
  });
