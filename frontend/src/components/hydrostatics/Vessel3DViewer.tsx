import { useMemo, useRef, useEffect } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls, Grid, PerspectiveCamera } from "@react-three/drei";
import type { OrbitControls as OrbitControlsImpl } from "three-stdlib";
import * as THREE from "three";
import { observer } from "mobx-react-lite";
import type { HydroResult } from "../../types/hydrostatics";

interface Vessel3DViewerProps {
  lpp: number; // Length between perpendiculars
  beam: number; // Maximum breadth
  designDraft: number; // Design draft
  draft?: number; // Current draft (for waterplane)
  kb?: number; // Center of buoyancy vertical position
  lcb?: number; // Center of buoyancy longitudinal position
  kg?: number; // Center of gravity vertical position
  lcg?: number; // Center of gravity longitudinal position
  currentResult?: HydroResult | null;
}

/**
 * Generate parametric hull surface using modified Wigley hull equation
 * y(x,z) = (B/2) * (1 - z²/D²) * (1 - (2x/Lpp - 1)²)
 */
function generateHullGeometry(
  lpp: number,
  beam: number,
  designDraft: number
): THREE.BufferGeometry {
  const geometry = new THREE.BufferGeometry();
  const vertices: number[] = [];
  const indices: number[] = [];

  // Grid resolution
  const xSegments = 50; // Longitudinal segments
  const zSegments = 30; // Vertical segments

  const halfBeam = beam / 2;

  // Generate vertices
  for (let zi = 0; zi <= zSegments; zi++) {
    const z = (zi / zSegments) * designDraft;
    const zNorm = z / designDraft; // Normalized z [0, 1]

    for (let xi = 0; xi <= xSegments; xi++) {
      const x = (xi / xSegments) * lpp;
      const xNorm = (2 * x) / lpp - 1; // Normalized x [-1, 1]

      // Modified Wigley hull equation
      const y = halfBeam * (1 - zNorm * zNorm) * (1 - xNorm * xNorm);

      // Port side (negative y)
      vertices.push(x, -y, z);
    }
  }

  // Generate indices for triangles (port side)
  for (let zi = 0; zi < zSegments; zi++) {
    for (let xi = 0; xi < xSegments; xi++) {
      const a = zi * (xSegments + 1) + xi;
      const b = a + 1;
      const c = a + (xSegments + 1);
      const d = c + 1;

      // Two triangles per quad
      indices.push(a, c, b);
      indices.push(b, c, d);
    }
  }

  // Mirror to starboard side
  const portVertexCount = vertices.length / 3;
  const portStartIndex = portVertexCount;

  for (let zi = 0; zi <= zSegments; zi++) {
    for (let xi = 0; xi <= xSegments; xi++) {
      const idx = zi * (xSegments + 1) + xi;
      const baseIdx = idx * 3;
      const x = vertices[baseIdx];
      const y = vertices[baseIdx + 1];
      const z = vertices[baseIdx + 2];

      // Starboard side (positive y, mirrored)
      vertices.push(x, -y, z);
    }
  }

  // Generate indices for starboard side
  for (let zi = 0; zi < zSegments; zi++) {
    for (let xi = 0; xi < xSegments; xi++) {
      const a = portStartIndex + zi * (xSegments + 1) + xi;
      const b = a + 1;
      const c = a + (xSegments + 1);
      const d = c + 1;

      indices.push(a, b, c);
      indices.push(b, d, c);
    }
  }

  // Add keel/base surface
  const keelStartIdx = vertices.length / 3;
  for (let xi = 0; xi <= xSegments; xi++) {
    const x = (xi / xSegments) * lpp;
    vertices.push(x, 0, 0); // Centerline at keel
  }

  // Connect keel to port and starboard
  for (let xi = 0; xi < xSegments; xi++) {
    const keelA = keelStartIdx + xi;
    const keelB = keelStartIdx + xi + 1;
    const portA = xi;
    const portB = xi + 1;
    const starA = portStartIndex + xi;
    const starB = portStartIndex + xi + 1;

    indices.push(keelA, portA, keelB);
    indices.push(keelB, portA, portB);
    indices.push(keelA, keelB, starA);
    indices.push(keelB, starB, starA);
  }

  geometry.setIndex(indices);
  geometry.setAttribute("position", new THREE.Float32BufferAttribute(vertices, 3));
  geometry.computeVertexNormals();
  geometry.computeBoundingBox();

  return geometry;
}

