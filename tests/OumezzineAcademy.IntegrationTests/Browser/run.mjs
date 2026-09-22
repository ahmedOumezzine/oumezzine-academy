import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const cwd = fileURLToPath(new URL('.', import.meta.url));
const fixture = spawnSync('dotnet', ['test', '../OumezzineAcademy.Tests.csproj', '-c', 'Release', '--no-restore', '--filter', 'FullyQualifiedName~LessonEditorHttpTests'], {
    cwd, stdio: 'inherit', env: { ...process.env, LEARN_EDITOR_BROWSER_FIXTURES: resolve(cwd, 'obj/fixtures') }
});
if (fixture.status !== 0) process.exit(fixture.status ?? 1);
const result = spawnSync(process.execPath, ['node_modules/@playwright/test/cli.js', 'test'], { cwd, stdio: 'inherit' });
process.exit(result.status ?? 1);
