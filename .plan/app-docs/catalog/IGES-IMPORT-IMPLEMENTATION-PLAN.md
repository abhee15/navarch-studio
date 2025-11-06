# IGES Import Implementation Plan

**Date:** November 6, 2025  
**Status:** 📋 PLANNED - Libraries Identified  
**Priority:** MEDIUM (Geometry visualization & CAD integration)  
**Estimated Effort:** 1-2 weeks

---

## 🎯 **OBJECTIVE**

Import hull geometry from IGES (Initial Graphics Exchange Specification) files to enable:
- 3D visualization of benchmark hulls
- Geometry validation against published data
- CAD-level hull geometry storage
- Export capabilities for external tools

---

## 📚 **LIBRARY OPTIONS**

### **Option 1: three-iges-loader** (Recommended for Frontend)

**Pros:**
- ✅ Pure JavaScript (no native dependencies)
- ✅ Integrates with Three.js (already in ecosystem)
- ✅ Browser-compatible
- ✅ Good for visualization
- ✅ Actively maintained

**Cons:**
- ⚠️ Limited to visual representation
- ⚠️ May not extract precise geometry data
- ⚠️ Focused on rendering, not calculation

**Use Case:** Frontend 3D hull viewer

**Installation:**
```bash
npm install three-iges-loader
```

**Example:**
```typescript
import { IGESLoader } from 'three-iges-loader';
import * as THREE from 'three';

const loader = new IGESLoader();
loader.load('path/to/hull.iges', (geometry) => {
  const material = new THREE.MeshStandardMaterial({ color: 0x4488ff });
  const mesh = new THREE.Mesh(geometry, material);
  scene.add(mesh);
});
```

---

### **Option 2: OpenCascade.js** (Recommended for Backend)

**Pros:**
- ✅ Industry-standard CAD kernel (OCCT)
- ✅ Full geometry extraction
- ✅ Precise mathematical operations
- ✅ Can compute hydrostatic properties
- ✅ Supports IGES, STEP, STL, BREP
- ✅ Node.js compatible (via WASM)

**Cons:**
- ⚠️ Large bundle size (~50MB WASM)
- ⚠️ Complex API
- ⚠️ Requires WASM support
- ⚠️ Steeper learning curve

**Use Case:** Backend geometry processing, precise calculations

**Installation:**
```bash
npm install opencascade.js
```

**Example:**
```javascript
const { initOpenCascade } = require('opencascade.js');

const oc = await initOpenCascade();

// Read IGES file
const igesReader = new oc.IGESControl_Reader_1();
const status = igesReader.ReadFile('hull.iges');

if (status === oc.IFSelect_ReturnStatus.IFSelect_RetDone) {
  igesReader.TransferRoots();
  const shape = igesReader.OneShape();
  
  // Extract properties
  const props = new oc.GProp_GProps_1();
  oc.BRepGProp.VolumeProperties(shape, props);
  const volume = props.Mass();
  
  // Extract mesh for rendering
  const mesh = oc.BRepMesh.Mesh(shape, 0.1);
}
```

---

### **Option 3: IxMilia.Iges (.NET C#)** (Alternative for Backend)

**Pros:**
- ✅ Native .NET (no WASM overhead)
- ✅ Good C# integration
- ✅ Actively maintained
- ✅ Clean API

**Cons:**
- ⚠️ Read-only (no write support)
- ⚠️ Limited geometry operations (extraction only)
- ⚠️ Requires post-processing for calculations

**Use Case:** Backend IGES parsing if staying pure .NET

**Installation:**
```bash
dotnet add package IxMilia.Iges
```

**Example:**
```csharp
using IxMilia.Iges;

var file = IgesFile.Load("hull.iges");

foreach (var entity in file.Entities)
{
    switch (entity)
    {
        case IgesRationalBSplineSurface surface:
            // Extract NURBS surface data
            var controlPoints = surface.ControlPoints;
            var knots = surface.Knots;
            break;
            
        case IgesCompositeCurve curve:
            // Extract curve segments
            break;
    }
}
```

---

## 🏗️ **RECOMMENDED ARCHITECTURE**

