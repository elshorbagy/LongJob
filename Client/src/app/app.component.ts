import { Component } from '@angular/core';
import { ApiService } from './api.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrls: ['./app.css'],
  standalone: false
})
export class AppComponent {
  input = '';
  result = '';
  busy = false;
  progress = 0;
  jobId: string | null = null;
  private disposeStream: (() => void) | null = null;

  constructor(private api: ApiService) { }

  onProcess() {
    if (!this.input.trim() || this.busy) return;
    this.result = ''; this.progress = 0; this.busy = true;

    this.api.start(this.input).subscribe({
      next: ({ jobId }) => {
        this.jobId = jobId;
        const expected = this.estimateOutputLength(this.input);
        this.disposeStream = this.api.stream(
          jobId,
          (c) => {
            this.result += c;
            if (expected > 0) {
              this.progress = Math.min(100, Math.round((this.result.length / expected) * 100));
            }
          },
          () => this.finish(),
          () => this.finish()
        );
      },
      error: () => this.finish()
    });
  }

  onCancel() {
    if (!this.jobId) return;
    this.api.cancel(this.jobId).subscribe({ complete: () => this.finish() });
  }

  private finish() {
    this.busy = false; this.progress = 100;
    if (this.disposeStream) { this.disposeStream(); this.disposeStream = null; }
    this.jobId = null;
  }

  private estimateOutputLength(input: string) {
    const m: Record<string, number> = {};
    for (const ch of input) m[ch] = (m[ch] ?? 0) + 1;
    const left = Object.keys(m).sort().map(k => `${k}${m[k]}`).join('').length;
    const right = btoa(unescape(encodeURIComponent(input))).length;
    return left + 1 + right;
  }
}
