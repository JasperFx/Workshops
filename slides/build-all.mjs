// Builds every deck into dist/<deck-name>/ so the whole workshop can be
// published as one static site.
import { readdirSync } from 'node:fs'
import { execFileSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const here = dirname(fileURLToPath(import.meta.url))

const decks = readdirSync(here)
  .filter(f => /^\d\d-.*\.md$/.test(f))
  .sort()

for (const deck of decks) {
  const name = deck.replace(/\.md$/, '')
  console.log(`\n=== building ${name} ===`)
  execFileSync(
    'npx',
    ['slidev', 'build', deck, '--base', `/${name}/`, '--out', join('dist', name)],
    { cwd: here, stdio: 'inherit' },
  )
}

console.log(`\nBuilt ${decks.length} decks into dist/`)
