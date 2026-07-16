<script setup lang="ts">
import { ref, onMounted } from "vue";
import type { VForm } from "vuetify/components";
import { useRoute, useRouter } from "vue-router";
import { getById, create, update } from "../api/productsApi";

const route = useRoute();
const router = useRouter();

const id = route.params.id ? Number(route.params.id) : null;
const isEdit = id !== null;

const sku = ref("");
const name = ref("");
const description = ref("");
const category = ref("");
const price = ref(0);
const stock = ref(0);
const active = ref(true);
const loading = ref(false);
const form = ref<VForm | null>(null);

const SKU_PATTERN = /^[A-Za-z0-9_-]{1,50}$/;
const TEXT_PATTERN = /^[\p{L}\p{N}\s.,:;_\-()/]{1,255}$/u;

const skuRules = [
  (v: string) => (!!v && !!v.trim()) || "El SKU es requerido",
  (v: string) =>
    SKU_PATTERN.test(v) ||
    "Solo letras, números, guiones y guiones bajos (máx. 50)",
];
const nameRules = [
  (v: string) => (!!v && !!v.trim()) || "El nombre es requerido",
  (v: string) =>
    TEXT_PATTERN.test(v) || "Formato de nombre inválido (máx. 255 caracteres)",
];
const descriptionRules = [
  (v: string) =>
    !v ||
    !v.trim() ||
    TEXT_PATTERN.test(v) ||
    "Formato de descripción inválido (máx. 255 caracteres)",
];
const categoryRules = [
  (v: string) => (!!v && !!v.trim()) || "La categoría es requerida",
  (v: string) =>
    TEXT_PATTERN.test(v) ||
    "Formato de categoría inválido (máx. 255 caracteres)",
];
const priceRules = [
  (v: number) => v != null || "El precio es requerido",
  (v: number) => v >= 0 || "El precio debe ser mayor o igual a 0",
];
const stockRules = [
  (v: number) => v != null || "El stock es requerido",
  (v: number) =>
    (Number.isInteger(v) && v >= 0) ||
    "El stock debe ser un entero mayor o igual a 0",
];

onMounted(async () => {
  if (isEdit) {
    loading.value = true;
    const { data } = await getById(id!);
    sku.value = data.sku;
    name.value = data.name;
    description.value = data.description ?? "";
    category.value = data.category ?? "";
    price.value = data.price;
    stock.value = data.stock;
    active.value = data.active;
    loading.value = false;
  }
});

async function submit() {
  const { valid } = await form.value!.validate();
  if (!valid) return;

  const payload = {
    sku: sku.value,
    name: name.value,
    description: description.value || null,
    category: category.value || null,
    price: price.value,
    stock: stock.value,
    active: active.value,
  };
  if (isEdit) {
    await update(id!, payload);
  } else {
    await create(payload);
  }
  router.push("/products");
}
</script>

<template>
  <h1>{{ isEdit ? "Edit Product" : "New Product" }}</h1>
  <v-form ref="form" @submit.prevent="submit">
    <v-text-field
      v-model="sku"
      label="SKU"
      data-testid="sku"
      :disabled="isEdit"
      :rules="skuRules"
    />
    <v-text-field
      v-model="name"
      label="Name"
      data-testid="name"
      :rules="nameRules"
    />
    <v-textarea
      v-model="description"
      label="Description"
      :rules="descriptionRules"
    />
    <v-text-field
      v-model="category"
      label="Category"
      data-testid="category"
      :rules="categoryRules"
    />
    <v-text-field
      v-model.number="price"
      label="Price"
      type="number"
      data-testid="price"
      :rules="priceRules"
    />
    <v-text-field
      v-model.number="stock"
      label="Stock"
      type="number"
      data-testid="stock"
      :rules="stockRules"
    />
    <v-checkbox v-model="active" label="Active" data-testid="active" />
    <v-btn type="submit" color="primary" data-testid="submit">
      {{ isEdit ? "Update" : "Create" }}
    </v-btn>
    <v-btn variant="text" @click="router.push('/products')"> Cancel </v-btn>
  </v-form>
</template>
