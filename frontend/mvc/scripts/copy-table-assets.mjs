import { copyFileSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../', import.meta.url));
const target = join(root, 'Viw.Mvc', 'wwwroot', 'lib', 'datatables');
mkdirSync(target, { recursive: true });
for (const [source, name] of [
    ['datatables.net/js/dataTables.min.js', 'dataTables.min.js'],
    ['datatables.net-bs5/js/dataTables.bootstrap5.min.js', 'dataTables.bootstrap5.min.js'],
    ['datatables.net-bs5/css/dataTables.bootstrap5.min.css', 'dataTables.bootstrap5.min.css'],
    ['datatables.net/License.txt', 'LICENSE.txt']
]) {
    copyFileSync(join(root, 'node_modules', source), join(target, name));
}
const options = JSON.parse(readFileSync(join(root, '..', 'shared', 'table-options.json'), 'utf8'));
writeFileSync(join(target, 'viw-options.js'), `globalThis.viwTableOptions = ${JSON.stringify(options)};\n`);
copyFileSync(join(root, '..', 'shared', 'table-theme.css'), join(target, 'viw-theme.css'));
