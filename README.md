# Flux Pointer

A dependency-free, shape-shifting web cursor with a chromatic particle trail,
velocity-reactive movement, interactive hover states, and a starburst click
animation.

## Windows desktop app

Want the effect outside the browser? Download `FluxPointer.exe` from the latest
GitHub release. It adds the animated Flux halo, particle trail, and click
rupture across the Windows desktop and ordinary apps while hiding the normal
Windows cursor. The Flux pointer fades away after a few seconds of inactivity
and wakes as soon as you move again. Its tray menu includes five selectable
skins with distinct shapes, trails, and click bursts, and remembers your choice.
The overlay stays above newly focused browser tabs and ordinary windows. No
installer or administrator access is required.

See [`windows/README.md`](windows/README.md) for controls, compatibility, source,
and build instructions.

## Try the demo

Serve this folder with any static web server:

```bash
python -m http.server 8080
```

Then open `http://localhost:8080`.

## Add it to another website

Copy `cursor.js` and the **Flux Pointer component** section from `styles.css`
into your project. Include the stylesheet, then initialize the cursor as an ES
module:

```html
<link rel="stylesheet" href="/styles.css" />

<script type="module">
  import { initFluxCursor } from '/cursor.js';

  const cursor = initFluxCursor();
</script>
```

Interactive elements such as links, buttons, form fields, and anything with
`data-cursor="hover"` automatically trigger the expanded hover state.

```html
<div data-cursor="hover">The cursor reacts here</div>
```

## Configuration

`initFluxCursor()` accepts optional settings:

```js
const cursor = initFluxCursor({
  maxParticles: 120,
  hueSpeed: 0.05,
  easing: 0.24,
  interactiveSelector: 'a, button, [data-cursor="hover"]',
});
```

- `maxParticles` limits the number of trail and burst particles.
- `hueSpeed` controls how quickly the cursor cycles through colors.
- `easing` controls how tightly the cursor follows the pointer.
- `interactiveSelector` controls which elements activate hover mode.

Call `cursor.destroy()` to remove the cursor and all its event listeners.

## Customize the look

The cursor uses CSS custom properties and classes in `styles.css`. The most
useful values to edit are:

```css
:root {
  --flux-ink: #f4f1e8;
  --flux-acid: #d8ff3e;
  --flux-night: #0a0a0c;
}
```

The animated pieces are `.flux-shell`, `.flux-core`, `.flux-orbit`, and
`.flux-click-ring`.

## Accessibility

- The custom cursor only activates for fine pointer devices.
- Touch devices keep their normal interaction behavior.
- `prefers-reduced-motion: reduce` disables the cursor and nonessential motion.
- Keyboard focus outlines remain visible.

## Files

- `cursor.js` — reusable cursor class and particle engine.
- `styles.css` — cursor styles plus the demo page design.
- `index.html` — dependency-free demo and integration example.