### **Hybrid Approach:**

```
┌─────────────────────────────────────────────────┐
│                  Frontend (React)                │
│  ┌──────────────────────────────────────────┐  │
│  │  three-iges-loader + Three.js            │  │
│  │  • 3D visualization                       │  │
│  │  • User interaction (rotate, zoom)        │  │
│  │  • Fast rendering                         │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
                      ↓ API
┌─────────────────────────────────────────────────┐
│              Backend (.NET + Node)               │
│  ┌──────────────────────────────────────────┐  │
│  │  OpenCascade.js (Node.js service)        │  │
│  │  • IGES parsing                           │  │
│  │  • Geometry extraction                    │  │
│  │  • Mesh generation                        │  │
│  │  • Property calculation (volume, areas)   │  │
│  └──────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────┐  │
│  │  DataService (.NET)                       │  │
│  │  • Store processed geometry (JSON/JSONB) │  │
│  │  • Cache mesh data                        │  │
│  │  • Serve to frontend                      │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│              Storage (PostgreSQL + S3)           │
│  • IGES files: S3                                │
│  • Processed mesh: PostgreSQL JSONB             │
│  • Metadata: PostgreSQL                          │
└─────────────────────────────────────────────────┘
```

**Why Hybrid?**
- Frontend needs fast rendering → three-iges-loader
- Backend needs precise geometry → OpenCascade.js
- Best of both worlds: visualization + calculation

---

## 📋 **IMPLEMENTATION PHASES**

### **Phase 1: Research & Setup (2-3 hours)**

**Tasks:**
1. ✅ Identify entity types from `iges_entity_types.txt` ✅ (DONE)
2. 📋 Set up Node.js service for OpenCascade.js
3. 📋 Test IGES parsing with sample file (KVLCC2, KCS)
4. 📋 Benchmark performance (file size vs load time)
5. 📋 Evaluate memory requirements

**Deliverable:** Working proof-of-concept with one IGES file

---

### **Phase 2: Backend Processing Service (1-2 days)**

**Create:** `backend/GeometryService/` (New microservice or Node.js script)

**Features:**
1. **IGES Upload Endpoint**
   ```typescript
   POST /api/geometry/import/iges
   
   Request:
   - file: IGES file (multipart/form-data)
   - hullName: string
   
   Response:
   - geometryId: uuid
   - volume: number
   - surfaceArea: number
   - boundingBox: { min, max }
   - meshPreview: base64 or URL
   ```

2. **Geometry Extraction**
   ```typescript
   async function processIGES(filePath: string) {
     const oc = await initOpenCascade();
     
     // Read IGES
     const reader = new oc.IGESControl_Reader_1();
     reader.ReadFile(filePath);
     reader.TransferRoots();
     const shape = reader.OneShape();
     
     // Extract properties
     const props = new oc.GProp_GProps_1();
     oc.BRepGProp.VolumeProperties(shape, props);
     
     // Generate mesh
     const mesh = generateMesh(shape, oc);
     
     // Extract stations/waterlines
     const stations = extractStations(shape, oc, 21);
     const waterlines = extractWaterlines(shape, oc, 13);
     
     return {
       volume: props.Mass(),
       surfaceArea: calculateArea(shape, oc),
       mesh: mesh,
       stations: stations,
       waterlines: waterlines
     };
   }
   ```

3. **Mesh Generation**
   ```typescript
   function generateMesh(shape: any, oc: any) {
     // Mesh the shape
     oc.BRepMesh.Mesh(shape, 0.1); // 0.1 = deflection tolerance
     
     const vertices: number[] = [];
     const indices: number[] = [];
     const normals: number[] = [];
     
     // Iterate faces
     const faceExplorer = new oc.TopExp_Explorer_2(
       shape, 
       oc.TopAbs_ShapeEnum.TopAbs_FACE
     );
     
     while (faceExplorer.More()) {
       const face = faceExplorer.Current();
       const location = new oc.TopLoc_Location_1();
       const triangulation = oc.BRep_Tool.Triangulation(face, location);
       
       if (triangulation) {
         // Extract vertices and indices
         for (let i = 1; i <= triangulation.NbNodes(); i++) {
           const node = triangulation.Node(i);
           vertices.push(node.X(), node.Y(), node.Z());
         }
         
         for (let i = 1; i <= triangulation.NbTriangles(); i++) {
           const triangle = triangulation.Triangle(i);
           indices.push(triangle.Value(1), triangle.Value(2), triangle.Value(3));
         }
       }
       
       faceExplorer.Next();
     }
     
     return { vertices, indices, normals };
   }
   ```

