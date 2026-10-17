<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import InputNumber from 'primevue/inputnumber'
import Select from 'primevue/select'
import type { Ingredient, Product } from '../api/products'

const props = defineProps<{ products: Product[] }>()
const model = defineModel<Ingredient[]>({ required: true })

const unitOf = computed(() => new Map(props.products.map((p) => [p.id, p.unit])))

// A product can appear in a recipe only once, so each row only offers products not used by the other rows.
function optionsFor(row: Ingredient) {
  const used = new Set(model.value.filter((r) => r !== row).map((r) => r.productId))
  return props.products.filter((p) => !used.has(p.id))
}

const canAdd = computed(() => model.value.length < props.products.length)

function add() {
  const used = new Set(model.value.map((r) => r.productId))
  const next = props.products.find((p) => !used.has(p.id))
  if (next) model.value = [...model.value, { productId: next.id, quantity: 1 }]
}

function remove(index: number) {
  model.value = model.value.filter((_, i) => i !== index)
}
</script>

<template>
  <div class="editor">
    <div v-for="(row, index) in model" :key="index" class="row">
      <Select
        v-model="row.productId"
        :options="optionsFor(row)"
        option-label="name"
        option-value="id"
        class="product"
        aria-label="Продукт"
      />
      <InputNumber v-model="row.quantity" :min="1" :max="1000000" :use-grouping="false" class="quantity" aria-label="Количество" />
      <span class="unit">{{ unitOf.get(row.productId) }}</span>
      <Button icon="pi pi-times" severity="secondary" text rounded aria-label="Убрать" @click="remove(index)" />
    </div>
    <div v-if="!model.length" class="muted">Ингредиенты не заданы — со склада ничего не списывается</div>
    <Button
      type="button"
      label="Добавить ингредиент"
      icon="pi pi-plus"
      severity="secondary"
      size="small"
      outlined
      :disabled="!canAdd"
      @click="add"
    />
  </div>
</template>

<style scoped>
.editor {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  align-items: flex-start;
}

.row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
}

.product {
  flex: 1;
  min-width: 0;
}

.quantity {
  width: 6rem;
}

.quantity :deep(input) {
  width: 100%;
}

.unit {
  width: 2.5rem;
  color: var(--p-text-muted-color);
}

.muted {
  color: var(--p-text-muted-color);
  font-size: 0.875rem;
}
</style>
