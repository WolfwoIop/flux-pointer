/**
 * Flux Pointer
 * A dependency-free, shape-shifting cursor with a particle trail and click burst.
 */

export class FluxCursor {
  constructor(options = {}) {
    this.options = {
      maxParticles: 150,
      hueSpeed: 0.042,
      easing: 0.28,
      interactiveSelector:
        'a, button, input, textarea, select, [data-cursor="hover"]',
      ...options,
    };

    this.active = false;
    this.frame = 0;
    this.pixelRatio = 1;
    this.lastHoverState = false;
    this.particles = [];
    this.ripples = [];
    this.target = { x: window.innerWidth / 2, y: window.innerHeight / 2 };
    this.current = { ...this.target };
    this.previousTarget = { ...this.target };

    this.handleResize = this.handleResize.bind(this);
    this.handlePointerMove = this.handlePointerMove.bind(this);
    this.handlePointerDown = this.handlePointerDown.bind(this);
    this.handlePointerUp = this.handlePointerUp.bind(this);
    this.handlePointerLeave = this.handlePointerLeave.bind(this);
    this.animate = this.animate.bind(this);

    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', () => this.start(), {
        once: true,
      });
    } else {
      this.start();
    }
  }

  start() {
    if (this.active) return this;

    const finePointer = window.matchMedia('(pointer: fine)').matches;
    const reducedMotion = window.matchMedia(
      '(prefers-reduced-motion: reduce)',
    ).matches;

    if (!finePointer || reducedMotion) return this;

    this.canvas = document.createElement('canvas');
    this.canvas.className = 'flux-particles';
    this.canvas.setAttribute('aria-hidden', 'true');

    this.cursor = document.createElement('div');
    this.cursor.className = 'flux-cursor';
    this.cursor.setAttribute('aria-hidden', 'true');
    this.cursor.innerHTML = `
      <span class="flux-orbit"></span>
      <span class="flux-shell"><span class="flux-core"></span></span>
      <span class="flux-caption">SHIFT</span>
      <span class="flux-click-ring"></span>
    `;

    document.body.append(this.canvas, this.cursor);
    document.documentElement.classList.add('flux-cursor-enabled');

    this.context = this.canvas.getContext('2d');
    if (!this.context) return this;

    this.active = true;
    this.handleResize();

    window.addEventListener('resize', this.handleResize);
    window.addEventListener('pointermove', this.handlePointerMove, {
      passive: true,
    });
    window.addEventListener('pointerdown', this.handlePointerDown, {
      passive: true,
    });
    window.addEventListener('pointerup', this.handlePointerUp, {
      passive: true,
    });
    window.addEventListener('blur', this.handlePointerUp);
    document.addEventListener('mouseout', this.handlePointerLeave);

    this.frame = window.requestAnimationFrame(this.animate);
    return this;
  }

  handleResize() {
    if (!this.canvas || !this.context) return;
    this.pixelRatio = Math.min(window.devicePixelRatio || 1, 2);
    this.canvas.width = Math.floor(window.innerWidth * this.pixelRatio);
    this.canvas.height = Math.floor(window.innerHeight * this.pixelRatio);
    this.canvas.style.width = `${window.innerWidth}px`;
    this.canvas.style.height = `${window.innerHeight}px`;
    this.context.setTransform(
      this.pixelRatio,
      0,
      0,
      this.pixelRatio,
      0,
      0,
    );
  }

  handlePointerMove(event) {
    if (!this.active) return;

    const dx = event.clientX - this.previousTarget.x;
    const dy = event.clientY - this.previousTarget.y;
    this.target.x = event.clientX;
    this.target.y = event.clientY;
    this.addTrail(this.target.x, this.target.y, dx, dy);
    this.previousTarget.x = this.target.x;
    this.previousTarget.y = this.target.y;
    this.cursor.classList.add('is-visible');

    const source = event.target instanceof Element ? event.target : null;
    const interactive = Boolean(
      source?.closest(this.options.interactiveSelector),
    );

    if (interactive !== this.lastHoverState) {
      this.lastHoverState = interactive;
      this.cursor.classList.toggle('is-hovering', interactive);
    }
  }

  handlePointerDown() {
    if (!this.active) return;
    this.cursor.classList.add('is-pressed');
    this.restartClickRing();
    this.addBurst();
  }

  handlePointerUp() {
    this.cursor?.classList.remove('is-pressed');
  }

  handlePointerLeave(event) {
    if (!event.relatedTarget) this.cursor?.classList.remove('is-visible');
  }

  restartClickRing() {
    const ring = this.cursor.querySelector('.flux-click-ring');
    ring.classList.remove('is-active');
    void ring.offsetWidth;
    ring.classList.add('is-active');
  }

  addTrail(x, y, dx, dy) {
    const speed = Math.hypot(dx, dy);
    if (speed < 1) return;

    const amount = Math.min(4, Math.max(1, Math.floor(speed / 9)));
    const hueBase = (performance.now() * this.options.hueSpeed) % 360;

    for (let index = 0; index < amount; index += 1) {
      const jitter = (Math.random() - 0.5) * 9;
      const life = 24 + Math.random() * 22;
      this.particles.push({
        x: x - dx * Math.random() * 0.5 + jitter,
        y: y - dy * Math.random() * 0.5 + jitter,
        previousX: x,
        previousY: y,
        velocityX: -dx * 0.018 + (Math.random() - 0.5) * 0.7,
        velocityY: -dy * 0.018 + (Math.random() - 0.5) * 0.7,
        life,
        maxLife: life,
        size: 0.8 + Math.random() * 2.2,
        hue: (hueBase + Math.random() * 75) % 360,
        spin: Math.random() * Math.PI,
        burst: false,
      });
    }

    const overflow = this.particles.length - this.options.maxParticles;
    if (overflow > 0) this.particles.splice(0, overflow);
  }

  addBurst() {
    const hueBase = (performance.now() * this.options.hueSpeed) % 360;

    for (let index = 0; index < 28; index += 1) {
      const angle = (Math.PI * 2 * index) / 28 + Math.random() * 0.16;
      const force = 2.6 + Math.random() * 5.8;
      const life = 30 + Math.random() * 22;
      this.particles.push({
        x: this.target.x,
        y: this.target.y,
        previousX: this.target.x,
        previousY: this.target.y,
        velocityX: Math.cos(angle) * force,
        velocityY: Math.sin(angle) * force,
        life,
        maxLife: life,
        size: 1.2 + Math.random() * 2.8,
        hue: (hueBase + index * 4.5) % 360,
        spin: angle,
        burst: true,
      });
    }

    this.ripples.push({
      x: this.target.x,
      y: this.target.y,
      life: 34,
      maxLife: 34,
      hue: hueBase,
    });
  }

  drawStar(particle, alpha) {
    const context = this.context;
    context.save();
    context.translate(particle.x, particle.y);
    context.rotate(particle.spin);
    context.fillStyle = `hsla(${particle.hue}, 100%, 72%, ${alpha})`;
    context.beginPath();
    context.moveTo(0, -particle.size * 2.4);
    context.lineTo(particle.size * 0.65, -particle.size * 0.65);
    context.lineTo(particle.size * 2.4, 0);
    context.lineTo(particle.size * 0.65, particle.size * 0.65);
    context.lineTo(0, particle.size * 2.4);
    context.lineTo(-particle.size * 0.65, particle.size * 0.65);
    context.lineTo(-particle.size * 2.4, 0);
    context.lineTo(-particle.size * 0.65, -particle.size * 0.65);
    context.closePath();
    context.fill();
    context.restore();
  }

  animate(time) {
    if (!this.active) return;

    const easing = this.options.easing;
    this.current.x += (this.target.x - this.current.x) * easing;
    this.current.y += (this.target.y - this.current.y) * easing;

    const velocityX = this.target.x - this.current.x;
    const velocityY = this.target.y - this.current.y;
    const speed = Math.hypot(velocityX, velocityY);
    const angle = Math.atan2(velocityY, velocityX) * (180 / Math.PI);
    const stretch = 1 + Math.min(speed / 65, 0.42);

    this.cursor.style.transform = `translate3d(${this.current.x}px, ${this.current.y}px, 0)`;
    this.cursor.style.setProperty('--flux-angle', `${angle}deg`);
    this.cursor.style.setProperty('--flux-stretch', stretch.toFixed(3));
    this.cursor.style.setProperty(
      '--flux-hue',
      `${(time * this.options.hueSpeed) % 360}`,
    );

    const context = this.context;
    context.setTransform(
      this.pixelRatio,
      0,
      0,
      this.pixelRatio,
      0,
      0,
    );
    context.clearRect(0, 0, window.innerWidth, window.innerHeight);
    context.globalCompositeOperation = 'lighter';
    context.lineCap = 'round';

    for (let index = this.particles.length - 1; index >= 0; index -= 1) {
      const particle = this.particles[index];
      particle.previousX = particle.x;
      particle.previousY = particle.y;
      particle.x += particle.velocityX;
      particle.y += particle.velocityY;
      particle.velocityX *= particle.burst ? 0.945 : 0.91;
      particle.velocityY =
        particle.velocityY * (particle.burst ? 0.945 : 0.91) + 0.012;
      particle.spin += particle.burst ? 0.12 : 0.035;
      particle.life -= 1;

      const alpha = Math.max(0, particle.life / particle.maxLife);

      if (particle.burst) {
        this.drawStar(particle, alpha * 0.92);
      } else {
        context.strokeStyle = `hsla(${particle.hue}, 100%, 70%, ${alpha * 0.58})`;
        context.lineWidth = particle.size;
        context.beginPath();
        context.moveTo(particle.previousX, particle.previousY);
        context.lineTo(particle.x, particle.y);
        context.stroke();

        context.fillStyle = `hsla(${particle.hue}, 100%, 76%, ${alpha})`;
        context.beginPath();
        context.arc(
          particle.x,
          particle.y,
          particle.size * alpha,
          0,
          Math.PI * 2,
        );
        context.fill();
      }

      if (particle.life <= 0) this.particles.splice(index, 1);
    }

    for (let index = this.ripples.length - 1; index >= 0; index -= 1) {
      const ripple = this.ripples[index];
      const progress = 1 - ripple.life / ripple.maxLife;
      context.strokeStyle = `hsla(${ripple.hue}, 100%, 72%, ${(1 - progress) * 0.72})`;
      context.lineWidth = 1.6 - progress;
      context.setLineDash([5 + progress * 7, 8]);
      context.lineDashOffset = time * -0.02;
      context.beginPath();
      context.arc(
        ripple.x,
        ripple.y,
        12 + progress * 66,
        0,
        Math.PI * 2,
      );
      context.stroke();
      ripple.life -= 1;
      if (ripple.life <= 0) this.ripples.splice(index, 1);
    }

    context.setLineDash([]);
    context.globalCompositeOperation = 'source-over';
    this.frame = window.requestAnimationFrame(this.animate);
  }

  destroy() {
    if (!this.active) return;
    this.active = false;
    window.cancelAnimationFrame(this.frame);
    window.removeEventListener('resize', this.handleResize);
    window.removeEventListener('pointermove', this.handlePointerMove);
    window.removeEventListener('pointerdown', this.handlePointerDown);
    window.removeEventListener('pointerup', this.handlePointerUp);
    window.removeEventListener('blur', this.handlePointerUp);
    document.removeEventListener('mouseout', this.handlePointerLeave);
    document.documentElement.classList.remove('flux-cursor-enabled');
    this.canvas?.remove();
    this.cursor?.remove();
  }
}

export function initFluxCursor(options = {}) {
  return new FluxCursor(options);
}