4. **Station/Waterline Extraction**
   ```typescript
   function extractStations(shape: any, oc: any, count: number) {
     const bbox = calculateBoundingBox(shape, oc);
     const step = (bbox.max.x - bbox.min.x) / (count - 1);
     
     const stations = [];
     
     for (let i = 0; i < count; i++) {
       const x = bbox.min.x + i * step;
       
       // Create cutting plane perpendicular to X axis
       const plane = new oc.gp_Pln_3(
         new oc.gp_Pnt_3(x, 0, 0),
         new oc.gp_Dir_3(1, 0, 0)
       );
       
       // Intersect shape with plane
       const section = oc.BRepAlgoAPI_Section.ctor_1(shape, plane);
       section.Build();
       
       if (section.IsDone()) {
         const sectionShape = section.Shape();
         const offsets = extractOffsets(sectionShape, oc);
         stations.push({ x, offsets });
       }
     }
     
     return stations;
   }
   ```

**Deliverable:** Backend service that processes IGES → JSON geometry

---

### **Phase 3: Database Storage (1 day)**

**Schema Update:**
```sql
-- Add geometry columns to catalog_hulls
ALTER TABLE catalog_real.vessels ADD COLUMN geometry_mesh JSONB;
ALTER TABLE catalog_real.vessels ADD COLUMN geometry_stations JSONB;
ALTER TABLE catalog_real.vessels ADD COLUMN geometry_waterlines JSONB;
ALTER TABLE catalog_real.vessels ADD COLUMN iges_file_s3_key TEXT;

-- Index for querying geometry
CREATE INDEX idx_vessels_has_geometry ON catalog_real.vessels ((geometry_mesh IS NOT NULL));
```

**Data Model:**
```typescript
interface HullGeometry {
  mesh: {
    vertices: number[];      // [x, y, z, x, y, z, ...]
    indices: number[];       // Triangle indices
    normals?: number[];      // [nx, ny, nz, ...]
  };
  stations: StationOffset[];
  waterlines: WaterlineOffset[];
  metadata: {
    volumeM3: number;
    surfaceAreaM2: number;
    boundingBox: { min: Point3D, max: Point3D };
    sourceFile: string;
    importedAt: Date;
  };
}
```

**Deliverable:** Geometry stored in database, queryable

---

### **Phase 4: Frontend 3D Viewer (2-3 days)**

**Component:** `frontend/src/components/catalog/Hull3DViewer.tsx`

