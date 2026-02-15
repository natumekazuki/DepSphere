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
      background: rgba(15, 23, 42, 0.7); padding: 8px 12px; border-radius: 8px;
      font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 12px;
    }
  </style>
</head>
<body>
  <canvas id="dep-graph-canvas"></canvas>
  <div id="overlay">DepSphere 3D Graph</div>
  <script src="https://unpkg.com/three@0.160.0/build/three.min.js"></script>
  <script>
    window.__depSphereGraph = JSON.parse(atob("{{base64}}"));

    const canvas = document.getElementById('dep-graph-canvas');
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x0b1220);

    const camera = new THREE.PerspectiveCamera(60, window.innerWidth / window.innerHeight, 0.1, 2000);
    camera.position.set(0, 0, 160);

    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    renderer.setPixelRatio(window.devicePixelRatio || 1);
    renderer.setSize(window.innerWidth, window.innerHeight);

    const ambient = new THREE.AmbientLight(0xffffff, 0.75);
    scene.add(ambient);

    const nodes = window.__depSphereGraph.nodes || [];
    const edges = window.__depSphereGraph.edges || [];

    const nodeMeshes = [];
    const nodeMap = new Map();

    function hexColor(color) {
      if (!color || !color.startsWith('#')) return 0x3b82f6;
      return parseInt(color.slice(1), 16);
    }

    nodes.forEach((node) => {
      const geometry = new THREE.SphereGeometry(Math.max(2, node.size * 0.15), 16, 16);
      const material = new THREE.MeshStandardMaterial({ color: hexColor(node.color) });
      const mesh = new THREE.Mesh(geometry, material);
      mesh.position.set(node.x || 0, node.y || 0, node.z || 0);
      mesh.userData.nodeId = node.id;
      scene.add(mesh);
      nodeMeshes.push(mesh);
      nodeMap.set(node.id, mesh);
    });

    const defaultScales = new Map();
    nodeMeshes.forEach((mesh) => {
      defaultScales.set(mesh.userData.nodeId, mesh.scale.clone());
    });

    edges.forEach((edge) => {
      const from = nodeMap.get(edge.from);
      const to = nodeMap.get(edge.to);
      if (!from || !to) return;

      const points = [from.position.clone(), to.position.clone()];
      const geometry = new THREE.BufferGeometry().setFromPoints(points);
      const material = new THREE.LineBasicMaterial({ color: hexColor(edge.color), opacity: 0.7, transparent: true });
      const line = new THREE.Line(geometry, material);
      scene.add(line);
    });

    const raycaster = new THREE.Raycaster();
    const pointer = new THREE.Vector2();

    function postNodeSelected(nodeId) {
      const payload = { type: 'nodeSelected', nodeId: nodeId };
      if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
        window.chrome.webview.postMessage(payload);
      }
      if (window.__depSphereHost && typeof window.__depSphereHost.onNodeSelected === 'function') {
        window.__depSphereHost.onNodeSelected(nodeId);
      }
    }

    window.depSphereFocusNode = function(nodeId) {
      const target = nodeMap.get(nodeId);
      if (!target) return;

      nodeMeshes.forEach((mesh) => {
        const baseScale = defaultScales.get(mesh.userData.nodeId);
        if (baseScale) {
          mesh.scale.copy(baseScale);
        }
      });

      target.scale.setScalar(1.6);
      const desired = target.position.clone().add(new THREE.Vector3(0, 0, 40));
      camera.position.lerp(desired, 0.45);
      camera.lookAt(target.position);
    };

    canvas.addEventListener('click', (event) => {
      const rect = canvas.getBoundingClientRect();
      pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
      pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
      raycaster.setFromCamera(pointer, camera);

      const hits = raycaster.intersectObjects(nodeMeshes, false);
      if (hits.length > 0) {
        const selectedId = hits[0].object.userData.nodeId;
        postNodeSelected(selectedId);
      }
    });

    window.addEventListener('resize', () => {
      camera.aspect = window.innerWidth / window.innerHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(window.innerWidth, window.innerHeight);
    });

    function animate() {
      requestAnimationFrame(animate);
      scene.rotation.y += 0.0015;
      renderer.render(scene, camera);
    }

    animate();
  </script>
</body>
</html>
""";
    }
}
