<script setup lang="ts">
import { useAuthStore } from "./stores/auth";
import { useRouter } from "vue-router";

const auth = useAuthStore();
const router = useRouter();

function logout() {
  auth.logout();
  router.push("/login");
}
</script>

<template>
  <v-app>
    <v-app-bar v-if="auth.isAuthenticated" color="primary">
      <v-app-bar-title>Product Manager</v-app-bar-title>
      <v-spacer />
      <span class="mr-4">{{ auth.username }}</span>
      <v-btn variant="text" @click="logout"> Logout </v-btn>
    </v-app-bar>
    <v-main>
      <v-container>
        <router-view />
      </v-container>
    </v-main>
  </v-app>
</template>