```typescript
import React, { useEffect, useRef } from 'react';
import * as THREE from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls';

interface Hull3DViewerProps {
  geometry: HullGeometry;
  showStations?: boolean;
  showWaterlines?: boolean;
}

export const Hull3DViewer: React.FC<Hull3DViewerProps> = ({ 
  geometry, 
  showStations = true,
  showWaterlines = true 
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  
  useEffect(() => {
    if (!containerRef.current) return;
    
    // Setup scene
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0xf0f0f0);
    
    // Setup camera
    const camera = new THREE.PerspectiveCamera(
      45, 
      containerRef.current.clientWidth / containerRef.current.clientHeight,
      0.1,
      1000
    );
    camera.position.set(50, 50, 50);
    
    // Setup renderer
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(
      containerRef.current.clientWidth, 
      containerRef.current.clientHeight
    );
    containerRef.current.appendChild(renderer.domElement);
    
    // Setup controls
    const controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    
    // Create hull mesh
    const hullGeometry = new THREE.BufferGeometry();
    hullGeometry.setAttribute(
      'position', 
      new THREE.Float32BufferAttribute(geometry.mesh.vertices, 3)
    );
    hullGeometry.setIndex(geometry.mesh.indices);
    hullGeometry.computeVertexNormals();
    
    const hullMaterial = new THREE.MeshPhongMaterial({
      color: 0x4488ff,
      shininess: 30,
      side: THREE.DoubleSide,
      transparent: true,
      opacity: 0.8
    });
    
    const hullMesh = new THREE.Mesh(hullGeometry, hullMaterial);
    scene.add(hullMesh);
    
    // Add stations
    if (showStations) {
      geometry.stations.forEach((station, i) => {
        const points = station.offsets.map(o => 
          new THREE.Vector3(station.x, o.y, o.z)
        );
        const curve = new THREE.CatmullRomCurve3(points);
        const tubeGeometry = new THREE.TubeGeometry(curve, 32, 0.05, 8);
        const tubeMaterial = new THREE.MeshBasicMaterial({ color: 0xff0000 });
        const tube = new THREE.Mesh(tubeGeometry, tubeMaterial);
        scene.add(tube);
      });
    }
    
    // Add waterlines
    if (showWaterlines) {
      geometry.waterlines.forEach((wl, i) => {
        const points = wl.offsets.map(o => 
          new THREE.Vector3(o.x, o.y, wl.z)
        );
        const curve = new THREE.CatmullRomCurve3(points);
        const tubeGeometry = new THREE.TubeGeometry(curve, 32, 0.05, 8);
        const tubeMaterial = new THREE.MeshBasicMaterial({ color: 0x00ff00 });
        const tube = new THREE.Mesh(tubeGeometry, tubeMaterial);
        scene.add(tube);
      });
    }
    
    // Add lights
    const ambientLight = new THREE.AmbientLight(0x404040);
    scene.add(ambientLight);
    
    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
    directionalLight.position.set(100, 100, 50);
    scene.add(directionalLight);
    
    // Add grid
    const gridHelper = new THREE.GridHelper(200, 20);
    scene.add(gridHelper);
    
    // Add axes
    const axesHelper = new THREE.AxesHelper(50);
    scene.add(axesHelper);
    
    // Animation loop
    const animate = () => {
      requestAnimationFrame(animate);
      controls.update();
      renderer.render(scene, camera);
    };
    animate();
    
    // Cleanup
    return () => {
      renderer.dispose();
      containerRef.current?.removeChild(renderer.domElement);
    };
  }, [geometry, showStations, showWaterlines]);
  
  return (
    <div className="relative w-full h-[600px] rounded-lg border border-gray-300">
      <div ref={containerRef} className="w-full h-full" />
      
      {/* Controls overlay */}
      <div className="absolute top-4 right-4 bg-white/90 rounded-lg p-4 shadow-lg">
        <h3 className="font-semibold mb-2">View Controls</h3>
        <ul className="text-sm space-y-1">
          <li>🖱️ Left drag: Rotate</li>
          <li>🖱️ Right drag: Pan</li>
          <li>🖱️ Scroll: Zoom</li>
        </ul>
      </div>
    </div>
  );
};
```

**Deliverable:** Interactive 3D viewer in catalog detail page

---

### **Phase 5: Integration & Testing (2-3 days)**

**Tasks:**
1. Add "Upload IGES" button to catalog admin UI
2. Process uploaded files through geometry service
3. Store results in database
4. Display in 3D viewer
5. Validate against published data (volume, Cb, etc.)
6. Performance testing (large files)
7. Error handling (corrupt files, unsupported entities)

**Test Cases:**
- ✅ Import KVLCC2 IGES → verify volume matches published
- ✅ Import KCS IGES → verify Cb matches published
- ✅ Display in 3D viewer → interactive controls work
- ✅ Extract stations → compare with offset tables
- ✅ Handle corrupt file → graceful error

**Deliverable:** End-to-end IGES import working

---

## 📊 **ENTITY TYPE REFERENCE**

From `.plan/app-docs/templates/MLData/iges_entity_types.txt`, key entities for hulls:

