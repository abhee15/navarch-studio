/**
 * Utility functions for exporting viewport views to SVG and PNG
 */

/**
 * Export an SVG element to a downloadable SVG file
 */
export const exportSVG = (svgElement: SVGSVGElement, filename: string): void => {
  const svgData = new XMLSerializer().serializeToString(svgElement);
  const svgBlob = new Blob([svgData], { type: "image/svg+xml;charset=utf-8" });
  const svgUrl = URL.createObjectURL(svgBlob);
  const downloadLink = document.createElement("a");
  downloadLink.href = svgUrl;
  downloadLink.download = `${filename}.svg`;
  document.body.appendChild(downloadLink);
  downloadLink.click();
  document.body.removeChild(downloadLink);
  URL.revokeObjectURL(svgUrl);
};

/**
 * Export an SVG element to a PNG file
 */
export const exportSVGToPNG = (
  svgElement: SVGSVGElement,
  filename: string,
  scale: number = 2
): Promise<void> => {
  return new Promise((resolve, reject) => {
    const svgData = new XMLSerializer().serializeToString(svgElement);
    const canvas = document.createElement("canvas");
    const ctx = canvas.getContext("2d");

    if (!ctx) {
      reject(new Error("Could not get canvas context"));
      return;
    }

    const img = new Image();
    const svgBlob = new Blob([svgData], { type: "image/svg+xml;charset=utf-8" });
    const url = URL.createObjectURL(svgBlob);

    img.onload = () => {
      canvas.width = img.width * scale;
      canvas.height = img.height * scale;
      ctx.scale(scale, scale);
      ctx.drawImage(img, 0, 0);
      URL.revokeObjectURL(url);

      canvas.toBlob((blob) => {
        if (!blob) {
          reject(new Error("Could not create blob"));
          return;
        }
        const pngUrl = URL.createObjectURL(blob);
        const downloadLink = document.createElement("a");
        downloadLink.href = pngUrl;
        downloadLink.download = `${filename}.png`;
        document.body.appendChild(downloadLink);
        downloadLink.click();
        document.body.removeChild(downloadLink);
        URL.revokeObjectURL(pngUrl);
        resolve();
      });
    };

    img.onerror = () => {
      URL.revokeObjectURL(url);
      reject(new Error("Failed to load SVG"));
    };

    img.src = url;
  });
};

/**
 * Export a Three.js canvas to PNG
 */
export const exportCanvasToPNG = (canvas: HTMLCanvasElement, filename: string): void => {
  canvas.toBlob((blob) => {
    if (!blob) return;
    const url = URL.createObjectURL(blob);
    const downloadLink = document.createElement("a");
    downloadLink.href = url;
    downloadLink.download = `${filename}.png`;
    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
    URL.revokeObjectURL(url);
  });
};

/**
 * Generate a safe filename from candidate data
 */
export const generateFilename = (
  candidateId: string,
  viewType: "plan" | "profile" | "sections" | "3d",
  hullFamily?: string
): string => {
  const timestamp = new Date().toISOString().split("T")[0];
  const family = hullFamily ? `_${hullFamily}` : "";
  return `hull_${viewType}${family}_${candidateId.slice(0, 8)}_${timestamp}`;
};

