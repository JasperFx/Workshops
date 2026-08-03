<!-- Footer rendered on every slide of every deck. -->
<script setup lang="ts">
import { useSlideContext } from '@slidev/client'

const { $slidev, $page } = useSlideContext()
</script>

<template>
  <footer v-if="$page > 1" class="workshop-footer">
    <span>{{ $slidev.configs.title }}</span>
    <span class="sep">·</span>
    <span>Critter Stack Workshop</span>
  </footer>
</template>

<style scoped>
.workshop-footer {
  position: absolute;
  left: 1.5rem;
  bottom: 0.7rem;
  font-size: 0.62rem;
  letter-spacing: 0.04em;
  color: var(--cs-muted);
  opacity: 0.7;
  pointer-events: none;
}

.sep {
  margin: 0 0.4rem;
}
</style>
