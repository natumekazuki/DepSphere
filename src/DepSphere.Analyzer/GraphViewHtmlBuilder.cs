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
    }
    #overlay h1 {
      margin: 0 0 8px 0; font-size: 13px; font-weight: 600;
    }
    #overlay .control { margin-top: 8px; }
    #overlay label { display: block; margin-bottom: 2px; color: #cbd5e1; }
    #overlay input[type='range'] { width: 100%; }
    #node-info {
      position: fixed; right: 12px; top: 12px; color: #e2e8f0;
      background: rgba(2, 6, 23, 0.82); padding: 10px 12px; border-radius: 10px;
      border: 1px solid rgba(148, 163, 184, 0.25);
      font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 12px;
      max-width: 340px;
      white-space: pre-wrap;
    }
    #node-info .title { font-weight: 600; margin-bottom: 6px; }
  </style>
</head>
<body>
  <canvas id="dep-graph-canvas"></canvas>
  <div id="overlay">
    <h1>DepSphere 3D Graph</h1>
    <div>左クリック: ノード選択</div>
    <div>右ドラッグ: 回転 / 左ドラッグ: 平行移動 / ホイール: ズーム</div>
    <div class="control">
      <label for="node-scale">ノード倍率</label>
      <input id="node-scale" type="range" min="0.6" max="2.4" step="0.1" value="1.0" />
    </div>
    <div class="control">
      <label for="spread-scale">距離倍率</label>
      <input id="spread-scale" type="range" min="0.6" max="2.6" step="0.1" value="1.0" />
    </div>
  </div>
  <div id="node-info">
    <div class="title">ノード情報</div>
    <div id="node-info-body">ノードをクリックするとクラス名とメソッド名を表示します。</div>
  </div>
  <script src="https://unpkg.com/three@0.160.0/build/three.min.js"></script>
  <script>
    window.__depSphereGraph = JSON.parse(atob("{{base64}}"));

    const canvas = document.getElementById('dep-graph-canvas');
    const infoBody = document.getElementById('node-info-body');
    const nodeScaleInput = document.getElementById('node-scale');
    const spreadScaleInput = document.getElementById('spread-scale');

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
    const edgeLines = [];
    let selectedNodeId = null;

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
        const fallbackMaterial = new THREE.SpriteMaterial({ color: 0xe2e8f0 });
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

      const points = [from.position.clone(), to.position.clone()];
      const geometry = new THREE.BufferGeometry().setFromPoints(points);
      const material = new THREE.LineBasicMaterial({ color: hexColor(edge.color), opacity: 0.68, transparent: true });
      const line = new THREE.Line(geometry, material);
      scene.add(line);
      edgeLines.push({ from: edge.from, to: edge.to, line });
    });

    const raycaster = new THREE.Raycaster();
    const pointer = new THREE.Vector2();

    let pointerDown = null;
    let pointerMoved = false;

    canvas.addEventListener('pointerdown', (event) => {
      pointerDown = { x: event.clientX, y: event.clientY, button: event.button };
      pointerMoved = false;
    });

    canvas.addEventListener('pointermove', (event) => {
      if (!pointerDown) return;
      const dx = event.clientX - pointerDown.x;
      const dy = event.clientY - pointerDown.y;
      if ((dx * dx + dy * dy) > 25) {
        pointerMoved = true;
      }
    });

    canvas.addEventListener('pointerup', () => {
      pointerDown = null;
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
        infoBody.textContent = 'ノードをクリックするとクラス名とメソッド名を表示します。';
        return;
      }

      const methods = Array.isArray(node.methodNames) ? node.methodNames : [];
      const methodText = methods.length > 0
        ? methods.join('\n')
        : '(メソッド情報なし)';

      infoBody.textContent =
        `Class: ${node.id}\n` +
        `Label: ${node.label || node.id}\n` +
        `Methods:\n${methodText}`;
    }

    function refreshEdges() {
      edgeLines.forEach((item) => {
        const from = nodeMap.get(item.from);
        const to = nodeMap.get(item.to);
        if (!from || !to) return;

        item.line.geometry.setFromPoints([from.position.clone(), to.position.clone()]);
      });
    }

    function applyVisualSettings() {
      const nodeScale = Number(nodeScaleInput.value || '1');
      const spreadScale = Number(spreadScaleInput.value || '1');

      nodeMeshes.forEach((mesh) => {
        const id = mesh.userData.nodeId;
        const basePos = basePositions.get(id);
        if (basePos) {
          mesh.position.copy(basePos).multiplyScalar(spreadScale);
        }

        const selected = selectedNodeId === id;
        const scale = (selected ? 1.6 : 1.0) * nodeScale;
        mesh.scale.setScalar(scale);

        const label = mesh.userData.labelSprite;
        if (label && label.userData.baseScale) {
          const bs = label.userData.baseScale;
          label.scale.set(bs.x * nodeScale, bs.y * nodeScale, 1);
        }
      });

      refreshEdges();
    }

    window.depSphereFocusNode = function(nodeId) {
      const target = nodeMap.get(nodeId);
      if (!target) return;

      selectedNodeId = nodeId;
      applyVisualSettings();

      cameraControl.focus(target.position, 52);
      updateNodeInfo(target.userData.node);
    };

    canvas.addEventListener('click', (event) => {
      if (pointerMoved) {
        return;
      }

      const rect = canvas.getBoundingClientRect();
      pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
      pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
      raycaster.setFromCamera(pointer, camera);

      const hits = raycaster.intersectObjects(nodeMeshes, false);
      if (hits.length === 0) {
        return;
      }

      const selected = hits[0].object;
      const selectedId = selected.userData.nodeId;
      selectedNodeId = selectedId;
      applyVisualSettings();
      updateNodeInfo(selected.userData.node);
      postNodeSelected(selectedId);
    });

    nodeScaleInput.addEventListener('input', applyVisualSettings);
    spreadScaleInput.addEventListener('input', applyVisualSettings);

    window.addEventListener('resize', () => {
      camera.aspect = window.innerWidth / window.innerHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(window.innerWidth, window.innerHeight);
    });

    applyVisualSettings();

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

      syncFromCamera();
      applyCamera();

      surface.addEventListener('pointerdown', handlePointerDown);
      surface.addEventListener('pointermove', handlePointerMove);
      surface.addEventListener('pointerup', handlePointerUp);
      surface.addEventListener('pointercancel', handlePointerUp);
      surface.addEventListener('wheel', handleWheel, { passive: false });

      return {
        setTarget,
        syncFromCamera,
        focus,
        update: function() { }
      };
    }

    function animate() {
      requestAnimationFrame(animate);
      cameraControl.update();
      renderer.render(scene, camera);
    }

    animate();
  </script>
</body>
</html>
""";
    }
}
