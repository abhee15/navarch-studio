import GIF from "gif.js";

export interface GIFCaptureOptions {
  width?: number;
  height?: number;
  quality?: number;
  workers?: number;
  workerScript?: string;
}

/**
 * Captures frames from a canvas and creates an animated GIF
 */
export class GIFCapture {
  private gif: GIF | null = null;
  private isRecording = false;
  private frameCount = 0;
  private readonly defaultOptions: Required<GIFCaptureOptions> = {
    width: 800,
    height: 600,
    quality: 10,
    workers: 2,
    workerScript: "/gif.worker.js",
  };

  constructor(private options: GIFCaptureOptions = {}) {
    this.options = { ...this.defaultOptions, ...options };
  }

  /**
   * Start recording frames
   */
  start(): void {
    if (this.isRecording) {
      console.warn("GIF capture already in progress");
      return;
    }

    this.gif = new GIF({
      workers: this.options.workers,
      quality: this.options.quality,
      workerScript: this.options.workerScript,
      width: this.options.width,
      height: this.options.height,
    });

    this.isRecording = true;
    this.frameCount = 0;
  }

  /**
   * Add a frame from a canvas element
   * @param canvas The canvas element to capture
   * @param delay Frame delay in milliseconds (default: 100ms = 10fps)
   */
  addFrame(canvas: HTMLCanvasElement, delay: number = 100): void {
    if (!this.isRecording || !this.gif) {
      console.warn("GIF capture not started");
      return;
    }

    // Create a temporary canvas with the specified dimensions
    const tempCanvas = document.createElement("canvas");
    tempCanvas.width = this.options.width!;
    tempCanvas.height = this.options.height!;
    const ctx = tempCanvas.getContext("2d");

    if (ctx) {
      // Draw the source canvas onto the temp canvas (scaled if needed)
      ctx.drawImage(canvas, 0, 0, tempCanvas.width, tempCanvas.height);
      this.gif.addFrame(ctx, { copy: true, delay });
      this.frameCount++;
    }
  }

  /**
   * Stop recording and generate the GIF
   * @returns Promise that resolves with the GIF blob
   */
  async stop(): Promise<Blob> {
    return new Promise((resolve, reject) => {
      if (!this.isRecording || !this.gif) {
        reject(new Error("GIF capture not started"));
        return;
      }

      this.gif.on("finished", (blob: Blob) => {
        this.isRecording = false;
        this.gif = null;
        resolve(blob);
      });

      // Handle errors - gif.js doesn't have typed error event
      // @ts-expect-error - gif.js error event not in types
      this.gif.on("error", (error: Error) => {
        this.isRecording = false;
        this.gif = null;
        reject(error);
      });

      this.gif.render();
    });
  }

  /**
   * Cancel recording without generating the GIF
   */
  cancel(): void {
    if (this.gif) {
      this.gif.abort();
      this.gif = null;
    }
    this.isRecording = false;
    this.frameCount = 0;
  }

  /**
   * Check if recording is in progress
   */
  getIsRecording(): boolean {
    return this.isRecording;
  }

  /**
   * Get the number of frames captured
   */
  getFrameCount(): number {
    return this.frameCount;
  }
}

/**
 * Download a blob as a file
 */
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

/**
 * Capture a single frame from a canvas as a data URL
 */
export function captureFrame(canvas: HTMLCanvasElement): string {
  return canvas.toDataURL("image/png");
}
