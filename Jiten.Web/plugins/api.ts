export default defineNuxtPlugin({
  setup() {
    const api = $fetch.create({
      baseURL: useRuntimeConfig().public.baseURL,
    });

    return {
      provide: {
        api,
      },
    };
  },
});
