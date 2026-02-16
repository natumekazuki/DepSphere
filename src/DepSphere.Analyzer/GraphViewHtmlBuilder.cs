using System.Text;

namespace DepSphere.Analyzer;

public static class GraphViewHtmlBuilder
{
    public static string Build(GraphView view)
    {
        var json = GraphViewJsonSerializer.Serialize(view);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        return $$"""
<!DOCTYPE html>
<html lang="ja">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>DepSphere Graph Viewer</title>
  <style>
    html, body { margin: 0; padding: 0; overflow: hidden; background: #0b1220; }
    #dep-graph-canvas { width: 100vw; height: 100vh; display: block; }
    #overlay {
      position: fixed; top: 12px; left: 12px; color: #e2e8f0;
      background: rgba(15, 23, 42, 0.82); padding: 10px 12px; border-radius: 10px;
      font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 12px;
      border: 1px solid rgba(148, 163, 184, 0.25);
      min-width: 240px;
      z-index: 10;
    }
    .overlay-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
    }
    #overlay .overlay-header {
      margin-bottom: 8px;
    }
    #overlay .overlay-title {
      margin: 0;
      font-size: 13px;
      font-weight: 600;
    }
    #overlay .control { margin-top: 8px; }
    #overlay label { display: block; margin-bottom: 2px; color: #cbd5e1; }
    #overlay input[type='range'] { width: 100%; }
    .overlay-icon-toggle {
      border: 1px solid rgba(148, 163, 184, 0.45);
      background: rgba(30, 41, 59, 0.85);
      color: #e2e8f0;
      border-radius: 6px;
      width: 24px;
      height: 24px;
      line-height: 1;
      padding: 0;
      font-size: 13px;
      cursor: pointer;
      flex: 0 0 auto;
    }
    #overlay .toolbar {
      display: flex;
      gap: 6px;
      margin-top: 10px;
    }
    #overlay .toolbar button {
      flex: 1;
      border: 1px solid rgba(148, 163, 184, 0.45);
      background: rgba(30, 41, 59, 0.85);
      color: #e2e8f0;
      border-radius: 6px;
      padding: 6px 8px;
      font-size: 12px;
      cursor: pointer;
    }
    #overlay .toolbar button:disabled {
      opacity: 0.45;
      cursor: default;
    }
    #search-wrap {
      display: flex;
      gap: 6px;
      margin-top: 10px;
    }
    #search-input {
      flex: 1;
      min-width: 0;
      border: 1px solid rgba(148, 163, 184, 0.4);
      border-radius: 6px;
      padding: 6px 8px;
      font-size: 12px;
      background: rgba(2, 6, 23, 0.72);
      color: #e2e8f0;
      outline: none;
    }
    #search-input::placeholder { color: rgba(226, 232, 240, 0.55); }
    #search-button {
      border: 1px solid rgba(148, 163, 184, 0.45);
      background: rgba(30, 41, 59, 0.85);
      color: #e2e8f0;
      border-radius: 6px;
      padding: 6px 10px;
      font-size: 12px;
      cursor: pointer;
    }
    #kind-filter {
      margin-top: 10px;
    }
    #kind-filter-label {
      margin-bottom: 4px;
      color: #cbd5e1;
    }
    #kind-filter-list {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 4px 8px;
    }
    .kind-item {
      display: flex;
      align-items: center;
      gap: 6px;
      min-width: 0;
    }
    .kind-item input {
      margin: 0;
    }
    .kind-item span {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    #kind-filter-actions {
      display: flex;
      gap: 6px;
      margin-top: 6px;
    }
    #kind-filter-actions button {
      flex: 1;
      border: 1px solid rgba(148, 163, 184, 0.45);
      background: rgba(30, 41, 59, 0.85);
      color: #e2e8f0;
      border-radius: 6px;
      padding: 5px 8px;
      font-size: 11px;
      cursor: pointer;
    }
    #project-filter {
      margin-top: 10px;
    }
    #project-filter-label {
      margin-bottom: 4px;
      color: #cbd5e1;
    }
    #project-filter-list {
      display: grid;
      grid-template-columns: minmax(0, 1fr);
      gap: 4px;
      max-height: 110px;
      overflow-y: auto;
      padding-right: 2px;
    }
    .project-item {
      display: flex;
      align-items: center;
      gap: 6px;
      min-width: 0;
    }
    .project-item input {
      margin: 0;
    }
    .project-item span {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    #project-filter-actions {
      display: flex;
      gap: 6px;
      margin-top: 6px;
    }
    #project-filter-actions button {
      flex: 1;
      border: 1px solid rgba(148, 163, 184, 0.45);
      background: rgba(30, 41, 59, 0.85);
      color: #e2e8f0;
      border-radius: 6px;
      padding: 5px 8px;
      font-size: 11px;
      cursor: pointer;
    }
    #filter-status {
      margin-top: 10px;
      color: #cbd5e1;
      font-size: 11px;
      line-height: 1.35;
    }
    #clear-filter {
      border: 1px solid rgba(148, 163, 184, 0.45);
      background: rgba(30, 41, 59, 0.85);
      color: #e2e8f0;
      border-radius: 6px;
      padding: 6px 8px;
      font-size: 12px;
      cursor: pointer;
    }
    #clear-filter:disabled {
      opacity: 0.45;
      cursor: default;
    }
    #node-info {
      position: fixed; right: 12px; top: 12px; color: #e2e8f0;
      background: rgba(2, 6, 23, 0.82); padding: 10px 12px; border-radius: 10px;
      border: 1px solid rgba(148, 163, 184, 0.25);
      font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 12px;
      max-width: 340px;
      white-space: pre-wrap;
      z-index: 10;
    }
    #node-info .title {
      font-weight: 600;
      margin: 0;
    }
    #node-info .overlay-header {
      margin-bottom: 6px;
    }
    .overlay-content-hidden {
      display: none;
    }
    .overlay-collapsed {
      min-width: 0;
    }
  </style>
</head>
<body>
  <canvas id="dep-graph-canvas"></canvas>
  <div id="overlay">
    <div class="overlay-header">
      <h1 class="overlay-title">DepSphere 3D Graph</h1>
      <button id="overlay-toggle" class="overlay-icon-toggle" type="button" aria-controls="overlay-body" aria-expanded="true" aria-label="操作UIを折りたたむ">▽</button>
    </div>
    <div id="overlay-body">
      <div>シングルクリック: 接続ノードに限定</div>
      <div>ダブルクリック: コード表示</div>
      <div>ホバー: ノード強調</div>
      <div>ラベルLOD: 遠景は重要ノード中心</div>
      <div>右ドラッグ: 回転 / 左ドラッグ: 平行移動 / ホイール: ズーム</div>
      <div class="control">
        <label for="node-scale">ノード倍率</label>
        <input id="node-scale" type="range" min="0.6" max="2.4" step="0.1" value="1.0" />
      </div>
      <div class="control">
        <label for="spread-scale">距離倍率</label>
        <input id="spread-scale" type="range" min="0.6" max="2.6" step="0.1" value="1.0" />
      </div>
      <div id="search-wrap">
        <input id="search-input" type="text" placeholder="ノード名検索 (Ctrl+F)" />
        <button id="search-button" type="button">検索</button>
      </div>
      <div id="kind-filter">
        <div id="kind-filter-label">ノード種別フィルタ</div>
        <div id="kind-filter-list"></div>
        <div id="kind-filter-actions">
          <button id="kind-filter-all" type="button">全選択</button>
          <button id="kind-filter-reset" type="button">型中心</button>
        </div>
      </div>
      <div id="project-filter">
        <div id="project-filter-label">プロジェクトフィルタ</div>
        <div id="project-filter-list"></div>
        <div id="project-filter-actions">
          <button id="project-filter-all" type="button">全選択</button>
          <button id="project-filter-none" type="button">全解除</button>
        </div>
      </div>
      <div class="toolbar">
        <button id="history-back" type="button" disabled>戻る</button>
        <button id="history-forward" type="button" disabled>進む</button>
      </div>
      <div class="toolbar">
        <button id="fit-view" type="button">Fit to View</button>
        <button id="auto-spread" type="button">自動拡散</button>
        <button id="clear-filter" type="button" disabled>表示限定解除</button>
      </div>
      <div id="filter-status">表示中: 0/0</div>
    </div>
  </div>
  <div id="node-info">
    <div class="overlay-header">
      <div class="title">ノード情報</div>
      <button id="node-info-toggle" class="overlay-icon-toggle" type="button" aria-controls="node-info-content" aria-expanded="false" aria-label="ノード情報を展開">△</button>
    </div>
    <div id="node-info-content">
      <div id="node-info-body">ノードをクリックするとクラス・メソッド・プロパティ情報を表示します。</div>
    </div>
  </div>
  <script src="https://unpkg.com/three@0.160.0/build/three.min.js"></script>
  <script>
    window.__depSphereGraph = JSON.parse(atob("{{base64}}"));

    const canvas = document.getElementById('dep-graph-canvas');
    const overlayPanel = document.getElementById('overlay');
    const overlayBody = document.getElementById('overlay-body');
    const overlayToggleButton = document.getElementById('overlay-toggle');
    const nodeInfoPanel = document.getElementById('node-info');
    const nodeInfoContent = document.getElementById('node-info-content');
    const nodeInfoToggleButton = document.getElementById('node-info-toggle');
    const infoBody = document.getElementById('node-info-body');
    const nodeScaleInput = document.getElementById('node-scale');
    const spreadScaleInput = document.getElementById('spread-scale');
    const historyBackButton = document.getElementById('history-back');
    const historyForwardButton = document.getElementById('history-forward');
    const fitViewButton = document.getElementById('fit-view');
    const autoSpreadButton = document.getElementById('auto-spread');
    const clearFilterButton = document.getElementById('clear-filter');
    const filterStatus = document.getElementById('filter-status');
    const searchInput = document.getElementById('search-input');
    const searchButton = document.getElementById('search-button');
    const kindFilterList = document.getElementById('kind-filter-list');
    const kindFilterAllButton = document.getElementById('kind-filter-all');
    const kindFilterResetButton = document.getElementById('kind-filter-reset');
    const projectFilterPanel = document.getElementById('project-filter');
    const projectFilterList = document.getElementById('project-filter-list');
    const projectFilterAllButton = document.getElementById('project-filter-all');
    const projectFilterNoneButton = document.getElementById('project-filter-none');
    let isOverlayExpanded = true;
    let isNodeInfoExpanded = false;

    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x0b1220);

    const camera = new THREE.PerspectiveCamera(60, window.innerWidth / window.innerHeight, 0.1, 2600);
    camera.position.set(0, 0, 200);
    camera.lookAt(0, 0, 0);

    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    renderer.setPixelRatio(window.devicePixelRatio || 1);
    renderer.setSize(window.innerWidth, window.innerHeight);

    canvas.style.touchAction = 'none';
    canvas.addEventListener('contextmenu', (event) => {
      event.preventDefault();
    });

    const cameraControl = createCameraController(camera, canvas);

    const ambient = new THREE.AmbientLight(0xffffff, 0.78);
    scene.add(ambient);
    const keyLight = new THREE.DirectionalLight(0xffffff, 0.55);
    keyLight.position.set(80, 120, 140);
    scene.add(keyLight);

    const nodes = window.__depSphereGraph.nodes || [];
    const edges = window.__depSphereGraph.edges || [];

    const nodeMeshes = [];
    const nodeMap = new Map();
    const basePositions = new Map();
    const adjacencyMap = new Map();
    const edgeLines = [];
    const edgeArrowGeometry = new THREE.ConeGeometry(1, 1, 10);
    const edgeArrowUp = new THREE.Vector3(0, 1, 0);
    let selectedNodeId = null;
    let hoveredNodeId = null;
    let visibleNodeIds = null;
    let filterRootNodeId = null;
    let singleClickTimer = null;
    const historyEntries = [];
    const historyMax = 80;
    let historyIndex = -1;
    let isApplyingHistory = false;
    const kindOrder = ['project', 'namespace', 'file', 'type', 'method', 'property', 'field', 'event', 'external'];
    const kindLabels = {
      project: 'Project',
      namespace: 'Namespace',
      file: 'File',
      type: 'Class',
      method: 'Method',
      property: 'Property',
      field: 'Field',
      event: 'Event',
      external: 'External'
    };
    const activeNodeKinds = new Set(kindOrder);
    const kindCheckboxMap = new Map();
    const projectNodeIds = nodes
      .filter((node) => normalizeNodeKind(node.nodeKind) === 'project')
      .map((node) => node.id);
    const activeProjectNodeIds = new Set(projectNodeIds);
    const projectCheckboxMap = new Map();
    const hierarchicalEdgeKinds = new Set(['contains', 'member', 'external']);
    const projectOwnedNodeIds = buildProjectOwnedNodeIndex();
    const defaultSpreadScaleMin = Number(spreadScaleInput.min || '0.6');
    const defaultSpreadScaleMax = Number(spreadScaleInput.max || '2.6');

    function setPanelExpanded(panel, content, toggleButton, isExpanded, collapseLabel, expandLabel) {
      if (!panel || !content || !toggleButton) {
        return;
      }

      panel.classList.toggle('overlay-collapsed', !isExpanded);
      content.classList.toggle('overlay-content-hidden', !isExpanded);
      toggleButton.textContent = isExpanded ? '▽' : '△';
      toggleButton.setAttribute('aria-expanded', isExpanded ? 'true' : 'false');
      const label = isExpanded ? collapseLabel : expandLabel;
      toggleButton.setAttribute('aria-label', label);
      toggleButton.setAttribute('title', label);
    }

    function applyOverlayPanelExpanded() {
      setPanelExpanded(overlayPanel, overlayBody, overlayToggleButton, isOverlayExpanded, '操作UIを折りたたむ', '操作UIを展開');
    }

    function applyNodeInfoPanelExpanded() {
      setPanelExpanded(nodeInfoPanel, nodeInfoContent, nodeInfoToggleButton, isNodeInfoExpanded, 'ノード情報を折りたたむ', 'ノード情報を展開');
    }

    function toggleOverlayPanelExpanded() {
      isOverlayExpanded = !isOverlayExpanded;
      applyOverlayPanelExpanded();
    }

    function toggleNodeInfoPanelExpanded() {
      isNodeInfoExpanded = !isNodeInfoExpanded;
      applyNodeInfoPanelExpanded();
    }

    function normalizeNodeKind(kind) {
      if (!kind) {
        return 'type';
      }

      const normalized = String(kind).toLowerCase();
      return kindOrder.includes(normalized) ? normalized : 'type';
    }

    function isNodeKindVisible(kind) {
      return activeNodeKinds.has(normalizeNodeKind(kind));
    }

    function updateKindFilterButtons() {
      if (!kindFilterAllButton || !kindFilterResetButton) {
        return;
      }

      kindFilterAllButton.disabled = activeNodeKinds.size === kindOrder.length;
      kindFilterResetButton.disabled =
        activeNodeKinds.size === 3
        && activeNodeKinds.has('project')
        && activeNodeKinds.has('namespace')
        && activeNodeKinds.has('type');
    }

    function syncKindCheckboxes() {
      kindCheckboxMap.forEach((item, kind) => {
        item.checked = activeNodeKinds.has(kind);
      });
      updateKindFilterButtons();
    }

    function setKindEnabled(kind, enabled, applyVisual = true) {
      const normalized = normalizeNodeKind(kind);
      if (enabled) {
        activeNodeKinds.add(normalized);
      } else {
        activeNodeKinds.delete(normalized);
      }

      if (activeNodeKinds.size === 0) {
        activeNodeKinds.add('type');
      }

      syncKindCheckboxes();
      if (applyVisual) {
        applyVisualSettings();
      }
    }

    function resetKindFilterToTypeFocused() {
      activeNodeKinds.clear();
      activeNodeKinds.add('project');
      activeNodeKinds.add('namespace');
      activeNodeKinds.add('type');
      syncKindCheckboxes();
      applyVisualSettings();
    }

    function selectAllKinds() {
      activeNodeKinds.clear();
      kindOrder.forEach((kind) => activeNodeKinds.add(kind));
      syncKindCheckboxes();
      applyVisualSettings();
    }

    function initializeKindFilter() {
      if (!kindFilterList) {
        return;
      }

      kindFilterList.innerHTML = '';
      kindOrder.forEach((kind) => {
        const item = document.createElement('label');
        item.className = 'kind-item';

        const input = document.createElement('input');
        input.type = 'checkbox';
        input.checked = activeNodeKinds.has(kind);
        input.addEventListener('change', () => {
          setKindEnabled(kind, input.checked);
        });

        const text = document.createElement('span');
        text.textContent = kindLabels[kind] || kind;

        item.appendChild(input);
        item.appendChild(text);
        kindFilterList.appendChild(item);
        kindCheckboxMap.set(kind, input);
      });

      if (kindFilterAllButton) {
        kindFilterAllButton.addEventListener('click', selectAllKinds);
      }

      if (kindFilterResetButton) {
        kindFilterResetButton.addEventListener('click', resetKindFilterToTypeFocused);
      }

      syncKindCheckboxes();
    }

    function buildProjectOwnedNodeIndex() {
      const ownerMap = new Map();
      if (projectNodeIds.length === 0) {
        return ownerMap;
      }

      const outgoing = new Map();
      edges.forEach((edge) => {
        const kind = String(edge.kind || '').toLowerCase();
        if (!hierarchicalEdgeKinds.has(kind)) {
          return;
        }

        if (!outgoing.has(edge.from)) {
          outgoing.set(edge.from, []);
        }

        outgoing.get(edge.from).push(edge.to);
      });

      projectNodeIds.forEach((projectId) => {
        const queue = [projectId];
        const visited = new Set([projectId]);
        while (queue.length > 0) {
          const current = queue.shift();
          if (!ownerMap.has(current)) {
            ownerMap.set(current, new Set());
          }

          ownerMap.get(current).add(projectId);
          const targets = outgoing.get(current) || [];
          targets.forEach((targetId) => {
            if (visited.has(targetId)) {
              return;
            }

            visited.add(targetId);
            queue.push(targetId);
          });
        }
      });

      return ownerMap;
    }

    function isNodeProjectVisible(nodeId, nodeKind) {
      if (projectNodeIds.length === 0) {
        return true;
      }

      const normalizedKind = normalizeNodeKind(nodeKind);
      if (normalizedKind === 'project') {
        return activeProjectNodeIds.has(nodeId);
      }

      const owners = projectOwnedNodeIds.get(nodeId);
      if (!owners || owners.size === 0) {
        return activeProjectNodeIds.size === projectNodeIds.length;
      }

      for (const projectId of owners) {
        if (activeProjectNodeIds.has(projectId)) {
          return true;
        }
      }

      return false;
    }

    function isNodeVisibleByActiveFilters(nodeId, nodeKind) {
      const kindVisible = isNodeKindVisible(nodeKind);
      const projectVisible = isNodeProjectVisible(nodeId, nodeKind);
      const graphVisible = !visibleNodeIds || visibleNodeIds.has(nodeId);
      return kindVisible && projectVisible && graphVisible;
    }

    function updateProjectFilterButtons() {
      if (!projectFilterAllButton || !projectFilterNoneButton) {
        return;
      }

      projectFilterAllButton.disabled = activeProjectNodeIds.size === projectNodeIds.length;
      projectFilterNoneButton.disabled = activeProjectNodeIds.size === 0;
    }

    function syncProjectCheckboxes() {
      projectCheckboxMap.forEach((input, projectId) => {
        input.checked = activeProjectNodeIds.has(projectId);
      });
      updateProjectFilterButtons();
    }

    function setProjectEnabled(projectId, enabled, applyVisual = true) {
      if (enabled) {
        activeProjectNodeIds.add(projectId);
      } else {
        activeProjectNodeIds.delete(projectId);
      }

      syncProjectCheckboxes();
      if (applyVisual) {
        applyVisualSettings();
      }
    }

    function selectAllProjects() {
      activeProjectNodeIds.clear();
      projectNodeIds.forEach((projectId) => activeProjectNodeIds.add(projectId));
      syncProjectCheckboxes();
      applyVisualSettings();
    }

    function clearAllProjects() {
      activeProjectNodeIds.clear();
      syncProjectCheckboxes();
      applyVisualSettings();
    }

    function ensureProjectVisibilityForNode(nodeId) {
      const owners = projectOwnedNodeIds.get(nodeId);
      if (!owners || owners.size === 0) {
        return false;
      }

      let changed = false;
      owners.forEach((projectId) => {
        if (!activeProjectNodeIds.has(projectId)) {
          activeProjectNodeIds.add(projectId);
          changed = true;
        }
      });

      if (changed) {
        syncProjectCheckboxes();
      }

      return changed;
    }

    function initializeProjectFilter() {
      if (!projectFilterPanel || !projectFilterList) {
        return;
      }

      if (projectNodeIds.length === 0) {
        projectFilterPanel.style.display = 'none';
        return;
      }

      projectFilterPanel.style.display = '';
      projectFilterList.innerHTML = '';

      const projectNodes = nodes
        .filter((node) => normalizeNodeKind(node.nodeKind) === 'project')
        .sort((a, b) => String(a.label || a.id).localeCompare(String(b.label || b.id), 'ja'));
      projectNodes.forEach((node) => {
        const item = document.createElement('label');
        item.className = 'project-item';

        const input = document.createElement('input');
        input.type = 'checkbox';
        input.checked = activeProjectNodeIds.has(node.id);
        input.addEventListener('change', () => {
          setProjectEnabled(node.id, input.checked);
        });

        const text = document.createElement('span');
        text.textContent = String(node.label || node.id);

        item.appendChild(input);
        item.appendChild(text);
        projectFilterList.appendChild(item);
        projectCheckboxMap.set(node.id, input);
      });

      if (projectFilterAllButton) {
        projectFilterAllButton.addEventListener('click', selectAllProjects);
      }

      if (projectFilterNoneButton) {
        projectFilterNoneButton.addEventListener('click', clearAllProjects);
      }

      syncProjectCheckboxes();
    }

    function hexColor(color) {
      if (!color || !color.startsWith('#')) return 0x3b82f6;
      return parseInt(color.slice(1), 16);
    }

    function createTextSprite(text) {
      const fontSize = 36;
      const pad = 14;
      const canvasEl = document.createElement('canvas');
      const ctx = canvasEl.getContext('2d');
      if (!ctx) {
        const fallbackMaterial = new THREE.SpriteMaterial({ color: 0xe2e8f0, transparent: true, opacity: 0.85 });
        const fallback = new THREE.Sprite(fallbackMaterial);
        fallback.scale.set(18, 5, 1);
        fallback.userData.baseScale = new THREE.Vector3(18, 5, 1);
        fallback.renderOrder = 10;
        return fallback;
      }

      ctx.font = `600 ${fontSize}px sans-serif`;
      const textWidth = Math.max(30, Math.ceil(ctx.measureText(text).width));
      canvasEl.width = textWidth + pad * 2;
      canvasEl.height = fontSize + pad * 2;

      ctx.font = `600 ${fontSize}px sans-serif`;
      ctx.fillStyle = 'rgba(11, 18, 32, 0.70)';
      ctx.fillRect(0, 0, canvasEl.width, canvasEl.height);
      ctx.fillStyle = '#e2e8f0';
      ctx.fillText(text, pad, fontSize + 4);

      const texture = new THREE.CanvasTexture(canvasEl);
      texture.needsUpdate = true;

      const material = new THREE.SpriteMaterial({
        map: texture,
        transparent: true,
        opacity: 0.85,
        depthWrite: false,
        depthTest: false
      });

      const sprite = new THREE.Sprite(material);
      const sx = canvasEl.width / 14;
      const sy = canvasEl.height / 14;
      sprite.scale.set(sx, sy, 1);
      sprite.userData.baseScale = new THREE.Vector3(sx, sy, 1);
      sprite.renderOrder = 10;
      return sprite;
    }

    nodes.forEach((node) => {
      const baseRadius = Math.max(2, node.size * 0.15);
      const geometry = new THREE.SphereGeometry(baseRadius, 16, 16);
      const material = new THREE.MeshStandardMaterial({ color: hexColor(node.color) });
      const mesh = new THREE.Mesh(geometry, material);
      mesh.position.set(node.x || 0, node.y || 0, node.z || 0);
      mesh.userData.nodeId = node.id;
      mesh.userData.node = node;
      mesh.userData.level = node.level || 'normal';
      mesh.userData.nodeKind = node.nodeKind || 'type';
      mesh.userData.openNodeId = node.ownerNodeId || node.id;
      mesh.userData.baseRadius = baseRadius;

      const label = createTextSprite(node.label || node.id);
      label.position.set(0, baseRadius + 5, 0);
      mesh.add(label);
      mesh.userData.labelSprite = label;

      scene.add(mesh);
      nodeMeshes.push(mesh);
      nodeMap.set(node.id, mesh);
      basePositions.set(node.id, mesh.position.clone());
    });

    edges.forEach((edge) => {
      const from = nodeMap.get(edge.from);
      const to = nodeMap.get(edge.to);
      if (!from || !to) return;

      addAdjacent(edge.from, edge.to);
      addAdjacent(edge.to, edge.from);

      const points = [from.position.clone(), to.position.clone()];
      const geometry = new THREE.BufferGeometry().setFromPoints(points);
      const material = new THREE.LineBasicMaterial({ color: hexColor(edge.color), opacity: 0.68, transparent: true });
      const line = new THREE.Line(geometry, material);
      const arrowMaterial = new THREE.MeshBasicMaterial({
        color: hexColor(edge.color),
        opacity: 0.9,
        transparent: true,
        depthWrite: false
      });
      const arrow = new THREE.Mesh(edgeArrowGeometry, arrowMaterial);
      scene.add(line);
      scene.add(arrow);
      edgeLines.push({ from: edge.from, to: edge.to, line, arrow });
    });

    function addAdjacent(sourceId, targetId) {
      if (!adjacencyMap.has(sourceId)) {
        adjacencyMap.set(sourceId, new Set());
      }

      adjacencyMap.get(sourceId).add(targetId);
    }

    const raycaster = new THREE.Raycaster();
    const pointer = new THREE.Vector2();
    const dragPlane = new THREE.Plane();
    const dragPoint = new THREE.Vector3();
    const dragOffset = new THREE.Vector3();
    const dragPlaneNormal = new THREE.Vector3();

    let pointerDown = null;
    let pointerMoved = false;
    let draggedNodeMesh = null;
    let draggedPointerId = null;

    function updatePointerFromEvent(event) {
      const rect = canvas.getBoundingClientRect();
      pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
      pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
    }

    function projectPointerToPlane(event, plane, target) {
      updatePointerFromEvent(event);
      raycaster.setFromCamera(pointer, camera);
      return raycaster.ray.intersectPlane(plane, target) !== null;
    }

    canvas.addEventListener('pointerdown', (event) => {
      pointerDown = { x: event.clientX, y: event.clientY, button: event.button };
      pointerMoved = false;

      if (event.button !== 0) {
        return;
      }

      const picked = pickNodeFromEvent(event);
      if (!picked) {
        return;
      }

      draggedNodeMesh = picked;
      draggedPointerId = event.pointerId;

      dragPlaneNormal.copy(camera.position).sub(picked.position);
      if (dragPlaneNormal.lengthSq() <= 1e-8) {
        dragPlaneNormal.set(0, 0, 1);
      }
      dragPlaneNormal.normalize();
      dragPlane.setFromNormalAndCoplanarPoint(dragPlaneNormal, picked.position);

      if (projectPointerToPlane(event, dragPlane, dragPoint)) {
        dragOffset.copy(picked.position).sub(dragPoint);
      } else {
        dragOffset.set(0, 0, 0);
      }

      selectedNodeId = picked.userData.nodeId;
      hoveredNodeId = picked.userData.nodeId;
      updateNodeInfo(picked.userData.node);
      applyVisualSettings();

      if (canvas.setPointerCapture) {
        canvas.setPointerCapture(event.pointerId);
      }

      event.preventDefault();
      event.stopImmediatePropagation();
    }, true);

    canvas.addEventListener('pointermove', (event) => {
      if (draggedNodeMesh && (draggedPointerId === null || event.pointerId === draggedPointerId)) {
        if (pointerDown) {
          const dx = event.clientX - pointerDown.x;
          const dy = event.clientY - pointerDown.y;
          if ((dx * dx + dy * dy) > 25) {
            pointerMoved = true;
          }
        }

        if (projectPointerToPlane(event, dragPlane, dragPoint)) {
          const nextPosition = dragPoint.clone().add(dragOffset);
          if (nextPosition.distanceToSquared(draggedNodeMesh.position) > 1e-8) {
            draggedNodeMesh.position.copy(nextPosition);
            const spreadScale = Number(spreadScaleInput.value || '1');
            const safeSpreadScale = Math.abs(spreadScale) <= 1e-8 ? 1 : spreadScale;
            basePositions.set(draggedNodeMesh.userData.nodeId, nextPosition.clone().divideScalar(safeSpreadScale));
            pointerMoved = true;
            refreshEdges();
            updateLabelLod();
          }
        }

        event.preventDefault();
        event.stopImmediatePropagation();
        return;
      }

      if (pointerDown) {
        const dx = event.clientX - pointerDown.x;
        const dy = event.clientY - pointerDown.y;
        if ((dx * dx + dy * dy) > 25) {
          pointerMoved = true;
        }

        return;
      }

      const hovered = pickNodeFromEvent(event);
      setHoveredNode(hovered ? hovered.userData.nodeId : null);
    }, true);

    canvas.addEventListener('pointerup', (event) => {
      if (draggedNodeMesh && (draggedPointerId === null || event.pointerId === draggedPointerId)) {
        draggedNodeMesh = null;
        draggedPointerId = null;
        if (canvas.releasePointerCapture && event.pointerId !== undefined) {
          try {
            canvas.releasePointerCapture(event.pointerId);
          } catch {
            // no-op
          }
        }

        event.preventDefault();
        event.stopImmediatePropagation();
      }

      pointerDown = null;
    }, true);

    canvas.addEventListener('pointercancel', (event) => {
      if (draggedNodeMesh && (draggedPointerId === null || event.pointerId === draggedPointerId)) {
        draggedNodeMesh = null;
        draggedPointerId = null;
      }

      pointerDown = null;
    }, true);

    canvas.addEventListener('pointerleave', () => {
      if (draggedNodeMesh) {
        return;
      }

      setHoveredNode(null);
    });

    function postNodeSelected(nodeId) {
      const payload = { type: 'nodeSelected', nodeId: nodeId };
      if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
        window.chrome.webview.postMessage(payload);
      }
      if (window.__depSphereHost && typeof window.__depSphereHost.onNodeSelected === 'function') {
        window.__depSphereHost.onNodeSelected(nodeId);
      }
    }

    function updateNodeInfo(node) {
      if (!node) {
        infoBody.textContent = 'ノードをクリックするとクラス・メソッド・プロパティ情報を表示します。';
        return;
      }

      const methods = Array.isArray(node.methodNames) ? node.methodNames : [];
      const properties = Array.isArray(node.propertyNames) ? node.propertyNames : [];
      const fields = Array.isArray(node.fieldNames) ? node.fieldNames : [];
      const events = Array.isArray(node.eventNames) ? node.eventNames : [];
      const nodeKind = node.nodeKind || 'type';
      const ownerNodeId = node.ownerNodeId || node.id;
      const methodText = methods.length > 0
        ? methods.join('\n')
        : '(メソッド情報なし)';
      const propertyText = properties.length > 0
        ? properties.join('\n')
        : '(プロパティ情報なし)';
      const fieldText = fields.length > 0
        ? fields.join('\n')
        : '(フィールド情報なし)';
      const eventText = events.length > 0
        ? events.join('\n')
        : '(イベント情報なし)';

      infoBody.textContent =
        `Kind: ${nodeKind}\n` +
        `Class: ${ownerNodeId}\n` +
        `Label: ${node.label || node.id}\n` +
        `Methods:\n${methodText}\n` +
        `Properties:\n${propertyText}\n` +
        `Fields:\n${fieldText}\n` +
        `Events:\n${eventText}`;
    }

    function refreshEdges() {
      edgeLines.forEach((item) => {
        const from = nodeMap.get(item.from);
        const to = nodeMap.get(item.to);
        if (!from || !to) return;

        const visible = from.visible && to.visible;
        item.line.visible = visible;
        item.arrow.visible = visible;
        if (!visible) {
          return;
        }

        const edgeDirection = to.position.clone().sub(from.position);
        const distance = edgeDirection.length();
        if (distance <= 0.001) {
          item.line.visible = false;
          item.arrow.visible = false;
          return;
        }

        edgeDirection.normalize();

        const fromRadius = Math.max(1.2, (from.userData.baseRadius || 2) * (from.scale ? from.scale.x : 1));
        const toRadius = Math.max(1.2, (to.userData.baseRadius || 2) * (to.scale ? to.scale.x : 1));
        const arrowLength = Math.max(2.4, Math.min(7.2, distance * 0.18));
        const arrowRadius = Math.max(0.9, Math.min(2.6, arrowLength * 0.42));

        const lineStart = from.position.clone().addScaledVector(edgeDirection, fromRadius + 0.4);
        const arrowTip = to.position.clone().addScaledVector(edgeDirection, -(toRadius + 0.6));
        const lineEnd = arrowTip.clone().addScaledVector(edgeDirection, -Math.max(0.8, arrowLength * 0.95));

        if (lineStart.distanceTo(lineEnd) <= 0.4) {
          item.line.visible = false;
          item.arrow.visible = false;
          return;
        }

        item.line.geometry.setFromPoints([lineStart, lineEnd]);

        item.arrow.position.copy(arrowTip).addScaledVector(edgeDirection, -(arrowLength * 0.5));
        item.arrow.scale.set(arrowRadius, arrowLength, arrowRadius);
        item.arrow.quaternion.setFromUnitVectors(edgeArrowUp, edgeDirection);
      });
    }

    function applyVisualSettings() {
      const nodeScale = Number(nodeScaleInput.value || '1');
      const spreadScale = Number(spreadScaleInput.value || '1');

      nodeMeshes.forEach((mesh) => {
        const id = mesh.userData.nodeId;
        const basePos = basePositions.get(id);
        const visible = isNodeVisibleByActiveFilters(id, mesh.userData.nodeKind);
        mesh.visible = visible;

        if (basePos) {
          mesh.position.copy(basePos).multiplyScalar(spreadScale);
        }

        const hovered = hoveredNodeId === id;
        const selected = selectedNodeId === id;
        const emphasis = selected ? 1.6 : (hovered ? 1.28 : 1.0);
        const scale = emphasis * nodeScale;
        mesh.scale.setScalar(scale);

        if (mesh.material && mesh.material.emissive) {
          if (selected) {
            mesh.material.emissive.setHex(0x334155);
            mesh.material.emissiveIntensity = 0.85;
          } else if (hovered) {
            mesh.material.emissive.setHex(0x1d4ed8);
            mesh.material.emissiveIntensity = 0.45;
          } else {
            mesh.material.emissive.setHex(0x000000);
            mesh.material.emissiveIntensity = 0;
          }
        }

        const label = mesh.userData.labelSprite;
        if (label && label.userData.baseScale) {
          const bs = label.userData.baseScale;
          const labelScale = (selected ? 1.18 : (hovered ? 1.08 : 1.0)) * nodeScale;
          label.scale.set(bs.x * labelScale, bs.y * labelScale, 1);
          if (label.material) {
            label.material.opacity = selected ? 1.0 : (hovered ? 0.98 : 0.82);
          }
        }
      });

      refreshEdges();
      updateLabelLod();
      updateFilterStatus();
    }

    function shouldShowLabelForMesh(mesh) {
      if (!mesh || !mesh.visible) {
        return false;
      }

      const id = mesh.userData.nodeId;
      if (selectedNodeId === id || hoveredNodeId === id || filterRootNodeId === id) {
        return true;
      }

      const nodeKind = (mesh.userData.nodeKind || '').toLowerCase();
      if (nodeKind === 'method' || nodeKind === 'property' || nodeKind === 'field' || nodeKind === 'event') {
        const visibleCountForMembers = visibleNodeIds ? visibleNodeIds.size : nodes.length;
        return visibleCountForMembers <= 22;
      }

      if (nodeKind === 'project' || nodeKind === 'namespace' || nodeKind === 'file' || nodeKind === 'external') {
        const visibleCountForStructures = visibleNodeIds ? visibleNodeIds.size : nodes.length;
        return visibleCountForStructures <= 120;
      }

      const level = (mesh.userData.level || '').toLowerCase();
      if (level === 'critical' || level === 'hotspot') {
        return true;
      }

      const visibleCount = visibleNodeIds ? visibleNodeIds.size : nodes.length;
      if (visibleCount <= 24) {
        return true;
      }

      const distanceToCamera = mesh.position.distanceTo(camera.position);
      const cameraToTarget = camera.position.distanceTo(cameraControl.getTarget());
      const distanceThreshold = Math.max(45, cameraToTarget * 0.42);
      return distanceToCamera <= distanceThreshold;
    }

    function updateLabelLod() {
      nodeMeshes.forEach((mesh) => {
        const label = mesh.userData.labelSprite;
        if (!label) {
          return;
        }

        label.visible = shouldShowLabelForMesh(mesh);
      });
    }

    function setConnectedNodeFilter(nodeId) {
      const visible = new Set();
      visible.add(nodeId);

      const connected = adjacencyMap.get(nodeId);
      if (connected) {
        connected.forEach((value) => visible.add(value));
      }

      visibleNodeIds = visible;
      filterRootNodeId = nodeId;
      updateFilterButtonState();
      applyVisualSettings();
    }

    function clearNodeFilter(recordHistory = true) {
      visibleNodeIds = null;
      filterRootNodeId = null;
      updateFilterButtonState();
      applyVisualSettings();
      if (recordHistory) {
        pushHistoryState();
      }
    }

    function updateFilterButtonState() {
      clearFilterButton.disabled = visibleNodeIds === null;
    }

    function updateHistoryButtons() {
      historyBackButton.disabled = historyIndex <= 0;
      historyForwardButton.disabled = historyIndex < 0 || historyIndex >= historyEntries.length - 1;
    }

    function buildStateSignature(state) {
      const selected = state.selectedNodeId || '';
      const root = state.filterRootNodeId || '';
      const visible = state.visibleNodeIds ? state.visibleNodeIds.join('|') : '*';
      const kinds = state.activeNodeKinds ? state.activeNodeKinds.join('|') : '';
      const projects = state.activeProjectNodeIds ? state.activeProjectNodeIds.join('|') : '';
      const camera = state.cameraState
        ? `${state.cameraState.position.join(',')}|${state.cameraState.target.join(',')}`
        : '';
      return `${selected}#${root}#${visible}#${kinds}#${projects}#${camera}`;
    }

    function captureUiState() {
      const state = {
        selectedNodeId,
        filterRootNodeId,
        visibleNodeIds: visibleNodeIds ? Array.from(visibleNodeIds).sort() : null,
        activeNodeKinds: Array.from(activeNodeKinds).sort(),
        activeProjectNodeIds: Array.from(activeProjectNodeIds).sort(),
        cameraState: cameraControl.captureState()
      };

      state.signature = buildStateSignature(state);
      return state;
    }

    function applyUiState(state) {
      isApplyingHistory = true;
      try {
        selectedNodeId = state.selectedNodeId || null;
        hoveredNodeId = selectedNodeId;
        filterRootNodeId = state.filterRootNodeId || null;
        visibleNodeIds = state.visibleNodeIds ? new Set(state.visibleNodeIds) : null;
        activeNodeKinds.clear();
        const stateKinds = Array.isArray(state.activeNodeKinds) ? state.activeNodeKinds : kindOrder;
        stateKinds.forEach((kind) => activeNodeKinds.add(normalizeNodeKind(kind)));
        if (activeNodeKinds.size === 0) {
          activeNodeKinds.add('type');
        }

        activeProjectNodeIds.clear();
        const stateProjects = Array.isArray(state.activeProjectNodeIds)
          ? state.activeProjectNodeIds
          : projectNodeIds;
        stateProjects.forEach((projectId) => {
          if (projectNodeIds.includes(projectId)) {
            activeProjectNodeIds.add(projectId);
          }
        });

        syncProjectCheckboxes();
        syncKindCheckboxes();
        updateFilterButtonState();
        applyVisualSettings();
        cameraControl.applyState(state.cameraState);

        const selectedNode = selectedNodeId ? nodeMap.get(selectedNodeId) : null;
        updateNodeInfo(selectedNode ? selectedNode.userData.node : null);
      } finally {
        isApplyingHistory = false;
      }
    }

    function pushHistoryState() {
      if (isApplyingHistory) {
        return;
      }

      const state = captureUiState();
      if (historyIndex >= 0 && historyEntries[historyIndex] && historyEntries[historyIndex].signature === state.signature) {
        updateHistoryButtons();
        return;
      }

      if (historyIndex < historyEntries.length - 1) {
        historyEntries.splice(historyIndex + 1);
      }

      historyEntries.push(state);
      if (historyEntries.length > historyMax) {
        historyEntries.shift();
      }

      historyIndex = historyEntries.length - 1;
      updateHistoryButtons();
    }

    function navigateHistory(offset) {
      if (historyEntries.length === 0) {
        return;
      }

      const nextIndex = historyIndex + offset;
      if (nextIndex < 0 || nextIndex >= historyEntries.length) {
        return;
      }

      historyIndex = nextIndex;
      applyUiState(historyEntries[historyIndex]);
      updateHistoryButtons();
    }

    function updateFilterStatus() {
      const totalCount = nodes.length;
      const visibleCount = nodeMeshes.reduce((count, mesh) => count + (mesh.visible ? 1 : 0), 0);
      const rootText = filterRootNodeId ? ` | 起点: ${filterRootNodeId}` : '';
      const kindText = ` | 種別: ${activeNodeKinds.size}/${kindOrder.length}`;
      const projectText = projectNodeIds.length > 0
        ? ` | プロジェクト: ${activeProjectNodeIds.size}/${projectNodeIds.length}`
        : '';
      filterStatus.textContent = `表示中: ${visibleCount}/${totalCount}${rootText}${kindText}${projectText}`;
    }

    function getVisiblePoints() {
      const points = [];
      nodeMeshes.forEach((mesh) => {
        if (mesh.visible) {
          points.push(mesh.position.clone());
        }
      });

      return points;
    }

    function ensureSpreadScaleMax(targetSpreadScale) {
      const target = Number(targetSpreadScale || spreadScaleInput.value || '1');
      if (!Number.isFinite(target)) {
        return;
      }

      const currentMax = Number(spreadScaleInput.max || defaultSpreadScaleMax);
      if (target <= currentMax) {
        return;
      }

      const nextMax = Math.max(defaultSpreadScaleMax, Math.ceil((target + 0.4) * 10) / 10);
      spreadScaleInput.max = nextMax.toFixed(1);
    }

    function computeRecommendedSpreadScale() {
      const visibleNodeIdsForSpread = new Set();
      nodeMeshes.forEach((mesh) => {
        if (isNodeVisibleByActiveFilters(mesh.userData.nodeId, mesh.userData.nodeKind)) {
          visibleNodeIdsForSpread.add(mesh.userData.nodeId);
        }
      });

      const nodeCount = visibleNodeIdsForSpread.size;
      if (nodeCount <= 1) {
        return defaultSpreadScaleMin;
      }

      let edgeCount = 0;
      edgeLines.forEach((edge) => {
        if (visibleNodeIdsForSpread.has(edge.from) && visibleNodeIdsForSpread.has(edge.to)) {
          edgeCount += 1;
        }
      });

      const possibleEdges = Math.max(1, nodeCount * (nodeCount - 1));
      const density = Math.min(1, edgeCount / possibleEdges);
      const edgePerNode = edgeCount / Math.max(1, nodeCount);

      const nodePressure = Math.pow(Math.max(1, nodeCount) / 32, 0.65);
      const densityBoost = 1 + Math.min(4.2, density * 12);
      const edgeBoost = 1 + Math.min(3, edgePerNode / 18);

      const recommended = nodePressure * densityBoost * edgeBoost;
      return Math.max(defaultSpreadScaleMin, Math.min(45, recommended));
    }

    function applyAutoSpread() {
      const recommendedSpreadScale = computeRecommendedSpreadScale();
      ensureSpreadScaleMax(recommendedSpreadScale);
      const rounded = Math.round(recommendedSpreadScale * 10) / 10;
      spreadScaleInput.value = String(rounded);
      applyVisualSettings();
      fitVisibleNodes(false);
      pushHistoryState();
    }

    function fitVisibleNodes(recordHistory = true) {
      const points = getVisiblePoints();
      if (points.length === 0) {
        return;
      }

      cameraControl.fitToPoints(points, 1.35);
      if (recordHistory) {
        pushHistoryState();
      }
    }

    function findNodeIdByQuery(rawQuery) {
      const normalized = (rawQuery || '').trim().toLowerCase();
      if (normalized.length === 0) {
        return null;
      }

      const exact = nodes.find((node) => {
        const id = (node.id || '').toLowerCase();
        const label = (node.label || '').toLowerCase();
        return id === normalized || label === normalized;
      });
      if (exact) {
        return exact.id;
      }

      const partial = nodes.find((node) => {
        const id = (node.id || '').toLowerCase();
        const label = (node.label || '').toLowerCase();
        return id.includes(normalized) || label.includes(normalized);
      });
      return partial ? partial.id : null;
    }

    function focusNodeById(nodeId, filterRelated, openCode, recordHistory = true) {
      const target = nodeMap.get(nodeId);
      if (!target) {
        return false;
      }

      const targetKind = normalizeNodeKind(target.userData.nodeKind);
      if (!activeNodeKinds.has(targetKind)) {
        activeNodeKinds.add(targetKind);
        syncKindCheckboxes();
      }
      ensureProjectVisibilityForNode(nodeId);

      selectedNodeId = nodeId;
      if (filterRelated) {
        setConnectedNodeFilter(nodeId);
      } else {
        applyVisualSettings();
      }

      cameraControl.focus(target.position, 52);
      updateNodeInfo(target.userData.node);
      if (openCode) {
        postNodeSelected(target.userData.openNodeId || nodeId);
      }

      if (recordHistory) {
        pushHistoryState();
      }

      return true;
    }

    function setHoveredNode(nodeId) {
      if (hoveredNodeId === nodeId) {
        return;
      }

      hoveredNodeId = nodeId;
      applyVisualSettings();
    }

    function runSearch() {
      const nodeId = findNodeIdByQuery(searchInput.value);
      if (!nodeId) {
        return;
      }

      focusNodeById(nodeId, true, false);
    }

    function pickNodeFromEvent(event) {
      updatePointerFromEvent(event);
      raycaster.setFromCamera(pointer, camera);

      const hits = raycaster.intersectObjects(nodeMeshes, false);
      if (hits.length === 0) {
        return null;
      }

      return hits[0].object;
    }

    window.depSphereFocusNode = function(nodeId) {
      if (visibleNodeIds && !visibleNodeIds.has(nodeId)) {
        clearNodeFilter(false);
      }

      focusNodeById(nodeId, false, false, false);
    };

    canvas.addEventListener('click', (event) => {
      if (pointerMoved) {
        return;
      }

      const selected = pickNodeFromEvent(event);
      if (!selected) {
        return;
      }

      const selectedId = selected.userData.nodeId;
      if (singleClickTimer) {
        clearTimeout(singleClickTimer);
      }

      singleClickTimer = setTimeout(() => {
        singleClickTimer = null;
        selectedNodeId = selectedId;
        hoveredNodeId = selectedId;
        setConnectedNodeFilter(selectedId);
        updateNodeInfo(selected.userData.node);
        pushHistoryState();
      }, 220);
    });

    canvas.addEventListener('dblclick', (event) => {
      if (pointerMoved) {
        return;
      }

      if (singleClickTimer) {
        clearTimeout(singleClickTimer);
        singleClickTimer = null;
      }

      const selected = pickNodeFromEvent(event);
      if (!selected) {
        return;
      }

      const selectedId = selected.userData.nodeId;
      hoveredNodeId = selectedId;
      focusNodeById(selectedId, false, true);
    });

    nodeScaleInput.addEventListener('input', applyVisualSettings);
    spreadScaleInput.addEventListener('input', applyVisualSettings);
    overlayToggleButton.addEventListener('click', toggleOverlayPanelExpanded);
    nodeInfoToggleButton.addEventListener('click', toggleNodeInfoPanelExpanded);
    historyBackButton.addEventListener('click', () => navigateHistory(-1));
    historyForwardButton.addEventListener('click', () => navigateHistory(1));
    fitViewButton.addEventListener('click', fitVisibleNodes);
    autoSpreadButton.addEventListener('click', applyAutoSpread);
    clearFilterButton.addEventListener('click', clearNodeFilter);
    searchButton.addEventListener('click', runSearch);
    searchInput.addEventListener('keydown', (event) => {
      if (event.key === 'Enter') {
        event.preventDefault();
        runSearch();
      }
    });
    window.addEventListener('keydown', (event) => {
      const key = (event.key || '').toLowerCase();
      if (event.altKey && key === 'arrowleft') {
        event.preventDefault();
        navigateHistory(-1);
        return;
      }

      if (event.altKey && key === 'arrowright') {
        event.preventDefault();
        navigateHistory(1);
        return;
      }

      if ((event.ctrlKey || event.metaKey) && key === 'f') {
        event.preventDefault();
        searchInput.focus();
        searchInput.select();
        return;
      }

      if (event.key === 'Escape') {
        clearNodeFilter();
        return;
      }

      if (!event.ctrlKey && !event.metaKey && !event.altKey && key === 'f') {
        if (selectedNodeId) {
          focusNodeById(selectedNodeId, false, false);
        } else {
          fitVisibleNodes();
        }
      }
    });

    window.addEventListener('resize', () => {
      camera.aspect = window.innerWidth / window.innerHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(window.innerWidth, window.innerHeight);
    });

    initializeKindFilter();
    initializeProjectFilter();
    applyOverlayPanelExpanded();
    applyNodeInfoPanelExpanded();
    updateFilterButtonState();
    updateHistoryButtons();
    applyVisualSettings();
    fitVisibleNodes(false);
    pushHistoryState();

    function createCameraController(camera, surface) {
      const target = new THREE.Vector3(0, 0, 0);
      const spherical = new THREE.Spherical();
      const offset = new THREE.Vector3();
      const right = new THREE.Vector3();
      const up = new THREE.Vector3();
      const panOffset = new THREE.Vector3();

      let dragMode = null;
      let dragPointerId = null;
      let lastX = 0;
      let lastY = 0;

      const rotateSpeed = 0.0055;
      const panSpeed = 0.0018;
      const zoomStep = 1.12;
      const minDistance = 12;
      const maxDistance = 1800;

      function clampDistance(value) {
        return Math.min(maxDistance, Math.max(minDistance, value));
      }

      function syncFromCamera() {
        offset.copy(camera.position).sub(target);
        if (offset.lengthSq() < 1e-8) {
          offset.set(0, 0, minDistance);
        }

        spherical.setFromVector3(offset);
        spherical.radius = clampDistance(spherical.radius);
        spherical.phi = Math.max(0.05, Math.min(Math.PI - 0.05, spherical.phi));
      }

      function applyCamera() {
        offset.setFromSpherical(spherical);
        camera.position.copy(target).add(offset);
        camera.lookAt(target);
      }

      function handlePointerDown(event) {
        if (event.button === 2) {
          dragMode = 'rotate';
        } else if (event.button === 0) {
          dragMode = 'pan';
        } else {
          return;
        }

        dragPointerId = event.pointerId;
        lastX = event.clientX;
        lastY = event.clientY;
        if (surface.setPointerCapture) {
          surface.setPointerCapture(event.pointerId);
        }
      }

      function handlePointerMove(event) {
        if (!dragMode || (dragPointerId !== null && event.pointerId !== dragPointerId)) {
          return;
        }

        const dx = event.clientX - lastX;
        const dy = event.clientY - lastY;
        lastX = event.clientX;
        lastY = event.clientY;

        if (dragMode === 'rotate') {
          spherical.theta -= dx * rotateSpeed;
          spherical.phi -= dy * rotateSpeed;
          spherical.phi = Math.max(0.05, Math.min(Math.PI - 0.05, spherical.phi));
          applyCamera();
          return;
        }

        const distance = Math.max(minDistance, spherical.radius);
        const scale = distance * panSpeed;

        camera.updateMatrix();
        right.setFromMatrixColumn(camera.matrix, 0);
        up.setFromMatrixColumn(camera.matrix, 1);

        panOffset.copy(right).multiplyScalar(-dx * scale);
        panOffset.add(up.multiplyScalar(dy * scale));

        target.add(panOffset);
        camera.position.add(panOffset);
        syncFromCamera();
        applyCamera();
      }

      function handlePointerUp(event) {
        if (dragPointerId !== null && event.pointerId !== dragPointerId) {
          return;
        }

        dragMode = null;
        dragPointerId = null;
        if (surface.releasePointerCapture && event.pointerId !== undefined) {
          try {
            surface.releasePointerCapture(event.pointerId);
          } catch {
            // no-op
          }
        }
      }

      function handleWheel(event) {
        event.preventDefault();
        const factor = event.deltaY < 0 ? (1 / zoomStep) : zoomStep;
        spherical.radius = clampDistance(spherical.radius * factor);
        applyCamera();
      }

      function setTarget(position) {
        target.copy(position);
        syncFromCamera();
        applyCamera();
      }

      function focus(position, distance) {
        target.copy(position);

        if (offset.lengthSq() < 1e-8) {
          offset.set(0, 0, 1);
        }

        const desiredDistance = clampDistance(distance || spherical.radius || 90);
        offset.normalize().multiplyScalar(desiredDistance);
        spherical.setFromVector3(offset);
        spherical.phi = Math.max(0.05, Math.min(Math.PI - 0.05, spherical.phi));
        applyCamera();
      }

      function fitToPoints(points, paddingRatio) {
        if (!points || points.length === 0) {
          return;
        }

        const box = new THREE.Box3();
        points.forEach((point) => box.expandByPoint(point));

        const center = box.getCenter(new THREE.Vector3());
        const size = box.getSize(new THREE.Vector3());
        const radius = Math.max(1, Math.max(size.x, size.y, size.z) * 0.5);
        const fovRad = THREE.MathUtils.degToRad(camera.fov);
        const padding = paddingRatio || 1.25;
        const requiredDistance = clampDistance((radius / Math.tan(fovRad * 0.5)) * padding);

        target.copy(center);
        if (offset.lengthSq() < 1e-8) {
          offset.set(0, 0, 1);
        }

        offset.normalize().multiplyScalar(requiredDistance);
        spherical.setFromVector3(offset);
        spherical.phi = Math.max(0.05, Math.min(Math.PI - 0.05, spherical.phi));
        applyCamera();
      }

      function round4(value) {
        return Math.round(value * 10000) / 10000;
      }

      function captureState() {
        return {
          position: [round4(camera.position.x), round4(camera.position.y), round4(camera.position.z)],
          target: [round4(target.x), round4(target.y), round4(target.z)]
        };
      }

      function applyState(state) {
        if (!state || !state.position || !state.target || state.position.length !== 3 || state.target.length !== 3) {
          return;
        }

        camera.position.set(state.position[0], state.position[1], state.position[2]);
        target.set(state.target[0], state.target[1], state.target[2]);
        syncFromCamera();
        applyCamera();
      }

      syncFromCamera();
      applyCamera();

      surface.addEventListener('pointerdown', handlePointerDown);
      surface.addEventListener('pointermove', handlePointerMove);
      surface.addEventListener('pointerup', handlePointerUp);
      surface.addEventListener('pointercancel', handlePointerUp);
      surface.addEventListener('wheel', handleWheel, { passive: false });

      return {
        setTarget,
        getTarget: function() { return target.clone(); },
        syncFromCamera,
        focus,
        fitToPoints,
        captureState,
        applyState,
        update: function() { }
      };
    }

    function animate() {
      requestAnimationFrame(animate);
      cameraControl.update();
      updateLabelLod();
      renderer.render(scene, camera);
    }

    animate();
  </script>
</body>
</html>
""";
    }
}
