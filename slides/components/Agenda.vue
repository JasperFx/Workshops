<!--
  The workshop agenda, with the current section highlighted.
  Usage:  <Agenda :current="1" />
-->
<script setup lang="ts">
defineProps<{ current?: number }>()

const sections = [
  'Introduction to CQRS and Event Sourcing',
  'Asynchronous Messaging',
  'Resiliency & Concurrency',
  'Data Consistency',
  'Modular Monolith',
  'Multi-Tenancy',
  'Observability, Metadata, and Metrics',
  'Testing & DevOps',
]
</script>

<template>
  <ol class="agenda">
    <li
      v-for="(s, i) in sections"
      :key="s"
      :class="{ active: current === i + 1, done: current != null && current > i + 1 }"
    >
      <span class="num">{{ i + 1 }}</span>
      <span class="title">{{ s }}</span>
    </li>
  </ol>
</template>

<style scoped>
.agenda {
  list-style: none;
  padding: 0;
  margin: 0;
}

.agenda li {
  display: flex;
  align-items: baseline;
  gap: 0.75rem;
  padding: 0.3rem 0.6rem;
  border-radius: 5px;
  opacity: 0.55;
}

.agenda li.done {
  opacity: 0.3;
}

.agenda li.active {
  opacity: 1;
  background: rgb(184 86 47 / 12%);
  font-weight: 600;
}

.num {
  font-variant-numeric: tabular-nums;
  color: var(--cs-accent);
  font-weight: 700;
  min-width: 1.2rem;
}
</style>
