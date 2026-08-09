<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import {
  listActive,
  listInactive,
  searchByPrefix,
  remove,
} from "../api/productsApi";
import type { Product } from "../models/product";
import { useAuthStore } from "../stores/auth";

const products = ref<Product[]>([]);
const loading = ref(false);
const searchPrefix = ref("");
const showInactive = ref(false);
const router = useRouter();
const auth = useAuthStore();

const headers = [
  { title: "SKU", key: "sku" },
  { title: "Name", key: "name" },
  { title: "Category", key: "category" },
  { title: "Price", key: "price" },
  { title: "Stock", key: "stock" },
  { title: "Active", key: "active" },
  { title: "Actions", key: "actions", sortable: false },
];

async function load() {
  loading.value = true;
  try {
    const { data } = searchPrefix.value
      ? await searchByPrefix(searchPrefix.value)
      : showInactive.value
        ? await listInactive()
        : await listActive();
    products.value = data.content;
  } finally {
    loading.value = false;
  }
}

function edit(id: number) {
  router.push(`/products/${id}/edit`);
}

async function del(id: number) {
  await remove(id);
  await load();
}

onMounted(load);
</script>

<template>
  <v-row class="mt-4" align="center">
    <v-col cols="8">
      <h1>Products</h1>
    </v-col>
    <v-col cols="4" class="text-right">
      <v-btn
        v-if="auth.isAdmin"
        color="primary"
        data-testid="new-product"
        @click="router.push('/products/new')"
      >
        New Product
      </v-btn>
    </v-col>
  </v-row>

  <v-text-field
    v-model="searchPrefix"
    label="Search by name prefix"
    data-testid="search"
    append-inner-icon="mdi-magnify"
    @keyup.enter="load"
    @click:append-inner="load"
  />

  <v-checkbox
    v-model="showInactive"
    label="Show inactive"
    data-testid="show-inactive"
    @update:model-value="load"
  />

  <v-data-table
    :headers="headers"
    :items="products"
    :loading="loading"
    data-testid="products-table"
  >
    <template #item.actions="{ item }">
      <v-btn
        v-if="auth.isAdmin"
        icon="mdi-pencil"
        variant="text"
        data-testid="edit-btn"
        @click="edit(item.id)"
      />
      <v-btn
        v-if="auth.isAdmin"
        icon="mdi-delete"
        variant="text"
        data-testid="delete-btn"
        @click="del(item.id)"
      />
    </template>
  </v-data-table>
</template>
