import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Phase 2 test app — pinned to 5190 (not Vite's default 5173, which was already occupied by an
// unrelated project on this machine during development, along with 5174). Must match
// backend/playground/APIPlatform.Playground/Program.cs's CORS policy origin.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5190,
    strictPort: true
  }
});
