<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { login } from "../api/authApi";
import { useAuthStore } from "../stores/auth";

const username = ref("");
const password = ref("");
const error = ref("");
const loading = ref(false);

const auth = useAuthStore();
const router = useRouter();

async function submit() {
  loading.value = true;
  error.value = "";
  try {
    const { data } = await login(username.value, password.value);
    auth.setSession(data.token, data.username, data.roles);
    router.push("/products");
  } catch {
    error.value = "Invalid username or password";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <v-row justify="center" class="mt-16">
    <v-col cols="12" sm="6" md="4">
      <v-card>
        <v-card-title>Product Manager Login</v-card-title>
        <v-card-text>
          <v-form @submit.prevent="submit">
            <v-text-field
              v-model="username"
              label="Username"
              data-testid="username"
            />
            <v-text-field
              v-model="password"
              label="Password"
              type="password"
              data-testid="password"
            />
            <v-alert v-if="error" type="error" density="compact" class="mb-4">
              {{ error }}
            </v-alert>
            <v-btn
              type="submit"
              color="primary"
              block
              :loading="loading"
              data-testid="submit"
            >
              Login
            </v-btn>
          </v-form>
        </v-card-text>
      </v-card>
    </v-col>
  </v-row>
</template>