function HullMesh({ lpp, beam, designDraft }: { lpp: number; beam: number; designDraft: number }) {
  const geometry = useMemo(
    () => generateHullGeometry(lpp, beam, designDraft),
    [lpp, beam, designDraft]
  );

  return (
    <mesh geometry={geometry} castShadow receiveShadow>
      <meshStandardMaterial
        color="#6b7280"
        metalness={0.1}
        roughness={0.7}
        side={THREE.DoubleSide}
      />
    </mesh>
  );
}

function Waterplane({ lpp, beam, draft }: { lpp: number; beam: number; draft: number }) {
  if (!draft || draft <= 0) return null;

  return (
    <mesh position={[lpp / 2, 0, draft]} rotation={[-Math.PI / 2, 0, 0]}>
      <planeGeometry args={[lpp * 1.2, beam * 1.2]} />
      <meshStandardMaterial color="#3b82f6" transparent opacity={0.3} side={THREE.DoubleSide} />
    </mesh>
  );
}

function BuoyancyMarker({ lcb, kb }: { lcb: number; kb: number }) {
  if (lcb === undefined || kb === undefined || lcb <= 0 || kb <= 0) return null;

  const markerSize = Math.max(lcb, kb) * 0.02;
  return (
    <group position={[lcb, 0, kb]}>
      <mesh>
        <sphereGeometry args={[markerSize, 16, 16]} />
        <meshStandardMaterial color="#10b981" />
      </mesh>
    </group>
  );
}

function GravityMarker({ lcg, kg }: { lcg: number; kg: number }) {
  if (kg === undefined || lcg === undefined || kg <= 0 || lcg <= 0) return null;

  const markerSize = Math.max(lcg, kg) * 0.02;
  return (
    <group position={[lcg, 0, kg]}>
      <mesh>
        <sphereGeometry args={[markerSize, 16, 16]} />
        <meshStandardMaterial color="#ef4444" />
      </mesh>
    </group>
  );
}

function SceneContent({ lpp, beam, designDraft, draft, kb, lcb, kg, lcg }: Vessel3DViewerProps) {
  const controlsRef = useRef<OrbitControlsImpl>(null);

  // Auto-fit camera when parameters change
  useEffect(() => {
    if (controlsRef.current) {
      controlsRef.current.reset();
    }
  }, [lpp, beam, designDraft]);

  const bounds = useMemo(() => {
    const maxDim = Math.max(lpp, beam, designDraft);
    return maxDim * 1.5;
  }, [lpp, beam, designDraft]);

  return (
    <>
      <PerspectiveCamera makeDefault position={[bounds, bounds * 0.8, bounds]} fov={50} />
      <OrbitControls
        ref={controlsRef}
        enableDamping
        dampingFactor={0.05}
        minDistance={bounds * 0.5}
        maxDistance={bounds * 3}
      />

      {/* Lighting */}
      <ambientLight intensity={0.6} />
      <directionalLight position={[10, 10, 5]} intensity={0.8} castShadow />

      {/* Grid helper */}
      <Grid
        args={[bounds * 2, 20]}
        cellColor="#6b7280"
        sectionColor="#4b5563"
        fadeDistance={bounds}
      />

      {/* Axes helper */}
      <axesHelper args={[bounds * 0.3]} />

      {/* Hull */}
      <HullMesh lpp={lpp} beam={beam} designDraft={designDraft} />

      {/* Waterplane */}
      <Waterplane lpp={lpp} beam={beam} draft={draft || designDraft} />

      {/* Center of Buoyancy */}
      {kb !== undefined && lcb !== undefined && <BuoyancyMarker lcb={lcb} kb={kb} />}

      {/* Center of Gravity */}
      <GravityMarker lcg={lcg || 0} kg={kg || 0} />
    </>
  );
}

export const Vessel3DViewer = observer(function Vessel3DViewer({
  lpp,
  beam,
  designDraft,
  draft,
  kb,
  lcb,
  kg,
  lcg,
  currentResult,
}: Vessel3DViewerProps) {
  const displayDraft = draft ?? currentResult?.draft ?? designDraft;
  const displayKb = kb ?? currentResult?.kBz;
  const displayLcb = lcb ?? currentResult?.lCBx;

  return (
    <div className="w-full h-full bg-gray-900 dark:bg-gray-950">
      <Canvas shadows gl={{ antialias: true }}>
        <SceneContent
          lpp={lpp}
          beam={beam}
          designDraft={designDraft}
          draft={displayDraft}
          kb={displayKb}
          lcb={displayLcb}
          kg={kg}
          lcg={lcg}
          currentResult={currentResult}
        />
      </Canvas>
    </div>
  );
});
