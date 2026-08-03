// Guards against snippet rot.
//
// Every `<<< path#region` import in every deck is checked: the file has to
// exist, and the named region has to be in it. A region renamed or deleted in
// the C# is a hard failure here rather than a silently empty code block on a
// projector in front of a room.
//
//   node verify-snippets.mjs

import { readdirSync, readFileSync, existsSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, resolve, relative } from 'node:path'

const here = dirname(fileURLToPath(import.meta.url))
const repoRoot = resolve(here, '..')

// Matches Slidev's own snippet syntax: <<< path#region lang {meta}
const SNIPPET = /^<<<[ \t]*(\S+?)(#[\w-]+)?(?:[ \t]+\S+)?[ \t]*(?:\{.*)?$/

const decks = readdirSync(here).filter(f => /^\d\d-.*\.md$/.test(f)).sort()

let checked = 0
const failures = []

for (const deck of decks) {
  const lines = readFileSync(resolve(here, deck), 'utf8').split(/\r?\n/)

  lines.forEach((line, i) => {
    const match = line.match(SNIPPET)
    if (!match) return

    const [, filepath, hash] = match
    const at = `${deck}:${i + 1}`
    const src = filepath.startsWith('@/')
      ? resolve(here, filepath.slice(2))
      : resolve(here, filepath)

    checked++

    if (!existsSync(src)) {
      failures.push(`${at}  missing file: ${relative(repoRoot, src)}`)
      return
    }

    if (!hash) return

    const region = hash.slice(1)
    const content = readFileSync(src, 'utf8')

    // C# `#region name`, plus the comment-prefixed forms other languages use.
    const opens = new RegExp(`^\\s*(?://\\s*|/\\*\\s*|#\\s*)?#region\\s+${region}\\s*$`, 'm')

    if (!opens.test(content)) {
      failures.push(`${at}  no #region ${region} in ${relative(repoRoot, src)}`)
    }
  })
}

if (failures.length) {
  console.error(`\n${failures.length} broken snippet import(s):\n`)
  for (const f of failures) console.error(`  ${f}`)
  console.error('')
  process.exit(1)
}

console.log(`All ${checked} snippet imports across ${decks.length} decks resolve.`)
