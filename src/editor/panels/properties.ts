// Properties panel: edits the selected layer. The panel is rebuilt only when the
// selection or the layer's content type/shader changes; value edits just sync.

import type { BlendMode, ContentLayer } from '../../state/types';
import type { Ctx } from '../ctx';
import { h } from '../dom';
import { alignLayer, resetShape, rotateLayer } from '../geometryOps';
import { setLayerType, selectedLayer, type NewLayerType } from '../layerOps';
import { PANEL_LABELS } from '../uiState';
import { panelShell } from '../widgets/panel';
import { actionButtons, button, numberRow, pair, Section, segmented, selectRow, sliderRow, type Row } from '../widgets/rows';
import { binder, buildMask, buildShader, buildSurface, buildText, buildWebcam } from './contentSections';

const TYPES: { value: NewLayerType; label: string }[] = [
  { value: 'surface', label: 'Surface' },
  { value: 'shader', label: 'Shader' },
  { value: 'text', label: 'Text' },
  { value: 'webcam', label: 'Webcam' },
  { value: 'mask', label: 'Mask' },
];

const BLENDS: { value: BlendMode; label: string }[] = [
  { value: 'normal', label: 'Normal' },
  { value: 'add', label: 'Add (brighten)' },
  { value: 'screen', label: 'Screen' },
  { value: 'multiply', label: 'Multiply (darken)' },
];

