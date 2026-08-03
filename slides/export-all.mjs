// Exports every deck to PDF in dist-pdf/. Requires playwright-chromium.
import { readdirSync, mkdirSync } from 'node:fs'
import { execFileSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const here = dirname(fileURLToPath(import.meta.url))
mkdirSync(join(here, 'dist-pdf'), { recursive: true })

const decks = readdirSync(here)
  .filter(f => /^\d\d-.*\.md$/.test(f))
  .sort()

for (const deck of decks) {
  const name = deck.replace(/\.md$/, '')
  console.log(`\n=== exporting ${name} ===`)
  execFileSync(
    'npx',
    ['slidev', 'export', deck, '--with-clicks', '--output', join('dist-pdf', `${name}.pdf`)],
    { cwd: here, stdio: 'inherit' },
  )
}
