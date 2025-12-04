import { useState } from "react";
import { Label } from "../../ui/label";
import { Lightbulb, Settings, Zap, Scale, Sparkles } from "lucide-react";

interface VisualizationSettingsProps {
  onSettingsChange: (settings: VisualizationOptions) => void;
}

export interface VisualizationOptions {
  show3DWaterplane: boolean;
  show3DCenters: boolean;
  show3DLabels: boolean;
  show3DGrid: boolean;
  show3DWaterlines: boolean;
  show3DButtocks: boolean;
  show3DSections: boolean;
  show3DWireframe: boolean;
  show2DWaterlines: boolean;
  show2DButtocks: boolean;
  show2DSections: boolean;
  waterlineCount: number;
  buttockCount: number;
  sectionCount: number;
  meshQuality: "low" | "medium" | "high" | "ultra";
  enableAnimations: boolean;
}

/**
 * Visualization Settings Panel
 *
 * Control visibility and quality of visualization elements
 */
export const VisualizationSettings: React.FC<VisualizationSettingsProps> = ({
  onSettingsChange,
}) => {
  const [settings, setSettings] = useState<VisualizationOptions>({
    show3DWaterplane: true,
    show3DCenters: true,
    show3DLabels: true,
    show3DGrid: true,
    show3DWaterlines: false,
    show3DButtocks: false,
    show3DSections: false,
    show3DWireframe: false,
    show2DWaterlines: true,
    show2DButtocks: true,
    show2DSections: true,
    waterlineCount: 7,
    buttockCount: 5,
    sectionCount: 10,
    meshQuality: "medium",
    enableAnimations: true,
  });

  const updateSetting = <K extends keyof VisualizationOptions>(
    key: K,
    value: VisualizationOptions[K]
  ) => {
    const newSettings = { ...settings, [key]: value };
    setSettings(newSettings);
    onSettingsChange(newSettings);
  };

  return (
    <div className="space-y-6">
      <div className="rounded-lg border border-border bg-card overflow-hidden shadow">
        <div className="bg-gradient-to-r from-purple-50 to-pink-50 dark:from-purple-900/20 dark:to-pink-900/20 px-4 py-3 border-b border-border">
          <h3 className="font-semibold text-gray-900 dark:text-white flex items-center gap-2">
            <Settings className="h-5 w-5 text-purple-600 dark:text-purple-400" />
            Visualization Settings
          </h3>
        </div>

        <div className="p-6 space-y-6">
          {/* 3D Settings */}
          <div>
            <h4 className="text-sm font-semibold text-gray-900 dark:text-white mb-3">3D View</h4>
            <div className="space-y-2">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.show3DWaterplane}
                  onChange={(e) => updateSetting("show3DWaterplane", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">Show Waterplane</span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.show3DCenters}
                  onChange={(e) => updateSetting("show3DCenters", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">
                  Show Centers (LCB, KB)
                </span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.show3DLabels}
                  onChange={(e) => updateSetting("show3DLabels", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">
                  Show BOW/STERN Labels
                </span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.show3DGrid}
                  onChange={(e) => updateSetting("show3DGrid", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">Show Grid</span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.show3DWaterlines}
                  onChange={(e) => updateSetting("show3DWaterlines", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">Waterlines Overlay</span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.show3DButtocks}
                  onChange={(e) => updateSetting("show3DButtocks", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">
                  Buttocks (Longitudinal Curves)
                </span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.show3DSections}
                  onChange={(e) => updateSetting("show3DSections", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">
                  Sections (Transverse Curves)
                </span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.show3DWireframe}
                  onChange={(e) => updateSetting("show3DWireframe", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">Wireframe Mode</span>
              </label>
            </div>
          </div>

          {/* 2D Settings */}
          <div className="pt-4 border-t border-border">
            <h4 className="text-sm font-semibold text-gray-900 dark:text-white mb-3">2D Views</h4>
            <div className="space-y-2">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.show2DWaterlines}
                  onChange={(e) => updateSetting("show2DWaterlines", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">Show Waterlines</span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.show2DButtocks}
                  onChange={(e) => updateSetting("show2DButtocks", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">Show Buttocks</span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.show2DSections}
                  onChange={(e) => updateSetting("show2DSections", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">Show Sections</span>
              </label>
            </div>
          </div>

          {/* Quality Settings */}
          <div className="pt-4 border-t border-border">
            <h4 className="text-sm font-semibold text-gray-900 dark:text-white mb-3">
              Detail Level
            </h4>
            <div className="space-y-3">
              <div>
                <Label className="text-xs text-gray-600 dark:text-gray-400">
                  Waterlines: {settings.waterlineCount}
                </Label>
                <input
                  type="range"
                  min="3"
                  max="15"
                  step="1"
                  value={settings.waterlineCount}
                  onChange={(e) => updateSetting("waterlineCount", parseInt(e.target.value))}
                  className="w-full h-2 rounded-lg appearance-none cursor-pointer bg-blue-200 dark:bg-blue-700"
                />
              </div>
              <div>
                <Label className="text-xs text-gray-600 dark:text-gray-400">
                  Buttocks: {settings.buttockCount}
                </Label>
                <input
                  type="range"
                  min="3"
                  max="9"
                  step="1"
                  value={settings.buttockCount}
                  onChange={(e) => updateSetting("buttockCount", parseInt(e.target.value))}
                  className="w-full h-2 rounded-lg appearance-none cursor-pointer bg-green-200 dark:bg-green-700"
                />
              </div>
              <div>
                <Label className="text-xs text-gray-600 dark:text-gray-400">
                  Sections: {settings.sectionCount}
                </Label>
                <input
                  type="range"
                  min="5"
                  max="21"
                  step="1"
                  value={settings.sectionCount}
                  onChange={(e) => updateSetting("sectionCount", parseInt(e.target.value))}
                  className="w-full h-2 rounded-lg appearance-none cursor-pointer bg-purple-200 dark:bg-purple-700"
                />
              </div>
            </div>
          </div>

          {/* Performance Settings */}
          <div className="pt-4 border-t border-border">
            <h4 className="text-sm font-semibold text-gray-900 dark:text-white mb-3">
              Performance
            </h4>
            <div className="space-y-3">
              <div>
                <Label className="text-xs text-gray-600 dark:text-gray-400 mb-2">
                  Mesh Quality
                </Label>
                <div className="grid grid-cols-4 gap-2">
                  {(["low", "medium", "high", "ultra"] as const).map((quality) => (
                    <button
                      key={quality}
                      onClick={() => updateSetting("meshQuality", quality)}
                      className={`px-3 py-2 text-xs font-medium rounded-lg transition-all ${
                        settings.meshQuality === quality
                          ? "bg-blue-600 text-white shadow"
                          : "bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600"
                      }`}
                    >
                      {quality.charAt(0).toUpperCase() + quality.slice(1)}
                    </button>
                  ))}
                </div>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-2 flex items-center gap-1">
                  {settings.meshQuality === "low" && (
                    <>
                      <Zap className="h-3 w-3" />
                      Faster (30×20 mesh)
                    </>
                  )}
                  {settings.meshQuality === "medium" && (
                    <>
                      <Scale className="h-3 w-3" />
                      Balanced (60×40 mesh)
                    </>
                  )}
                  {settings.meshQuality === "high" && (
                    <>
                      <Sparkles className="h-3 w-3" />
                      High Quality (120×80 mesh)
                    </>
                  )}
                  {settings.meshQuality === "ultra" && (
                    <>
                      <Sparkles className="h-3 w-3" />
                      Ultra (240×160 mesh)
                    </>
                  )}
                </p>
              </div>

              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.enableAnimations}
                  onChange={(e) => updateSetting("enableAnimations", e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">Enable Animations</span>
              </label>
            </div>
          </div>

          {/* Info */}
          <div className="pt-4 border-t border-border">
            <div className="rounded-lg bg-blue-50 dark:bg-blue-900/20 p-3 text-xs text-blue-800 dark:text-blue-400">
              <p className="font-medium mb-1 flex items-center gap-1">
                <Lightbulb className="h-3 w-3" />
                Performance Tips:
              </p>
              <ul className="space-y-1 ml-4">
                <li>• Lower mesh quality for faster rendering</li>
                <li>• Fewer curves for smoother animations</li>
                <li>• Disable grid if experiencing lag</li>
                <li>• Changes apply immediately</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