| Entity | Type | Purpose | Frequency |
|--------|------|---------|-----------|
| **128** | NURBS Surface | Modern hull surfaces | ⭐⭐⭐ High |
| **144** | Trimmed Surface | Hull panels | ⭐⭐⭐ High |
| **126** | NURBS Curve | Hull curves | ⭐⭐⭐ High |
| **102** | Composite Curve | Waterlines/buttocks | ⭐⭐ Medium |
| **114** | Parametric Spline Surface | Hull surfaces | ⭐⭐ Medium |
| **186** | Solid BREP | Complete hull solid | ⭐ Low |

**Strategy:** Focus on entities 126, 128, 144 first (covers 90% of hull files)

---

## 💰 **COST ANALYSIS**

### **Development Costs:**
- OpenCascade.js setup: 4-6 hours
- Backend processing service: 12-16 hours
- Database schema: 4 hours
- Frontend 3D viewer: 16-20 hours
- Testing & validation: 12-16 hours
- **Total:** 48-62 hours (~1.5-2 weeks)

### **Runtime Costs:**
- WASM bundle size: ~50MB (one-time download, cached)
- Processing: ~2-5 seconds per IGES file
- Storage: ~1-5MB per hull (mesh + metadata)
- No additional AWS services needed

---

## 🎯 **SUCCESS CRITERIA**

### **Phase Complete When:**
- ✅ Can upload IGES file via UI
- ✅ Backend processes and extracts geometry
- ✅ Stores mesh + stations + waterlines in DB
- ✅ 3D viewer displays hull correctly
- ✅ Can rotate, zoom, pan hull
- ✅ Stations and waterlines visible
- ✅ Volume/Cb matches published data (±5%)
- ✅ Processing time <10 seconds per file

---

## 🚀 **QUICK START (Proof of Concept)**

### **Step 1: Install OpenCascade.js**
```bash
cd backend
mkdir GeometryService
cd GeometryService
npm init -y
npm install opencascade.js express multer
```

### **Step 2: Create Test Script**
```javascript
// test-iges.js
const { initOpenCascade } = require('opencascade.js');
const fs = require('fs');

async function testIGES() {
  const oc = await initOpenCascade();
  
  const reader = new oc.IGESControl_Reader_1();
  const status = reader.ReadFile('sample.iges');
  
  if (status === oc.IFSelect_ReturnStatus.IFSelect_RetDone) {
    reader.TransferRoots();
    const shape = reader.OneShape();
    
    const props = new oc.GProp_GProps_1();
    oc.BRepGProp.VolumeProperties(shape, props);
    
    console.log('Volume:', props.Mass());
    console.log('✅ IGES parsing works!');
  }
}

testIGES();
```

### **Step 3: Test with Sample File**
```bash
# Download sample IGES (if available)
node test-iges.js
```

---

## 📚 **RESOURCES**

### **Libraries:**
- OpenCascade.js: https://ocjs.org/
- three-iges-loader: https://www.npmjs.com/package/three-iges-loader
- IxMilia.Iges: https://github.com/ixmilia/iges

### **Documentation:**
- IGES Specification: https://www.nist.gov/publications/initial-graphics-exchange-specification-iges-53
- OpenCascade Tutorials: https://dev.opencascade.org/doc/overview/html/
- Three.js Examples: https://threejs.org/examples/

### **Sample Files:**
- SIMMAN 2008: https://www.simman2008.dk/ (may require registration)
- GrabCAD: https://grabcad.com/ (user-submitted CAD files)

---

## ✅ **RECOMMENDATION**

**Start with Option 2 (OpenCascade.js) for backend processing:**
- Most complete solution
- Industry-standard CAD kernel
- Handles all common IGES entities
- Can compute hydrostatic properties directly
- Future-proof (supports STEP, BREP, etc.)

**Add Option 1 (three-iges-loader) for frontend later:**
- Lighter weight for visualization
- Can load files client-side
- Good fallback if backend unavailable

**Timeline:**
- Week 1: Backend processing service + database
- Week 2: Frontend 3D viewer + integration
- Week 3: Testing + validation + polish

**Priority:** MEDIUM (nice-to-have for visualization, not critical for calculations)

---

**Let's implement this after benchmark data import is complete!** 🚀
