import { Html } from "@react-three/drei";

interface InteractiveCoGMarkerProps {
  position: [number, number, number];
  onDragEnd?: (newPosition: [number, number, number]) => void;
  color?: string;
  label?: string;
  size?: number;
}

/**
 * Marker for CoG/CoB visualization.
 * Note: Interactive dragging will be added in Phase 5 with proper transform controls.
 */
export function InteractiveCoGMarker({
  position,
  color = "#ef4444",
  label = "CoG",
  size = 0.5,
}: InteractiveCoGMarkerProps) {
  return (
    <group>
      <mesh position={position}>
        <sphereGeometry args={[size, 16, 16]} />
        <meshStandardMaterial
          color={color}
          emissive={color}
          emissiveIntensity={0.3}
          roughness={0.3}
          metalness={0.7}
        />
      </mesh>

      {/* Label */}
      <Html position={[position[0], position[1], position[2] + 1]}>
        <div className="bg-black/80 text-white px-2 py-1 rounded text-xs font-semibold pointer-events-none">
          {label}
        </div>
      </Html>
    </group>
  );
}
