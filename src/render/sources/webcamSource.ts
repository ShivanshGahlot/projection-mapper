// Webcam content: a getUserMedia stream shown through a video element.

import type { Content, WebcamContent } from '../../state/types';
import type { LayerStatus } from '../api';
import { STATUS_LOADING, STATUS_OK, errorStatus, newOutput, setFullOutput, type ContentSource, type FrameInfo, type SizeRequest, type SourceEnv, type SourceOutput } from '../source';
import { VideoFeed } from './videoFeed';

export class WebcamSource implements ContentSource {
  readonly generated = false;
  mirrorX: boolean;
  private deviceId: string | null;
  private readonly video = document.createElement('video');
  private readonly feed: VideoFeed;
  private stream: MediaStream | null = null;
  private token = 0;
  private st: LayerStatus = STATUS_LOADING;
  private readonly out = newOutput();

  constructor(env: SourceEnv, content: WebcamContent) {
    this.mirrorX = content.mirror;
    this.deviceId = content.deviceId;
    this.video.muted = true;
    this.video.playsInline = true;
    this.feed = new VideoFeed(env.gl, this.video);
    this.start();
  }

  private start(): void {
    const token = ++this.token;
    this.stopStream();
    this.st = STATUS_LOADING;
    const md = navigator.mediaDevices;
    if (!md?.getUserMedia) {
      this.st = { state: 'error', message: 'Camera access is not available in this browser.' };
      return;
    }
    const video: MediaTrackConstraints = this.deviceId ? { deviceId: { exact: this.deviceId } } : {};
    md.getUserMedia({ video, audio: false }).then(
      (stream) => {
        if (token !== this.token) {
          stream.getTracks().forEach((t) => t.stop());
          return;
        }
        this.stream = stream;
        this.video.srcObject = stream;
        this.video.play().catch(() => {});
      },
      (e) => {
        if (token === this.token) this.st = errorStatus(e);
      },
    );
  }

  private stopStream(): void {
    this.stream?.getTracks().forEach((t) => t.stop());
    this.stream = null;
    this.video.srcObject = null;
  }

  update(content: Content): boolean {
    if (content.type !== 'webcam') return false;
    this.mirrorX = content.mirror;
    if (content.deviceId !== this.deviceId) {
      this.deviceId = content.deviceId;
      this.start();
    }
    return true;
  }

  tick(_frame: FrameInfo): void {}

  render(_size: SizeRequest): SourceOutput | null {
    if (!this.feed.update()) return null;
    if (this.st.state === 'loading') this.st = STATUS_OK;
    return setFullOutput(this.out, this.feed.texture, true);
  }

  nativeSize(): { width: number; height: number } | null {
    return this.feed.hasFrame ? { width: this.feed.width, height: this.feed.height } : null;
  }

  status(): LayerStatus {
    return this.st;
  }

  dispose(): void {
    this.token++;
    this.stopStream();
    this.feed.dispose();
  }
}
