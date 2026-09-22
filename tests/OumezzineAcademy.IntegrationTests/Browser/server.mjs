import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { resolve, extname, sep } from 'node:path';
const root = resolve('../../../src/OumezzineAcademy.Web/wwwroot');
export const server = createServer(async (req, res) => {
    const path = new URL(req.url, 'http://localhost').pathname;
    const fixture = ['/create', '/edit', '/validation'].includes(path);
    const file = fixture ? resolve(`obj/fixtures${path}.html`) : resolve(root, `.${path}`);
    if (!fixture && !file.startsWith(root + sep)) { res.writeHead(403).end(); return; }
    try {
        res.setHeader('Content-Security-Policy', "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'");
        res.setHeader('Content-Type', ({ '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.css': 'text/css' })[extname(file)] || 'application/octet-stream');
        res.end(await readFile(file));
    } catch { res.writeHead(404).end(); }
});