export function createPropertiesPanel(ctx: Ctx) {
  const shell = panelShell(PANEL_LABELS.properties.title, PANEL_LABELS.properties.key, 'properties-panel');
  const empty = h('div', 'empty-hint pad', 'Select a layer to edit properties');
  shell.body.append(empty);
  let sig = '\0';
  let section: Section | null = null;
  let syncErrors: () => void = () => {};

  function signature(): string {
    const l = selectedLayer(ctx);
    if (!l) return 'none';
    if (l.kind === 'mask') return `mask:${l.id}:${ctx.store.state.scenes.flatMap((s) => s.layers.map((x) => x.name)).join('|')}`;
    const c = l.content;
    return c.type === 'shader' ? `c:${l.id}:shader:${c.builtinId}:${c.fileName}:${c.source.length}` : `c:${l.id}:${c.type}`;
  }

  function build(): void {
    syncErrors = () => {};
    const l = selectedLayer(ctx);
    if (!l) {
      section = null;
      shell.body.replaceChildren(empty);
      return;
    }
    const s = new Section('props');
    s.add(selectRow({
      label: 'Type',
      tip: 'Map a surface with a grid, then choose Mask to cut out the same mapped shape. Switching content type replaces the content.',
      options: TYPES,
      get: () => { const layer = selectedLayer(ctx); return layer?.kind === 'mask' ? 'mask' : layer?.content.type ?? 'surface'; },
      set: (v) => setLayerType(ctx, l.id, v as NewLayerType),
    }));
    if (l.kind === 'mask') {
      s.addEl(h('div', 'prop-title', `Mask: ${l.name}`));
      s.add(buildMask(ctx, l.id));
      mount(s);
      return;
    }
    const b = binder(ctx, l.id);
    const lay = () => b.layer();

    s.addEl(
      actionButtons([
        { icon: 'alignLeft', title: 'Align shape to the left edge of the canvas', run: () => b.edit((x) => alignLayer(x, ctx.store.state.canvas, 'left')) },
        { icon: 'alignCenterH', title: 'Centre shape horizontally on the canvas', run: () => b.edit((x) => alignLayer(x, ctx.store.state.canvas, 'centre')) },
        { icon: 'alignRight', title: 'Align shape to the right edge of the canvas', run: () => b.edit((x) => alignLayer(x, ctx.store.state.canvas, 'right')) },
        { icon: 'alignTop', title: 'Align shape to the top edge of the canvas', run: () => b.edit((x) => alignLayer(x, ctx.store.state.canvas, 'top')) },
        { icon: 'alignMiddle', title: 'Centre shape vertically on the canvas', run: () => b.edit((x) => alignLayer(x, ctx.store.state.canvas, 'middle')) },
        { icon: 'alignBottom', title: 'Align shape to the bottom edge of the canvas', run: () => b.edit((x) => alignLayer(x, ctx.store.state.canvas, 'bottom')) },
      ]),
    );

    let rotShown = 0;
    const rotation: Row = numberRow({
      label: 'Rotation',
      title: 'Turn the shape around its centre, in degrees. The value returns to 0 after each turn.',
      unit: '°',
      step: 1,
      min: -360,
      max: 360,
      get: () => rotShown,
      set: (v, merge) => {
        const delta = v - rotShown;
        if (delta === 0) return;
        rotShown = merge ? v : 0;
        b.edit((x) => rotateLayer(x, delta), merge ? 'rotate' : undefined);
      },
      onEnd: () => {
        rotShown = 0;
        rotation.sync();
      },
    });

    s.add(
      pair(
        segmented<'rect' | 'ellipse'>(
          [
            { value: 'rect', icon: 'square', title: 'Square / rectangle shape' },
            { value: 'ellipse', icon: 'circle', title: 'Circle / ellipse shape' },
          ],
          () => lay().shape,
          (v) => b.edit((x) => { x.shape = v; }),
        ),
        numberRow({
          label: 'Radius',
          title: 'Corner radius in pixels (rounds the corners of a rectangle)',
          unit: 'px',
          min: 0,
          max: 2000,
          step: 1,
          get: () => lay().cornerRadius,
          set: b.num('radius', (x, v) => { x.cornerRadius = v; }),
          disabled: () => lay().shape === 'ellipse',
        }),
      ),
      pair(
        numberRow({
          label: 'Opacity',
          title: 'How see-through the layer is',
          unit: '%',
          min: 0,
          max: 100,
          step: 1,
          get: () => Math.round(lay().opacity * 100),
          set: b.num('opacity', (x, v) => { x.opacity = v / 100; }),
        }),
        rotation,
      ),
      selectRow({
        label: 'Blend',
        tip: 'How this layer mixes with what is underneath it',
        options: BLENDS,
        get: () => lay().blend,
        set: (v) => b.edit((x) => { x.blend = v as BlendMode; }),
      }),
      numberRow({
        rowLabel: 'Edge feather',
        tip: 'Softens the edge of the shape, in pixels',
        unit: 'px',
        min: 0,
        max: 500,
        step: 1,
        get: () => lay().edgeFeather,
        set: b.num('feather', (x, v) => { x.edgeFeather = v; }),
      }),
      sliderRow({
        label: 'Quality',
        tip: 'Resolution of generated content. Lower is faster but softer.',
        min: 0.25,
        max: 1,
        step: 0.05,
        decimals: 2,
        def: 1,
        get: () => lay().quality,
        set: b.num('quality', (x, v) => { x.quality = v; }),
      }),
    );
    s.addEl(h('div', 'row', button('Reset shape', () => b.edit((x: ContentLayer) => resetShape(x, ctx.store.state.canvas)), 'btn small', 'Put the shape back to an upright rectangle in the middle of the canvas')));
    s.addEl(h('div', 'divider'));

    switch (l.content.type) {
      case 'surface':
        s.add(buildSurface(ctx, b));
        break;
      case 'shader': {
        const r = buildShader(ctx, b, l.id);
        syncErrors = r.syncErrors;
        s.add(r.section);
        break;
      }
      case 'text':
        s.add(buildText(ctx, b));
        break;
      case 'webcam':
        s.add(buildWebcam(ctx, b));
        break;
    }
    mount(s);
  }

  function mount(s: Section): void {
    section = s;
    shell.body.replaceChildren(s.el);
  }

  function refresh(): void {
    const next = signature();
    if (next !== sig) {
      sig = next;
      build();
    } else {
      section?.sync();
      syncErrors();
    }
  }

  return { el: shell.el, refresh, syncErrors: () => syncErrors() };
}
