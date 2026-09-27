import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

const target =
  process.env['services__webapi__https__0'] ||
  process.env['services__webapi__http__0'] ||
  'http://localhost:5270';

const proxyOptions = target
  ? { target, secure: false, changeOrigin: true }
  : undefined;

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: parseInt(process.env.PORT!),
    proxy: proxyOptions
      ? {
          '/api': proxyOptions,
          '/openapi': proxyOptions,
          '/scalar': proxyOptions,
          '/weatherforecast': proxyOptions,
          '/WeatherForecast': proxyOptions,
        }
      : undefined,
  },
  build: {
    outDir: 'build',
  },
  test: {
    environment: 'jsdom',
  },
});
