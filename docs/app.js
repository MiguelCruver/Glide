(() => {
  'use strict';
  const screens = {
    home: { image: 'app-home.png', alt: 'Tela inicial do Glide com os módulos de áudio, vídeo, notificações e Glide Connect', description: 'O seu ponto de partida. Acesse música, vídeos, mensagens e comunicação em um só hub.' },
    music: { image: 'app-music.png', alt: 'Módulo de áudio do Glide com YouTube Music, opção Spotify e controles do miniplayer', description: 'Sua música acompanha o jogo. Alterne entre YouTube Music e Spotify e acesse o miniplayer.' },
    connect: { image: 'app-connect.png', alt: 'Glide Connect com sala de voz, chat privado e opção de compartilhamento de tela', description: 'Um espaço para o squad. Reúna voz, chat e compartilhamento de tela no Glide Connect.' },
    messages: { image: 'app-messages.png', alt: 'Central do Glide mostrando notificações organizadas por jogos, comunicação e sistema', description: 'Acompanhe o que importa. Explore a central de notificações e seus filtros por categoria.' }
  };
  const tabs = [...document.querySelectorAll('[data-screen]')];
  const screenshot = document.querySelector('#app-screenshot');
  function selectTab(tab, focus = false) {
    const screen = screens[tab.dataset.screen];
    tabs.forEach(item => { const selected = item === tab; item.setAttribute('aria-selected', String(selected)); item.tabIndex = selected ? 0 : -1; });
    screenshot.src = `assets/${screen.image}`;
    screenshot.alt = screen.alt;
    document.querySelector('#screen-description').textContent = screen.description;
    document.querySelector('#app-panel').setAttribute('aria-labelledby', tab.id);
    if (focus) tab.focus();
  }
  tabs.forEach((tab, index) => {
    tab.addEventListener('click', () => selectTab(tab));
    tab.addEventListener('keydown', event => {
      let next;
      if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
      if (event.key === 'ArrowLeft') next = (index + tabs.length - 1) % tabs.length;
      if (event.key === 'Home') next = 0;
      if (event.key === 'End') next = tabs.length - 1;
      if (next !== undefined) { event.preventDefault(); selectTab(tabs[next], true); }
    });
  });

  const dialog = document.querySelector('#download-dialog');
  let downloadUrl = null;
  let configuredDownloadUrl = null;
  try {
    const candidate = new URL(window.GLIDE_CONFIG?.downloadUrl || '');
    if (candidate.protocol === 'https:' && !candidate.username && !candidate.password) configuredDownloadUrl = candidate.href;
  } catch { /* O estado de preparação é mantido até existir um endereço válido. */ }
  function enableDownload() {
    downloadUrl = configuredDownloadUrl;
    document.querySelector('#download-status').textContent = 'Para Windows · Download direto da release oficial no GitHub.';
  }
  async function checkRelease() {
    const config = window.GLIDE_CONFIG;
    if (!configuredDownloadUrl) return;
    if (!config.releaseApiUrl) { enableDownload(); return; }
    if (typeof fetch !== 'function') return;
    try {
      const response = await fetch(config.releaseApiUrl, { signal: AbortSignal.timeout(5000), headers: { Accept: 'application/vnd.github+json' } });
      if (!response.ok) return;
      const release = await response.json();
      if (!release.draft && release.assets?.some(asset =>
        asset.name === config.assetName && asset.state === 'uploaded' && asset.size > 0 &&
        asset.browser_download_url?.startsWith('https://github.com/MiguelCruver/Glide/releases/download/')
      )) enableDownload();
    } catch { /* Mantém o aviso de preparação quando a release não pode ser verificada. */ }
  }
  checkRelease();
  document.querySelectorAll('[data-download]').forEach(button => button.addEventListener('click', () => {
    if (downloadUrl) window.location.assign(downloadUrl);
    else dialog.showModal();
  }));
  document.querySelectorAll('.dialog-close, .dialog-dismiss').forEach(button => button.addEventListener('click', () => dialog.close()));
  dialog.addEventListener('click', event => { if (event.target === dialog) { const r = dialog.getBoundingClientRect(); if (event.clientX < r.left || event.clientX > r.right || event.clientY < r.top || event.clientY > r.bottom) dialog.close(); } });

  const imageDialog = document.querySelector('#image-dialog');
  const imageSize = document.querySelector('#image-size');
  document.querySelector('.screenshot-open').addEventListener('click', () => {
    const fullImage = document.querySelector('#full-screenshot');
    fullImage.src = screenshot.src;
    fullImage.alt = screenshot.alt;
    imageDialog.dataset.original = 'false';
    imageSize.setAttribute('aria-pressed', 'false');
    imageSize.textContent = 'Tamanho original';
    imageDialog.showModal();
    document.querySelector('.image-viewport').scrollTo(0, 0);
  });
  imageSize.addEventListener('click', () => {
    const original = imageDialog.dataset.original !== 'true';
    imageDialog.dataset.original = String(original);
    imageSize.setAttribute('aria-pressed', String(original));
    imageSize.textContent = original ? 'Ajustar à tela' : 'Tamanho original';
  });
  document.querySelector('#image-close').addEventListener('click', () => imageDialog.close());

  const scene = document.querySelector('#scene');
  const sceneArea = document.querySelector('.hero-scene');
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  const finePointer = window.matchMedia('(pointer: fine)');
  const motionToggle = document.querySelector('#motion-toggle');
  const motionLabel = document.querySelector('#motion-label');
  // Ativação padrão solicitada para o Glide; o controle permite pausar a interação.
  let motionEnabled = true;
  let frame = null;
  const clamp = value => Math.max(-1, Math.min(1, value));
  let position = { x: 0, y: 0 };
  function renderPosition(x, y) {
    position = { x: clamp(x), y: clamp(y) };
    if (frame) cancelAnimationFrame(frame);
    frame = requestAnimationFrame(() => {
      scene.style.setProperty('--rx', `${-position.y * 7}deg`);
      scene.style.setProperty('--ry', `${position.x * 10}deg`);
      scene.style.setProperty('--px', `${position.x * 9}px`);
      scene.style.setProperty('--py', `${position.y * 7}px`);
      scene.style.setProperty('--light-x', `${50 + position.x * 35}%`);
      scene.style.setProperty('--light-y', `${50 + position.y * 35}%`);
      frame = null;
    });
  }
  function resetScene() {
    if (frame) cancelAnimationFrame(frame);
    frame = null; position = { x: 0, y: 0 };
    for (const property of ['--rx', '--ry']) scene.style.setProperty(property, '0deg');
    for (const property of ['--px', '--py']) scene.style.setProperty(property, '0px');
    scene.style.setProperty('--light-x', '50%'); scene.style.setProperty('--light-y', '50%');
  }
  function updateMotion() {
    scene.dataset.motion = String(motionEnabled);
    motionToggle.setAttribute('aria-pressed', String(motionEnabled));
    motionLabel.textContent = motionEnabled ? 'Efeito 3D ativo' : 'Ativar efeito 3D';
    resetScene();
  }
  motionToggle.addEventListener('click', () => { motionEnabled = !motionEnabled; updateMotion(); });
  sceneArea.addEventListener('pointermove', event => {
    if (!motionEnabled || !finePointer.matches) return;
    const rect = sceneArea.getBoundingClientRect();
    renderPosition(((event.clientX - rect.left) / rect.width - .5) * 2, ((event.clientY - rect.top) / rect.height - .5) * 2);
  });
  sceneArea.addEventListener('keydown', event => {
    if (!motionEnabled) return;
    const keys = { ArrowLeft: [-.2, 0], ArrowRight: [.2, 0], ArrowUp: [0, -.2], ArrowDown: [0, .2] };
    if (keys[event.key]) { event.preventDefault(); const [dx, dy] = keys[event.key]; renderPosition(position.x + dx, position.y + dy); }
    if (event.key === 'Home') { event.preventDefault(); resetScene(); }
  });
  sceneArea.addEventListener('pointerleave', resetScene);
  sceneArea.addEventListener('blur', resetScene);
  reducedMotion.addEventListener('change', resetScene);
  updateMotion();
})();
