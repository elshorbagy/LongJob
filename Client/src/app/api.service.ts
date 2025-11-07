// src/app/api.service.ts
import { Injectable, NgZone } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = 'https://localhost:7073/api'; //==> chech later

  constructor(private http: HttpClient, private zone: NgZone) {}

  start(text: string) {
    return this.http.post<{ jobId: string }>(`${this.base}/jobs`, { text });
  }

  stream(jobId: string, onChar: (c: string) => void, onDone: () => void, onError: () => void) {
    const es = new EventSource(`${this.base}/jobs/${jobId}/stream`);
    es.onmessage = (e) => {
      this.zone.run(() => onChar(e.data));
    };
    es.onerror = () => {
      this.zone.run(() => {
        es.close();
        onError();
      });
    };
    es.onopen = () => { };

    let idleTimer: any;
    const reset = () => {
      clearTimeout(idleTimer);
      idleTimer = setTimeout(() => { es.close(); this.zone.run(onDone); }, 7000);
    };
    es.onmessage = (e) => { this.zone.run(() => onChar(e.data)); reset(); };
    reset();
    return () => es.close();
  }

  cancel(jobId: string) {
    return this.http.delete(`${this.base}/jobs/${jobId}`, { responseType: 'text' });
  }
}
