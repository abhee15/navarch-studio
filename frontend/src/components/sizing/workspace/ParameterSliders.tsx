import { useState, useEffect, useCallback, useMemo } from "react";
import { Label } from "../../ui/label";
import { Select } from "../../ui/select";
import type { CandidateDesign } from "../../../types/sizing";
import {
  Sliders,
  Ruler,
  BarChart2,
  Triangle,
  ArrowLeftRight,
  ChevronRight,
  ChevronLeft,
  Square,
  Circle,
  Info,
} from "lucide-react";
import {
  isParameterGroupVisible,
  isParameterVisible,
  type ParameterId,
} from "../../../utils/shipdParameterFilter";

interface ParameterSlidersProps {
  candidate: CandidateDesign;
  onUpdate: (updates: Partial<CandidateDesign>) => void;
  isUpdating?: boolean;
}

type ParameterGroup =
  | "dimensions"
  | "coefficients"
  | "longitudinal"
  | "bow"
  | "stern"
  | "midship"
  | "bulb";

/**
 * Interactive Parameter Sliders
 *
 * Allows real-time adjustment of hull dimensions and advanced ShipD parameters
 * with live preview updates and hybrid solver mode
 */
export const ParameterSliders: React.FC<ParameterSlidersProps> = ({
  candidate,
  onUpdate,
  isUpdating = false,
}) => {
  const [selectedGroup, setSelectedGroup] = useState<ParameterGroup>("dimensions");

  // Filter parameters based on vessel configuration
  const filterConfig = useMemo(
    () => ({
      candidate,
      vesselType: candidate.vesselType,
      vesselCategory: candidate.vesselCategory,
      maskVersion: candidate.familyMaskVersion,
    }),
    [candidate]
  );

  // Get visible parameter groups
  const visibleGroups = useMemo(() => {
    const groups: ParameterGroup[] = [
      "dimensions",
      "coefficients",
      "longitudinal",
      "bow",
      "stern",
      "midship",
      "bulb",
    ];
    return groups.filter((group) => isParameterGroupVisible(group, filterConfig));
  }, [filterConfig]);

  // Update selected group if current selection is not visible
  useEffect(() => {
    if (!visibleGroups.includes(selectedGroup)) {
      setSelectedGroup(visibleGroups[0] || "dimensions");
    }
  }, [visibleGroups, selectedGroup]);

  // Initialize local values from candidate
  const initializeLocalValues = useCallback(
    () => ({
      // Basic - Dimensions
      lpp: candidate.lppM,
      beam: candidate.beamM,
      draft: candidate.draftM,
      depth: candidate.depthM,

      // Basic - Coefficients
      cb: candidate.cb,
      cp: candidate.cp,
      cwp: candidate.cwp,

      // Advanced - Longitudinal
      bowLength: candidate.bowLengthRatio ?? 0.25,
      sternLength: candidate.sternLengthRatio ?? 0.25,

      // Advanced - Bow shape
      bowFlareAngle: candidate.bowFlareAngle ?? 15,
      bowCurvature: candidate.bowCurvature ?? 0.5,
      bowKnuckle: candidate.bowKnuckle ?? 0.0,
      deadriseAngle: candidate.deadriseAngle ?? 10,

      // Advanced - Stern shape
      sternRakeAngle: candidate.sternRakeAngle ?? 15,
      sternCurvature: candidate.sternCurvature ?? 0.5,
      sternKnuckle: candidate.sternKnuckle ?? 0.0,
      transomArea: candidate.transomArea ?? 0.0,
      transomWidth: candidate.transomWidth ?? 0.8,

      // Advanced - Midship
      hasSheer: candidate.hasSheer ?? false,
      hasTumblehome: candidate.hasTumblehome ?? false,

      // Advanced - Bulb
      hasBulb: candidate.hasBulb ?? false,
      bulbLength: candidate.bulbLengthRatio ?? 0.04,
      bulbHeight: candidate.bulbHeightRatio ?? 0.5,
      bulbWidth: candidate.bulbWidthRatio ?? 0.7,
      bulbAsymmetry: candidate.bulbAsymmetry ?? 0.5,
      bulbFillet: candidate.bulbFilletRadius ?? 0.2,
    }),
    [
      candidate.lppM,
      candidate.beamM,
      candidate.draftM,
      candidate.depthM,
      candidate.cb,
      candidate.cp,
      candidate.cwp,
      candidate.bowLengthRatio,
      candidate.sternLengthRatio,
      candidate.bowFlareAngle,
      candidate.bowCurvature,
      candidate.bowKnuckle,
      candidate.deadriseAngle,
      candidate.sternRakeAngle,
      candidate.sternCurvature,
      candidate.sternKnuckle,
      candidate.transomArea,
      candidate.transomWidth,
      candidate.hasSheer,
      candidate.hasTumblehome,
      candidate.hasBulb,
      candidate.bulbLengthRatio,
      candidate.bulbHeightRatio,
      candidate.bulbWidthRatio,
      candidate.bulbAsymmetry,
      candidate.bulbFilletRadius,
    ]
  );

  const [localValues, setLocalValues] = useState(initializeLocalValues);

  // Sync localValues when candidate changes (e.g., when parameters are loaded from backend)
  useEffect(() => {
    setLocalValues(initializeLocalValues());
  }, [initializeLocalValues]);

  const handleSliderChange = (param: keyof typeof localValues, value: number | boolean) => {
    setLocalValues((prev) => ({ ...prev, [param]: value }));
  };

  const handleSliderRelease = (param: keyof typeof localValues) => {
    // Trigger update to backend/solver (hybrid mode: fast preview + background solver)
    const updates: Partial<CandidateDesign> = {};

    // Map local parameter names to CandidateDesign properties
    const paramMap: Record<keyof typeof localValues, keyof CandidateDesign> = {
      lpp: "lppM",
      beam: "beamM",
      draft: "draftM",
      depth: "depthM",
      cb: "cb",
      cp: "cp",
      cwp: "cwp",
      bowLength: "bowLengthRatio",
      sternLength: "sternLengthRatio",
      bowFlareAngle: "bowFlareAngle",
      bowCurvature: "bowCurvature",
      bowKnuckle: "bowKnuckle",
      deadriseAngle: "deadriseAngle",
      sternRakeAngle: "sternRakeAngle",
      sternCurvature: "sternCurvature",
      sternKnuckle: "sternKnuckle",
      transomArea: "transomArea",
      transomWidth: "transomWidth",
      hasSheer: "hasSheer",
      hasTumblehome: "hasTumblehome",
      hasBulb: "hasBulb",
      bulbLength: "bulbLengthRatio",
      bulbHeight: "bulbHeightRatio",
      bulbWidth: "bulbWidthRatio",
      bulbAsymmetry: "bulbAsymmetry",
      bulbFillet: "bulbFilletRadius",
    };

    const mappedKey = paramMap[param];
    if (mappedKey) {
      // Type-safe assignment to updates object
      const value = localValues[param];
      // Use Record type for type-safe dynamic property assignment
      (updates as Record<string, number | boolean>)[mappedKey] = value;
    }

    onUpdate(updates);
  };

  // Parameter group definitions with filtered counts
  const parameterGroups = useMemo(() => {
    const allGroups = [
      { value: "dimensions" as const, label: "Dimensions", icon: Ruler },
      { value: "coefficients" as const, label: "Coefficients", icon: Triangle },
      { value: "longitudinal" as const, label: "Longitudinal", icon: ArrowLeftRight },
      { value: "bow" as const, label: "Bow Shape", icon: ChevronRight },
      { value: "stern" as const, label: "Stern Shape", icon: ChevronLeft },
      { value: "midship" as const, label: "Midship", icon: Square },
      { value: "bulb" as const, label: "Bulbous Bow", icon: Circle },
    ];

    // Filter to only visible groups and add counts
    return allGroups
      .filter((g) => visibleGroups.includes(g.value))
      .map((g) => {
        let count = 0;
        switch (g.value) {
          case "dimensions":
            count = ["lpp", "beam", "draft", "depth"].filter((p) =>
              isParameterVisible(p as ParameterId, filterConfig)
            ).length;
            break;
          case "coefficients":
            count = ["cb", "cp", "cwp"].filter((p) =>
              isParameterVisible(p as ParameterId, filterConfig)
            ).length;
            break;
          case "longitudinal":
            count = ["bowLength", "sternLength"].filter((p) =>
              isParameterVisible(p as ParameterId, filterConfig)
            ).length;
            break;
          case "bow":
            count = ["bowFlareAngle", "bowCurvature", "bowKnuckle", "deadriseAngle"].filter((p) =>
              isParameterVisible(p as ParameterId, filterConfig)
            ).length;
            break;
          case "stern":
            count = [
              "sternRakeAngle",
              "sternCurvature",
              "sternKnuckle",
              "transomArea",
              "transomWidth",
            ].filter((p) => isParameterVisible(p as ParameterId, filterConfig)).length;
            break;
          case "midship":
            count = ["hasSheer", "hasTumblehome"].filter((p) =>
              isParameterVisible(p as ParameterId, filterConfig)
            ).length;
            break;
          case "bulb":
            count = [
              "hasBulb",
              "bulbLength",
              "bulbHeight",
              "bulbWidth",
              "bulbAsymmetry",
              "bulbFillet",
            ].filter((p) => isParameterVisible(p as ParameterId, filterConfig)).length;
            break;
        }
        return { ...g, count };
      });
  }, [visibleGroups, filterConfig]);

  const currentGroup = parameterGroups.find((g) => g.value === selectedGroup);

  // Render slider component
  const renderSlider = (config: {
    id: keyof typeof localValues;
    label: string;
    value: number;
    min: number;
    max: number;
    step: number;
    unit: string;
    description?: string;
    color?: string;
  }) => (
    <div key={config.id} className="space-y-2">
      <div className="flex items-center justify-between">
        <div className="flex-1">
          <Label className="text-sm font-medium text-foreground">{config.label}</Label>
          {config.description && (
            <p className="text-xs text-muted-foreground mt-0.5">{config.description}</p>
          )}
        </div>
        <span
          className={`text-sm font-bold tabular-nums ${config.color ? `text-${config.color}-600 dark:text-${config.color}-400` : "text-primary"}`}
        >
          {config.value.toFixed(config.step >= 1 ? 1 : 2)} {config.unit}
        </span>
      </div>

      <div className="relative">
        <input
          type="range"
          min={config.min}
          max={config.max}
          step={config.step}
          value={config.value}
          onChange={(e) => handleSliderChange(config.id, parseFloat(e.target.value))}
          onMouseUp={() => handleSliderRelease(config.id)}
          onTouchEnd={() => handleSliderRelease(config.id)}
          disabled={isUpdating}
          className="w-full h-2.5 rounded-lg appearance-none cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed bg-muted"
        />
      </div>

      <div className="flex justify-between text-xs text-muted-foreground">
        <span>{config.min.toFixed(config.step >= 1 ? 1 : 2)}</span>
        <span>{config.max.toFixed(config.step >= 1 ? 1 : 2)}</span>
      </div>
    </div>
  );

  // Render toggle switch
  const renderToggle = (config: {
    id: keyof typeof localValues;
    label: string;
    description: string;
  }) => (
    <div
      key={config.id}
      className="flex items-center justify-between p-3 rounded-lg border border-border bg-muted/30"
    >
      <div className="flex-1">
        <Label className="text-sm font-medium cursor-pointer">{config.label}</Label>
        <p className="text-xs text-muted-foreground mt-0.5">{config.description}</p>
      </div>
      <button
        type="button"
        role="switch"
        aria-checked={!!localValues[config.id]}
        onClick={() => {
          const newValue = !localValues[config.id];
          handleSliderChange(config.id, newValue);
          handleSliderRelease(config.id);
        }}
        disabled={isUpdating}
        className={`
          relative inline-flex h-6 w-11 items-center rounded-full transition-colors
          ${localValues[config.id] ? "bg-primary" : "bg-muted"}
          ${isUpdating ? "opacity-50 cursor-not-allowed" : "cursor-pointer"}
        `}
      >
        <span
          className={`
            inline-block h-4 w-4 transform rounded-full bg-card transition-transform
            ${localValues[config.id] ? "translate-x-6" : "translate-x-1"}
          `}
        />
      </button>
    </div>
  );

  // Calculate derived ratios
  const lOverB = (localValues.lpp / localValues.beam).toFixed(2);
  const bOverT = (localValues.beam / localValues.draft).toFixed(2);
  const dOverT = (localValues.depth / localValues.draft).toFixed(2);
  const midBodyLength = (1 - localValues.bowLength - localValues.sternLength).toFixed(2);

  return (
    <div className="space-y-6">
      <div className="rounded-lg border border-border bg-card overflow-hidden shadow-lg">
        {/* Header */}
        <div className="bg-gradient-to-r from-blue-50 to-cyan-50 dark:from-blue-900/20 dark:to-cyan-900/20 px-4 py-3 border-b border-border">
          <h3 className="font-semibold text-foreground flex items-center gap-2">
            <Sliders className="h-4 w-4 text-primary" />
            Interactive Parameters
          </h3>
          <p className="text-xs text-muted-foreground mt-1">
            Adjust dimensions and form to explore design space
          </p>
        </div>

        <div className="p-6 space-y-6">
          {/* Parameter Group Selector (Dropdown) */}
          <div className="space-y-2">
            <Label className="text-sm font-medium text-foreground">Parameter Group</Label>
            <Select
              value={selectedGroup}
              onChange={(value) => setSelectedGroup(value as ParameterGroup)}
              options={parameterGroups.map((group) => ({
                value: group.value,
                label: `${group.label} (${group.count})`,
              }))}
              className="w-full"
              disabled={isUpdating}
            />
          </div>

          {/* GROUP 1: DIMENSIONS */}
          {selectedGroup === "dimensions" && (
            <div className="space-y-6">
              <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                {currentGroup && <currentGroup.icon className="h-4 w-4 text-blue-500" />}
                <span>Principal Dimensions</span>
              </div>

              <div className="space-y-4">
                {isParameterVisible("lpp", filterConfig) &&
                  renderSlider({
                    id: "lpp",
                    label: "Length (Lpp)",
                    value: localValues.lpp,
                    min: candidate.lppM * 0.7,
                    max: candidate.lppM * 1.3,
                    step: 0.5,
                    unit: "m",
                    description: "Length between perpendiculars",
                    color: "blue",
                  })}

                {isParameterVisible("beam", filterConfig) &&
                  renderSlider({
                    id: "beam",
                    label: "Beam",
                    value: localValues.beam,
                    min: candidate.beamM * 0.7,
                    max: candidate.beamM * 1.3,
                    step: 0.1,
                    unit: "m",
                    description: "Maximum beam (width)",
                    color: "cyan",
                  })}

                {isParameterVisible("draft", filterConfig) &&
                  renderSlider({
                    id: "draft",
                    label: "Draft",
                    value: localValues.draft,
                    min: candidate.draftM * 0.7,
                    max: candidate.draftM * 1.3,
                    step: 0.1,
                    unit: "m",
                    description: "Depth of hull below waterline",
                    color: "green",
                  })}

                {isParameterVisible("depth", filterConfig) &&
                  renderSlider({
                    id: "depth",
                    label: "Depth",
                    value: localValues.depth,
                    min: candidate.depthM * 0.8,
                    max: candidate.depthM * 1.2,
                    step: 0.1,
                    unit: "m",
                    description: "Molded depth from keel to deck",
                    color: "amber",
                  })}
              </div>

              {/* Derived Ratios */}
              <div className="rounded-lg bg-gradient-to-r from-gray-50 to-blue-50 dark:from-gray-800 dark:to-blue-900/20 p-4 border border-border">
                <h4 className="text-xs font-semibold text-foreground mb-3 flex items-center gap-2">
                  <BarChart2 className="h-4 w-4 text-emerald-500" />
                  Derived Ratios (Live)
                </h4>
                <div className="grid grid-cols-3 gap-3 text-center">
                  <div>
                    <div className="text-xs text-muted-foreground mb-1">L/B</div>
                    <div className="text-lg font-bold text-blue-600 dark:text-blue-400">
                      {lOverB}
                    </div>
                  </div>
                  <div>
                    <div className="text-xs text-muted-foreground mb-1">B/T</div>
                    <div className="text-lg font-bold text-cyan-600 dark:text-cyan-400">
                      {bOverT}
                    </div>
                  </div>
                  <div>
                    <div className="text-xs text-muted-foreground mb-1">D/T</div>
                    <div className="text-lg font-bold text-green-600 dark:text-green-400">
                      {dOverT}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* GROUP 2: COEFFICIENTS */}
          {selectedGroup === "coefficients" && (
            <div className="space-y-6">
              <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                {currentGroup && <currentGroup.icon className="h-4 w-4 text-purple-500" />}
                <span>Form Coefficients</span>
              </div>

              <div className="flex items-center gap-2 text-sm text-muted-foreground bg-blue-50 dark:bg-blue-900/20 p-3 rounded-lg">
                <Info className="h-4 w-4 flex-shrink-0" />
                <span>Control hull fullness and volume distribution</span>
              </div>

              <div className="space-y-4">
                {isParameterVisible("cb", filterConfig) &&
                  renderSlider({
                    id: "cb",
                    label: "Block Coefficient (Cb)",
                    value: localValues.cb,
                    min: Math.max(0.4, candidate.cb - 0.15),
                    max: Math.min(0.9, candidate.cb + 0.15),
                    step: 0.01,
                    unit: "",
                    description: "Overall hull fullness (V / L×B×T)",
                    color: "purple",
                  })}

                {isParameterVisible("cp", filterConfig) &&
                  renderSlider({
                    id: "cp",
                    label: "Prismatic Coefficient (Cp)",
                    value: localValues.cp,
                    min: Math.max(0.5, candidate.cp - 0.1),
                    max: Math.min(0.95, candidate.cp + 0.1),
                    step: 0.01,
                    unit: "",
                    description: "Longitudinal fullness (V / Am×L)",
                    color: "indigo",
                  })}

                {isParameterVisible("cwp", filterConfig) &&
                  renderSlider({
                    id: "cwp",
                    label: "Waterplane Coefficient (Cwp)",
                    value: localValues.cwp,
                    min: Math.max(0.6, candidate.cwp - 0.1),
                    max: Math.min(0.98, candidate.cwp + 0.1),
                    step: 0.01,
                    unit: "",
                    description: "Waterplane area fullness (Awp / L×B)",
                    color: "violet",
                  })}
              </div>
            </div>
          )}

          {/* GROUP 3: LONGITUDINAL */}
          {selectedGroup === "longitudinal" && (
            <div className="space-y-6">
              <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                {currentGroup && <currentGroup.icon className="h-4 w-4 text-orange-500" />}
                <span>Longitudinal Proportions</span>
              </div>

              <div className="flex items-center gap-2 text-sm text-muted-foreground bg-blue-50 dark:bg-blue-900/20 p-3 rounded-lg">
                <Info className="h-4 w-4 flex-shrink-0" />
                <span>Control bow, mid-body, and stern length distribution</span>
              </div>

              {/* Visual distribution bar */}
              <div className="space-y-2">
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>Bow</span>
                  <span>Mid-body</span>
                  <span>Stern</span>
                </div>
                <div className="flex h-10 rounded-md overflow-hidden border border-border shadow-sm">
                  <div
                    className="bg-blue-500 flex items-center justify-center text-xs font-medium text-white transition-all"
                    style={{ width: `${(localValues.bowLength * 100).toFixed(1)}%` }}
                  >
                    {(localValues.bowLength * 100).toFixed(0)}%
                  </div>
                  <div
                    className="bg-green-500 flex items-center justify-center text-xs font-medium text-white transition-all"
                    style={{ width: `${(parseFloat(midBodyLength) * 100).toFixed(1)}%` }}
                  >
                    {(parseFloat(midBodyLength) * 100).toFixed(0)}%
                  </div>
                  <div
                    className="bg-purple-500 flex items-center justify-center text-xs font-medium text-white transition-all"
                    style={{ width: `${(localValues.sternLength * 100).toFixed(1)}%` }}
                  >
                    {(localValues.sternLength * 100).toFixed(0)}%
                  </div>
                </div>
              </div>

              <div className="space-y-4">
                {isParameterVisible("bowLength", filterConfig) &&
                  renderSlider({
                    id: "bowLength",
                    label: "Bow Length Ratio (Lb)",
                    value: localValues.bowLength,
                    min: 0.1,
                    max: Math.min(0.45, 0.9 - localValues.sternLength),
                    step: 0.01,
                    unit: "",
                    description: "Proportion of LOA for bow region",
                  })}

                {isParameterVisible("sternLength", filterConfig) &&
                  renderSlider({
                    id: "sternLength",
                    label: "Stern Length Ratio (Ls)",
                    value: localValues.sternLength,
                    min: 0.1,
                    max: Math.min(0.45, 0.9 - localValues.bowLength),
                    step: 0.01,
                    unit: "",
                    description: "Proportion of LOA for stern region",
                  })}
              </div>
            </div>
          )}

          {/* GROUP 4: BOW SHAPE */}
          {selectedGroup === "bow" && (
            <div className="space-y-6">
              <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                {currentGroup && <currentGroup.icon className="h-4 w-4 text-blue-500" />}
                <span>Bow Shape</span>
              </div>

              <div className="flex items-center gap-2 text-sm text-muted-foreground bg-blue-50 dark:bg-blue-900/20 p-3 rounded-lg">
                <Info className="h-4 w-4 flex-shrink-0" />
                <span>Control bow entry angle, curvature, and form</span>
              </div>

              <div className="space-y-4">
                {isParameterVisible("bowFlareAngle", filterConfig) &&
                  renderSlider({
                    id: "bowFlareAngle",
                    label: "Flare Angle (β)",
                    value: localValues.bowFlareAngle,
                    min: 0,
                    max: 45,
                    step: 1,
                    unit: "°",
                    description: "Outward angle above waterline",
                  })}

                {isParameterVisible("bowCurvature", filterConfig) &&
                  renderSlider({
                    id: "bowCurvature",
                    label: "Curvature (Rc)",
                    value: localValues.bowCurvature,
                    min: 0.1,
                    max: 1.0,
                    step: 0.05,
                    unit: "",
                    description: "Section fullness (0.1=fine, 1.0=full)",
                  })}

                {isParameterVisible("bowKnuckle", filterConfig) &&
                  renderSlider({
                    id: "bowKnuckle",
                    label: "Knuckle (Rk)",
                    value: localValues.bowKnuckle,
                    min: 0.0,
                    max: 1.0,
                    step: 0.05,
                    unit: "",
                    description: "Hard chine effect (0=round, 1=angular)",
                  })}

                {isParameterVisible("deadriseAngle", filterConfig) &&
                  renderSlider({
                    id: "deadriseAngle",
                    label: "Deadrise Angle",
                    value: localValues.deadriseAngle,
                    min: 0,
                    max: 45,
                    step: 1,
                    unit: "°",
                    description: "V-shape at keel (0°=flat, 45°=sharp)",
                  })}
              </div>
            </div>
          )}

          {/* GROUP 5: STERN SHAPE */}
          {selectedGroup === "stern" && (
            <div className="space-y-6">
              <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                {currentGroup && <currentGroup.icon className="h-4 w-4 text-purple-500" />}
                <span>Stern Shape</span>
              </div>

              <div className="flex items-center gap-2 text-sm text-muted-foreground bg-blue-50 dark:bg-blue-900/20 p-3 rounded-lg">
                <Info className="h-4 w-4 flex-shrink-0" />
                <span>Control stern shape, rake, and transom</span>
              </div>

              <div className="space-y-4">
                {isParameterVisible("sternRakeAngle", filterConfig) &&
                  renderSlider({
                    id: "sternRakeAngle",
                    label: "Rake Angle",
                    value: localValues.sternRakeAngle,
                    min: 0,
                    max: 45,
                    step: 1,
                    unit: "°",
                    description: "Aft overhang angle",
                  })}

                {isParameterVisible("sternCurvature", filterConfig) &&
                  renderSlider({
                    id: "sternCurvature",
                    label: "Curvature (Rc_trans)",
                    value: localValues.sternCurvature,
                    min: 0.1,
                    max: 1.0,
                    step: 0.05,
                    unit: "",
                    description: "Section fullness",
                  })}

                {isParameterVisible("sternKnuckle", filterConfig) &&
                  renderSlider({
                    id: "sternKnuckle",
                    label: "Knuckle (Rk_trans)",
                    value: localValues.sternKnuckle,
                    min: 0.0,
                    max: 1.0,
                    step: 0.05,
                    unit: "",
                    description: "Hard chine effect",
                  })}

                {isParameterVisible("transomArea", filterConfig) &&
                  renderSlider({
                    id: "transomArea",
                    label: "Transom Area",
                    value: localValues.transomArea,
                    min: 0.0,
                    max: 1.0,
                    step: 0.05,
                    unit: "",
                    description: "Flat stern (0=pointed, 1=full transom)",
                  })}

                {isParameterVisible("transomWidth", filterConfig) &&
                  localValues.transomArea > 0.05 &&
                  renderSlider({
                    id: "transomWidth",
                    label: "Transom Width",
                    value: localValues.transomWidth,
                    min: 0.3,
                    max: 1.0,
                    step: 0.05,
                    unit: "",
                    description: "Width relative to beam",
                  })}
              </div>
            </div>
          )}

          {/* GROUP 6: MIDSHIP */}
          {selectedGroup === "midship" && (
            <div className="space-y-6">
              <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                {currentGroup && <currentGroup.icon className="h-4 w-4 text-green-500" />}
                <span>Midship Features</span>
              </div>

              <div className="flex items-center gap-2 text-sm text-muted-foreground bg-blue-50 dark:bg-blue-900/20 p-3 rounded-lg">
                <Info className="h-4 w-4 flex-shrink-0" />
                <span>Control deck edge features</span>
              </div>

              <div className="space-y-3">
                {isParameterVisible("hasSheer", filterConfig) &&
                  renderToggle({
                    id: "hasSheer",
                    label: "Sheer",
                    description: "Outward curve at deck edge",
                  })}

                {isParameterVisible("hasTumblehome", filterConfig) &&
                  renderToggle({
                    id: "hasTumblehome",
                    label: "Tumblehome",
                    description: "Inward slope at deck edge",
                  })}
              </div>
            </div>
          )}

          {/* GROUP 7: BULBOUS BOW */}
          {selectedGroup === "bulb" && (
            <div className="space-y-6">
              <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                {currentGroup && <currentGroup.icon className="h-4 w-4 text-cyan-500" />}
                <span>Bulbous Bow</span>
              </div>

              <div className="flex items-center gap-2 text-sm text-muted-foreground bg-blue-50 dark:bg-blue-900/20 p-3 rounded-lg">
                <Info className="h-4 w-4 flex-shrink-0" />
                <span>Control bulbous bow dimensions and shape</span>
              </div>

              {/* Bulb enable toggle */}
              {isParameterVisible("hasBulb", filterConfig) &&
                renderToggle({
                  id: "hasBulb",
                  label: "Enable Bulbous Bow",
                  description: "Add bulb for wave resistance reduction",
                })}

              {localValues.hasBulb && (
                <div className="space-y-4">
                  {isParameterVisible("bulbLength", filterConfig) &&
                    renderSlider({
                      id: "bulbLength",
                      label: "Length Ratio (Lbb)",
                      value: localValues.bulbLength,
                      min: 0.02,
                      max: 0.08,
                      step: 0.005,
                      unit: "",
                      description: "Relative to Lpp",
                    })}

                  {isParameterVisible("bulbHeight", filterConfig) &&
                    renderSlider({
                      id: "bulbHeight",
                      label: "Height Ratio (Hbb)",
                      value: localValues.bulbHeight,
                      min: 0.3,
                      max: 0.8,
                      step: 0.05,
                      unit: "",
                      description: "Relative to draft",
                    })}

                  {isParameterVisible("bulbWidth", filterConfig) &&
                    renderSlider({
                      id: "bulbWidth",
                      label: "Width Ratio (Bbb)",
                      value: localValues.bulbWidth,
                      min: 0.4,
                      max: 0.9,
                      step: 0.05,
                      unit: "",
                      description: "Relative to beam",
                    })}

                  {isParameterVisible("bulbAsymmetry", filterConfig) &&
                    renderSlider({
                      id: "bulbAsymmetry",
                      label: "Asymmetry (Lbbm)",
                      value: localValues.bulbAsymmetry,
                      min: 0.3,
                      max: 0.7,
                      step: 0.05,
                      unit: "",
                      description: "Fore/aft position (0.5=symmetric)",
                    })}

                  {isParameterVisible("bulbFillet", filterConfig) &&
                    renderSlider({
                      id: "bulbFillet",
                      label: "Fillet Radius (Rbb)",
                      value: localValues.bulbFillet,
                      min: 0.05,
                      max: 0.33,
                      step: 0.01,
                      unit: "",
                      description: "Roundness (low=pointed, high=round)",
                    })}
                </div>
              )}
            </div>
          )}

          {/* Update indicator */}
          {isUpdating && (
            <div className="rounded-lg bg-blue-50 dark:bg-blue-900/20 p-3 flex items-center gap-3">
              <div className="h-4 w-4 animate-spin rounded-full border-2 border-blue-600 border-t-transparent"></div>
              <span className="text-sm text-blue-800 dark:text-blue-300">
                Updating hull geometry...
              </span>
            </div>
          )}
        </div>
      </div>

      <style>{`
        /* Webkit (Chrome, Safari, Edge) */
        input[type="range"]::-webkit-slider-thumb {
          appearance: none;
          width: 20px;
          height: 20px;
          border-radius: 50%;
          background: linear-gradient(135deg, #3b82f6, #06b6d4);
          border: 3px solid white;
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
          cursor: pointer;
          transition: transform 0.2s, box-shadow 0.2s;
        }

        input[type="range"]::-webkit-slider-thumb:hover {
          transform: scale(1.15);
          box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
        }

        input[type="range"]::-webkit-slider-thumb:active {
          transform: scale(1.05);
        }

        /* Firefox */
        input[type="range"]::-moz-range-thumb {
          width: 20px;
          height: 20px;
          border-radius: 50%;
          background: linear-gradient(135deg, #3b82f6, #06b6d4);
          border: 3px solid white;
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
          cursor: pointer;
          transition: transform 0.2s, box-shadow 0.2s;
        }

        input[type="range"]::-moz-range-thumb:hover {
          transform: scale(1.15);
          box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
        }

        input[type="range"]::-moz-range-thumb:active {
          transform: scale(1.05);
        }

        /* Ensure track is visible in both light and dark modes */
        input[type="range"]::-webkit-slider-runnable-track {
          width: 100%;
          height: 10px;
          border-radius: 5px;
        }

        input[type="range"]::-moz-range-track {
          width: 100%;
          height: 10px;
          border-radius: 5px;
        }
      `}</style>
    </div>
  );
};
