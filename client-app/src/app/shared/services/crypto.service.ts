import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CryptoService {
  private readonly http = inject(HttpClient);
  private cryptoKey: CryptoKey | null = null;
  private readonly base = environment.bffBaseUrl;

  /** Fetch the symmetric key from the BFF and import it for AES-GCM encryption. */
  async init(): Promise<void> {
    const response = await firstValueFrom(
      this.http.get<{ key: string }>(`${this.base}/api/auth/public-key`),
    );
    const raw = Uint8Array.from(atob(response.key), (c) => c.charCodeAt(0));
    this.cryptoKey = await crypto.subtle.importKey(
      'raw',
      raw,
      { name: 'AES-GCM', length: 256 },
      false,
      ['encrypt'],
    );
  }

  /**
   * AES-256-GCM encrypt `plaintext`.
   * Returns a string in the format `base64(iv):base64(ciphertext_with_tag)`.
   * The BFF decrypts this before forwarding to Order.API — the raw token
   * never travels over the external network.
   */
  async encrypt(plaintext: string): Promise<string> {
    if (!this.cryptoKey) await this.init();
    const iv = crypto.getRandomValues(new Uint8Array(12));
    const encoded = new TextEncoder().encode(plaintext);
    const ciphertextBuf = await crypto.subtle.encrypt(
      { name: 'AES-GCM', iv, tagLength: 128 },
      this.cryptoKey!,
      encoded,
    );
    const ivB64 = btoa(String.fromCharCode(...iv));
    const ctB64 = btoa(String.fromCharCode(...new Uint8Array(ciphertextBuf)));
    return `${ivB64}:${ctB64}`;
  }
}
