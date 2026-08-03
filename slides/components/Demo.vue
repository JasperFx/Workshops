<!--
  Banner for "put the slides down and go to the IDE" moments.
  Usage:  <Demo path="src/HelpDesk/HelpDesk.Api" run="dotnet run" />
-->
<script setup lang="ts">
defineProps<{ path?: string; run?: string }>()
</script>

<template>
  <div class="demo">
    <div class="tag">DEMO</div>
    <div class="body">
      <slot />
      <div v-if="path" class="path"><code>{{ path }}</code></div>
      <div v-if="run" class="run"><code>{{ run }}</code></div>
    </div>
  </div>
</template>

<style scoped>
.demo {
  display: flex;
  gap: 1rem;
  align-items: flex-start;
  border: 2px solid var(--cs-accent);
  border-radius: 8px;
  padding: 1rem 1.2rem;
}

.tag {
  font-weight: 800;
  letter-spacing: 0.12em;
  color: var(--cs-accent);
  font-size: 0.8rem;
  padding-top: 0.15rem;
}

.path,
.run {
  margin-top: 0.5rem;
  font-size: 0.85em;
  color: var(--cs-muted);
}
</style>
