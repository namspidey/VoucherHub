import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const violations = []
function walk(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const target = path.join(directory, entry.name)
    return entry.isDirectory() ? walk(target) : [target]
  })
}
for (const file of walk(path.join(root, 'backend/src'))) {
  if (!file.endsWith('.cs') || /[\\/](bin|obj)[\\/]/.test(file)) continue
  const text = fs.readFileSync(file, 'utf8')
  const relative = path.relative(root, file)
  if (relative.includes('VoucherHub.Application') && /using (Microsoft\.EntityFrameworkCore|Npgsql|VoucherHub\.Infrastructure|Microsoft\.AspNetCore)/.test(text)) violations.push(relative)
  if (relative.includes('VoucherHub.Domain') && /using (VoucherHub\.(Application|Infrastructure)|Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore|Npgsql)/.test(text)) violations.push(relative)
  if (relative.includes('VoucherHub.Api') && /using VoucherHub\.Infrastructure\.Persistence|VoucherHubDbContext/.test(text)) violations.push(relative)
}
for (const file of walk(path.join(root, 'frontend/src'))) {
  if (!/\.(js|jsx)$/.test(file)) continue
  const text = fs.readFileSync(file, 'utf8')
  for (const match of text.matchAll(/(?:from\s*|import\s*\()\s*['"]([^'"]+)['"]/g)) {
    if (!match[1].startsWith('.')) continue
    const target = path.resolve(path.dirname(file), match[1])
    const relative = path.relative(path.join(root, 'frontend/src'), file).replaceAll('\\', '/')
    const dependency = path.relative(path.join(root, 'frontend/src'), target).replaceAll('\\', '/')
    if (relative.startsWith('shared/') && /^(features|app)\//.test(dependency)) violations.push(`${relative} -> ${dependency}`)
    if (relative.startsWith('features/') && dependency.startsWith('app/')) violations.push(`${relative} -> ${dependency}`)
    if (relative.startsWith('features/') && /\/pages\//.test(dependency) && target !== file) violations.push(`${relative} -> page ${dependency}`)
  }
}
if (violations.length) {
  process.stderr.write(violations.join('\n') + '\n')
  process.exitCode = 1
} else console.log('PASS: BE layer boundaries and FE shared/feature/page import boundaries')
