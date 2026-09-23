import { fileURLToPath, URL } from 'node:url';

import { defineConfig, loadEnv } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';

/**
 * Resuelve el certificado HTTPS de desarrollo de ASP.NET (dotnet dev-certs).
 * Si no es posible generarlo (p. ej. sin SDK de .NET), el servidor de Vite
 * arranca en HTTP para no bloquear el desarrollo del frontend.
 */
function resolveHttpsConfig(useHttps) {
    if (!useHttps) return undefined;

    const baseFolder =
        process.env.APPDATA !== undefined && process.env.APPDATA !== ''
            ? `${process.env.APPDATA}/ASP.NET/https`
            : `${process.env.HOME}/.aspnet/https`;

    const certificateArg = process.argv.map(arg => arg.match(/--name=(?<value>.+)/i)).filter(Boolean)[0];
    const certificateName = certificateArg ? certificateArg.groups.value : 'LexisCampusDMS.client';

    const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
    const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        const result = child_process.spawnSync('dotnet', [
            'dev-certs',
            'https',
            '--export-path',
            certFilePath,
            '--format',
            'Pem',
            '--no-password',
        ], { stdio: 'inherit' });

        if (result.status !== 0) {
            console.warn('[vite] No se pudo generar el certificado HTTPS de desarrollo. Se usará HTTP.');
            return undefined;
        }
    }

    return {
        key: fs.readFileSync(keyFilePath),
        cert: fs.readFileSync(certFilePath),
    };
}

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), '');

    // Destino del backend ASP.NET Core. Por defecto el perfil "https" (puerto 7081) de launchSettings.json.
    // Visual Studio inyecta ASPNETCORE_HTTPS_PORT cuando se ejecuta con el perfil HTTPS.
    const apiTarget = env.VITE_API_TARGET
        || (env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` : 'https://localhost:7081');

    return {
        plugins: [plugin()],
        resolve: {
            alias: {
                '@': fileURLToPath(new URL('./src', import.meta.url))
            }
        },
        server: {
            port: 5173,
            https: resolveHttpsConfig(env.VITE_DEV_HTTPS !== 'false'),
            proxy: {
                '^/api': {
                    target: apiTarget,
                    secure: false,
                    changeOrigin: true
                }
            }
        }
    };
});
