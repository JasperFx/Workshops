// Exports decks to PDF in dist-pdf/ as a fallback for a projector that won't
// cooperate. Deliberately not run as part of the normal build -- it drives a
// headless browser and takes a minute or so per deck.
//
//   npm run export --workspace slides              all eight decks
//   npm run export --workspace slides -- 1 2       just sections 1 and 2
//
// First run needs the browser:
//
//   npx playwright install chromium

import { readdirSync, mkdirSync, existsSync } from 'node:fs'
import { execFileSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const here = dirname(fileURLToPath(import.meta.url))
const outDir = join(here, 'dist-pdf')

// Section numbers can be passed through as arguments; no arguments means all.
const wanted = process.argv.slice(2).map(a => a.padStart(2, '0'))

const decks = readdirSync(here)
  .filter(f => /^\d\d-.*\.md$/.test(f))
  .filter(f => wanted.length === 0 || wanted.includes(f.slice(0, 2)))
  .sort()

if (decks.length === 0) {
  console.error(`No decks matched ${process.argv.slice(2).join(', ')}.`)
  console.error('Pass section numbers like "1 2", or nothing to export all.')
  process.exit(1)
}

mkdirSync(outDir, { recursive: true })

// Fail early with a useful message rather than a stack trace from deep inside
// Slidev when the browser was never installed.
const browsers = join(here, '..', 'node_modules', 'playwright-chromium')
if (!existsSync(browsers)) {
  console.error('playwright-chromium is not installed. Run: npm install')
  process.exit(1)
}

console.log(`Exporting ${decks.length} deck(s) to dist-pdf/\n`)

const failed = []

for (const deck of decks) {
  const name = deck.replace(/\.md$/, '')
  console.log(`=== ${name} ===`)

  try {
    execFileSync(
      'npx',
      [
        'slidev', 'export', deck,
        // Each click-step becomes its own page, so incremental builds survive.
        '--with-clicks',
        '--with-toc',
        '--output', join(outDir, `${name}.pdf`),
      ],
      { cwd: here, stdio: 'inherit' },
    )
  } catch {
    failed.push(name)
    console.error(`  FAILED: ${name}\n`)
  }
}

if (failed.length) {
  console.error(`\n${failed.length} deck(s) failed: ${failed.join(', ')}`)
  console.error('If this is the first run, try: npx playwright install chromium')
  process.exit(1)
}

console.log(`\nDone. ${decks.length} PDF(s) in slides/dist-pdf/`)
