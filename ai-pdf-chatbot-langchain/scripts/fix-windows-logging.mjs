// The installed LangGraph CLI formatter treats file: URLs as Windows paths.
// Apply the compatibility fix again after dependency installation when needed.
import fs from 'node:fs';
const target = new URL('../node_modules/@langchain/langgraph-cli/dist/logging.mjs', import.meta.url);
let source = fs.readFileSync(target, 'utf8');
if (!source.includes('import { fileURLToPath }')) {
  source = source.replace('import path from "node:path";', 'import path from "node:path";\nimport { fileURLToPath } from "node:url";');
  source = source.replace('const filePath = firstFile.file;', 'const filePath = firstFile.file.startsWith("file:") ? fileURLToPath(firstFile.file) : firstFile.file;\n        try { readFileSync(filePath); } catch { return stack; }');
  source = source.replace('i.includes(filePath)', 'i.includes(firstFile.file)');
  fs.writeFileSync(target, source);
}
